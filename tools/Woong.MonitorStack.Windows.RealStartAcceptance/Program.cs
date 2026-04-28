using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Microsoft.Data.Sqlite;

var exitCode = RealStartAcceptanceRunner.Run(args);
return exitCode;

internal static class RealStartAcceptanceRunner
{
    public static int Run(string[] args)
    {
        RealStartOptions options;
        try
        {
            options = RealStartOptions.Parse(args);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine($"[FAIL] {exception.Message}");
            Console.Error.WriteLine("Usage: --app <path> --db <path> [--seconds <seconds>] [--allow-server-sync]");
            return 2;
        }

        Console.WriteLine("This will observe foreground window metadata for local testing.");
        Console.WriteLine("It will not record keystrokes.");
        Console.WriteLine("It will not capture screen contents.");
        Console.WriteLine($"It will use a temp DB unless configured otherwise: {options.DatabasePath}");
        if (!options.AllowServerSync)
        {
            Console.WriteLine("Server sync is disabled. Pass --allow-server-sync only for an explicit real sync test.");
        }

        Process? process = null;
        try
        {
            if (!File.Exists(options.AppPath))
            {
                throw new FileNotFoundException("WPF app executable was not found.", options.AppPath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(options.DatabasePath)!);
            var startInfo = new ProcessStartInfo(options.AppPath)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(options.AppPath) ?? Environment.CurrentDirectory
            };
            startInfo.Environment["WOONG_MONITOR_LOCAL_DB"] = options.DatabasePath;
            startInfo.Environment["WOONG_MONITOR_DEVICE_ID"] = "real-start-local";
            startInfo.Environment["WOONG_MONITOR_ALLOW_SERVER_SYNC"] = options.AllowServerSync ? "1" : "0";

            process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to launch WPF app.");
            using var automation = new UIA3Automation();
            using Application application = Application.Attach(process);
            Window mainWindow = application.GetMainWindow(automation, options.Timeout)
                ?? throw new InvalidOperationException("Main window did not appear.");

            mainWindow.Focus();
            Invoke(mainWindow, "StartTrackingButton");
            Thread.Sleep(options.ObservationDuration);
            Invoke(mainWindow, "StopTrackingButton");
            Thread.Sleep(TimeSpan.FromSeconds(1));

            int focusCount = CountRows(options.DatabasePath, "focus_session");
            int outboxCount = CountRows(options.DatabasePath, "sync_outbox");
            if (focusCount <= 0)
            {
                throw new InvalidOperationException("No focus_session rows were persisted.");
            }

            if (outboxCount <= 0)
            {
                throw new InvalidOperationException("No sync_outbox rows were queued.");
            }

            Console.WriteLine($"PASS: persisted {focusCount} focus_session row(s) and queued {outboxCount} sync_outbox row(s).");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"[FAIL] RealStart acceptance failed: {exception.Message}");
            return 1;
        }
        finally
        {
            try
            {
                if (process is { HasExited: false })
                {
                    process.CloseMainWindow();
                    if (!process.WaitForExit(5000))
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"[WARN] Failed to close app process cleanly: {exception.Message}");
            }
        }
    }

    private static void Invoke(Window mainWindow, string automationId)
    {
        AutomationElement? element = mainWindow.FindFirstDescendant(automationId);
        if (element is null)
        {
            throw new InvalidOperationException($"Could not find control `{automationId}`.");
        }

        element.AsButton().Invoke();
    }

    private static int CountRows(string databasePath, string tableName)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName};";

        return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }
}

internal sealed record RealStartOptions(
    string AppPath,
    string DatabasePath,
    TimeSpan ObservationDuration,
    TimeSpan Timeout,
    bool AllowServerSync)
{
    public static RealStartOptions Parse(string[] args)
    {
        string? appPath = null;
        string? databasePath = null;
        var observationDuration = TimeSpan.FromSeconds(3);
        var timeout = TimeSpan.FromSeconds(20);
        var allowServerSync = false;

        for (var index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            switch (arg)
            {
                case "--app":
                    appPath = ReadValue(args, ref index, arg);
                    break;
                case "--db":
                    databasePath = ReadValue(args, ref index, arg);
                    break;
                case "--seconds":
                    observationDuration = TimeSpan.FromSeconds(ReadPositiveInt(args, ref index, arg));
                    break;
                case "--timeout-seconds":
                    timeout = TimeSpan.FromSeconds(ReadPositiveInt(args, ref index, arg));
                    break;
                case "--allow-server-sync":
                    allowServerSync = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        if (string.IsNullOrWhiteSpace(appPath))
        {
            throw new ArgumentException("--app is required.");
        }

        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("--db is required.");
        }

        return new RealStartOptions(
            Path.GetFullPath(appPath),
            Path.GetFullPath(databasePath),
            observationDuration,
            timeout,
            allowServerSync);
    }

    private static int ReadPositiveInt(string[] args, ref int index, string argumentName)
    {
        string value = ReadValue(args, ref index, argumentName);
        if (!int.TryParse(value, out int parsed) || parsed <= 0)
        {
            throw new ArgumentException($"{argumentName} must be a positive integer.");
        }

        return parsed;
    }

    private static string ReadValue(string[] args, ref int index, string argumentName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"{argumentName} requires a value.");
        }

        index++;
        return args[index];
    }
}
