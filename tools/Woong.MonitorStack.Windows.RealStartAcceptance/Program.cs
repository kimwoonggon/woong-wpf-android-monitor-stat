using System.Diagnostics;
using System.Text.Json;
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
            EnsureTrackingRunning(mainWindow);
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

            string processName = ReadLatestFocusSessionProcessName(options.DatabasePath);
            VerifyRecentAppSessionVisible(mainWindow, processName, options.Timeout);

            RealStartEvidence[] evidence =
            [
                new(
                    "focus_session persisted",
                    "> 0 rows in local SQLite focus_session",
                    focusCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    AcceptanceStatus.Pass),
                new(
                    "sync_outbox queued",
                    "> 0 rows queued while sync remains opt-in",
                    outboxCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    AcceptanceStatus.Pass),
                new(
                    "latest focus session app/process readable",
                    "non-empty app or process name",
                    processName,
                    AcceptanceStatus.Pass),
                new(
                    "server sync disabled unless explicitly allowed",
                    "AllowServerSync=false by default",
                    options.AllowServerSync ? "AllowServerSync=true" : "AllowServerSync=false",
                    options.AllowServerSync ? AcceptanceStatus.Warn : AcceptanceStatus.Pass)
            ];
            WriteRealStartArtifacts(options, evidence, isSuccess: true);

            Console.WriteLine($"PASS: persisted {focusCount} focus_session row(s) and queued {outboxCount} sync_outbox row(s).");
            return 0;
        }
        catch (Exception exception)
        {
            WriteRealStartFailureArtifacts(options, exception);
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

    private static void EnsureTrackingRunning(Window mainWindow)
    {
        string trackingStatus = GetElementText(mainWindow, "TrackingStatusText");
        if (trackingStatus.Contains("Running", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("PASS: Tracking already running; StartTrackingButton is disabled because auto-start already ran.");
            return;
        }

        AutomationElement? startButton = mainWindow.FindFirstDescendant("StartTrackingButton")
            ?? throw new InvalidOperationException("Could not find control `StartTrackingButton`.");
        if (!startButton.IsEnabled)
        {
            throw new InvalidOperationException(
                $"StartTrackingButton is disabled because auto-start already ran, but TrackingStatusText was `{trackingStatus}`.");
        }

        startButton.AsButton().Invoke();
        Thread.Sleep(TimeSpan.FromMilliseconds(200));
        trackingStatus = GetElementText(mainWindow, "TrackingStatusText");
        if (!trackingStatus.Contains("Running", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Tracking did not start. TrackingStatusText was `{trackingStatus}`.");
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

    private static string GetElementText(Window mainWindow, string automationId)
    {
        AutomationElement? element = mainWindow.FindFirstDescendant(automationId);
        if (element is null)
        {
            return "";
        }

        string itemStatus = element.ItemStatus;
        if (!string.IsNullOrWhiteSpace(itemStatus))
        {
            return itemStatus;
        }

        if (element.Patterns.Text.IsSupported)
        {
            string text = element.Patterns.Text.Pattern.DocumentRange.GetText(-1).TrimEnd('\r', '\n');
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        if (element.Patterns.Value.IsSupported)
        {
            string value = element.Patterns.Value.Pattern.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return element.Name ?? "";
    }

    private static int CountRows(string databasePath, string tableName)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName};";

        return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void VerifyRecentAppSessionVisible(Window mainWindow, string processName, TimeSpan timeout)
    {
        AutomationElement? list = mainWindow.FindFirstDescendant("RecentAppSessionsList");
        if (list is null)
        {
            throw new InvalidOperationException("Could not find control `RecentAppSessionsList`.");
        }

        bool appeared = WaitUntil(timeout, () => ElementContainsText(list, processName));
        if (!appeared)
        {
            throw new InvalidOperationException(
                $"Persisted focus session `{processName}` did not appear in RecentAppSessionsList.");
        }

        Console.WriteLine($"PASS: persisted focus session appeared in RecentAppSessionsList: {processName}");
    }

    private static void WriteRealStartFailureArtifacts(RealStartOptions options, Exception exception)
    {
        try
        {
            RealStartEvidence[] evidence =
            [
                new(
                    "real-start acceptance completed",
                    "real WPF app start persists focus_session and queues sync_outbox",
                    exception.Message,
                    AcceptanceStatus.Fail)
            ];
            WriteRealStartArtifacts(options, evidence, isSuccess: false, exception.Message);
        }
        catch (Exception artifactException)
        {
            Console.Error.WriteLine($"[WARN] Failed to write RealStart failure artifacts: {artifactException.Message}");
        }
    }

    private static void WriteRealStartArtifacts(
        RealStartOptions options,
        IReadOnlyCollection<RealStartEvidence> evidence,
        bool isSuccess,
        string? failure = null)
    {
        string outputDirectory = Path.GetDirectoryName(options.DatabasePath) ?? Environment.CurrentDirectory;
        Directory.CreateDirectory(outputDirectory);
        IReadOnlyCollection<RealStartEvidence> safety = BuildRealStartSafetyEvidence(options);

        WriteRealStartReport(outputDirectory, evidence, safety, isSuccess, failure);
        WriteRealStartManifest(outputDirectory, options, evidence, safety, isSuccess, failure);
    }

    private static void WriteRealStartReport(
        string outputDirectory,
        IReadOnlyCollection<RealStartEvidence> evidence,
        IReadOnlyCollection<RealStartEvidence> safety,
        bool isSuccess,
        string? failure)
    {
        string reportPath = Path.Combine(outputDirectory, "real-start-report.md");
        using var writer = new StreamWriter(reportPath, append: false);
        writer.WriteLine("# WPF RealStart Acceptance Report");
        writer.WriteLine();
        writer.WriteLine($"Status: {(isSuccess ? "PASS" : "FAIL")}");
        writer.WriteLine($"Generated at UTC: {DateTimeOffset.UtcNow:O}");
        if (!string.IsNullOrWhiteSpace(failure))
        {
            writer.WriteLine($"Failure: {EscapeMarkdownCell(failure)}");
        }

        writer.WriteLine();
        writer.WriteLine("## RealStart Local DB Evidence");
        writer.WriteLine();
        writer.WriteLine("| Claim | Expected | Actual | Status |");
        writer.WriteLine("| --- | --- | --- | --- |");
        foreach (RealStartEvidence item in evidence)
        {
            writer.WriteLine(
                $"| {EscapeMarkdownCell(item.Claim)} | {EscapeMarkdownCell(item.Expected)} | {EscapeMarkdownCell(item.Actual)} | {item.Status} |");
        }

        writer.WriteLine();
        writer.WriteLine("## RealStart Safety Evidence");
        writer.WriteLine();
        writer.WriteLine("| Claim | Expected | Actual | Status |");
        writer.WriteLine("| --- | --- | --- | --- |");
        foreach (RealStartEvidence item in safety)
        {
            writer.WriteLine(
                $"| {EscapeMarkdownCell(item.Claim)} | {EscapeMarkdownCell(item.Expected)} | {EscapeMarkdownCell(item.Actual)} | {item.Status} |");
        }
    }

    private static void WriteRealStartManifest(
        string outputDirectory,
        RealStartOptions options,
        IReadOnlyCollection<RealStartEvidence> evidence,
        IReadOnlyCollection<RealStartEvidence> safety,
        bool isSuccess,
        string? failure)
    {
        string manifestPath = Path.Combine(outputDirectory, "real-start-manifest.json");
        var manifest = new
        {
            status = isSuccess ? "PASS" : "FAIL",
            generatedAtUtc = DateTimeOffset.UtcNow,
            appPath = options.AppPath,
            databasePath = options.DatabasePath,
            allowServerSync = options.AllowServerSync,
            failure,
            realStartEvidence = evidence.Select(item => new
            {
                claim = item.Claim,
                expected = item.Expected,
                actual = item.Actual,
                status = item.Status.ToString()
            }).ToArray(),
            realStartSafetyEvidence = safety.Select(item => new
            {
                claim = item.Claim,
                expected = item.Expected,
                actual = item.Actual,
                status = item.Status.ToString()
            }).ToArray()
        };

        File.WriteAllText(
            manifestPath,
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static RealStartEvidence[] BuildRealStartSafetyEvidence(RealStartOptions options)
        =>
        [
            new(
                "Explicit local SQLite DB",
                "RealStart acceptance writes only to the explicit --db path.",
                options.DatabasePath,
                AcceptanceStatus.Pass),
            new(
                "Test device id only",
                "Launched WPF app receives WOONG_MONITOR_DEVICE_ID=real-start-local.",
                "real-start-local",
                AcceptanceStatus.Pass),
            new(
                "Server sync opt-in",
                "WOONG_MONITOR_ALLOW_SERVER_SYNC stays 0 unless --allow-server-sync is passed.",
                options.AllowServerSync ? "WOONG_MONITOR_ALLOW_SERVER_SYNC=1" : "WOONG_MONITOR_ALLOW_SERVER_SYNC=0",
                options.AllowServerSync ? AcceptanceStatus.Warn : AcceptanceStatus.Pass),
            new(
                "Process cleanup scoped to launched WPF app",
                "Cleanup closes only the process launched by this RealStart acceptance run.",
                options.AppPath,
                AcceptanceStatus.Pass)
        ];

    private static string EscapeMarkdownCell(string value)
        => value.Replace("|", "\\|", StringComparison.Ordinal);

    private static string ReadLatestFocusSessionProcessName(string databasePath)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE(process_name, platform_app_key)
            FROM focus_session
            ORDER BY ended_at_utc DESC
            LIMIT 1;
            """;

        object? value = command.ExecuteScalar();
        return value is string processName && !string.IsNullOrWhiteSpace(processName)
            ? processName
            : throw new InvalidOperationException("No readable focus_session process/app name was persisted.");
    }

    private static bool ElementContainsText(AutomationElement element, string expectedText)
        => element
            .FindAllDescendants()
            .Select(descendant => descendant.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Any(name => name.Contains(expectedText, StringComparison.OrdinalIgnoreCase));

    private static bool WaitUntil(TimeSpan timeout, Func<bool> condition)
    {
        var stopwatch = Stopwatch.StartNew();
        do
        {
            if (condition())
            {
                return true;
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(100));
        }
        while (stopwatch.Elapsed < timeout);

        return false;
    }
}

internal enum AcceptanceStatus
{
    Pass,
    Warn,
    Fail
}

internal sealed record RealStartEvidence(
    string Claim,
    string Expected,
    string Actual,
    AcceptanceStatus Status);

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
