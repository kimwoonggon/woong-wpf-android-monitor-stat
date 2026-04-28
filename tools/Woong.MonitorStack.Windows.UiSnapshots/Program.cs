using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

var exitCode = UiSnapshotRunner.Run(args);
return exitCode;

internal static class UiSnapshotRunner
{
    private const string AppFileName = "Woong.MonitorStack.Windows.App.exe";
    private const string MainWindowAutomationId = "MainWindow";

    public static int Run(string[] args)
    {
        UiSnapshotOptions options;
        try
        {
            options = UiSnapshotOptions.Parse(args);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine($"[FAIL] {exception.Message}");
            Console.Error.WriteLine("Usage: dotnet run --project tools/Woong.MonitorStack.Windows.UiSnapshots -- [--app <path>] [--output-root <path>] [--timeout-seconds <seconds>]");
            return 2;
        }

        var notes = new List<string>();
        Directory.CreateDirectory(options.RunDirectory);

        Application? application = null;
        try
        {
            if (!File.Exists(options.AppPath))
            {
                throw new FileNotFoundException(
                    $"WPF app executable was not found. Build the app first or pass --app. Expected: {options.AppPath}",
                    options.AppPath);
            }

            using var automation = new UIA3Automation();
            application = Application.Launch(options.AppPath);
            Window mainWindow = application.GetMainWindow(automation, options.Timeout)
                ?? throw new InvalidOperationException($"Main window with title process for '{AppFileName}' did not appear within {options.Timeout.TotalSeconds:N0} seconds.");

            string? automationId = TryGetAutomationId(mainWindow, notes);
            if (!string.Equals(automationId, MainWindowAutomationId, StringComparison.Ordinal))
            {
                notes.Add($"Main window AutomationId was '{automationId ?? "<unsupported>"}', expected '{MainWindowAutomationId}'. Continuing because the window was found by process.");
            }

            mainWindow.Focus();
            TryMoveWindow(mainWindow, notes);
            Thread.Sleep(500);

            CaptureWindow(mainWindow, options.RunDirectory, "01-startup.png", notes);
            CaptureElementIfAvailable(mainWindow, "SummaryCardsContainer", options.RunDirectory, "summary-cards.png", notes);
            CaptureElementIfAvailable(mainWindow, "ChartArea", options.RunDirectory, "chart-area.png", notes);
            CaptureElementIfAvailable(mainWindow, "RecentAppSessionsList", options.RunDirectory, "recent-sessions.png", notes);

            InvokeIfAvailable(mainWindow, "RefreshButton", "Refresh", notes);
            Thread.Sleep(500);
            CaptureWindow(mainWindow, options.RunDirectory, "02-dashboard-after-refresh.png", notes);

            InvokeIfAvailable(mainWindow, "Last6HoursPeriodButton", "Last 6 hours period", notes);
            Thread.Sleep(500);
            CaptureWindow(mainWindow, options.RunDirectory, "03-dashboard-period-change.png", notes);

            SelectTabIfAvailable(mainWindow, "LiveEventsTab", "Live Event Log", notes);
            Thread.Sleep(300);
            CaptureElementIfAvailable(mainWindow, "LiveEventsList", options.RunDirectory, "live-events.png", notes);

            SelectTabIfAvailable(mainWindow, "SettingsTab", "Settings", notes);
            Thread.Sleep(500);
            CaptureWindow(mainWindow, options.RunDirectory, "04-settings.png", notes);

            WriteReport(options.RunDirectory, options.AppPath, notes, isSuccess: true);
            ReplaceLatest(options.OutputRoot, options.RunDirectory);
            Console.WriteLine($"UI snapshots saved to: {options.RunDirectory}");
            Console.WriteLine($"Latest snapshots copied to: {Path.Combine(options.OutputRoot, "latest")}");

            return 0;
        }
        catch (Exception exception)
        {
            notes.Add($"Failure: {exception.GetType().Name}: {exception.Message}");
            WriteReport(options.RunDirectory, options.AppPath, notes, isSuccess: false);
            Console.Error.WriteLine($"[FAIL] UI snapshot automation failed: {exception.Message}");
            Console.Error.WriteLine($"Report written to: {Path.Combine(options.RunDirectory, "report.md")}");

            return 1;
        }
        finally
        {
            try
            {
                application?.Close(killIfCloseFails: true);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"[WARN] Failed to close WPF app cleanly: {exception.Message}");
            }
        }
    }

    private static void CaptureWindow(Window window, string directory, string fileName, ICollection<string> notes)
    {
        string path = Path.Combine(directory, fileName);
        window.CaptureToFile(path);
        notes.Add($"Captured `{fileName}`.");
    }

    private static string? TryGetAutomationId(AutomationElement element, ICollection<string> notes)
    {
        try
        {
            return element.AutomationId;
        }
        catch (Exception exception)
        {
            notes.Add($"Main window AutomationId could not be read: {exception.Message}");
            return null;
        }
    }

    private static void CaptureElementIfAvailable(
        Window window,
        string automationId,
        string directory,
        string fileName,
        ICollection<string> notes)
    {
        AutomationElement? element = window.FindFirstDescendant(automationId);
        if (element is null)
        {
            notes.Add($"Optional crop `{fileName}` skipped because `{automationId}` was not visible.");
            return;
        }

        element.CaptureToFile(Path.Combine(directory, fileName));
        notes.Add($"Captured optional crop `{fileName}` from `{automationId}`.");
    }

    private static void InvokeIfAvailable(Window window, string automationId, string label, ICollection<string> notes)
    {
        AutomationElement? element = window.FindFirstDescendant(automationId);
        if (element is null)
        {
            notes.Add($"{label} control skipped because `{automationId}` was not found.");
            return;
        }

        element.AsButton().Invoke();
        notes.Add($"Invoked {label} control.");
    }

    private static void SelectTabIfAvailable(Window window, string automationId, string tabName, ICollection<string> notes)
    {
        AutomationElement? element = window.FindFirstDescendant(automationId)
            ?? window.FindFirstDescendant(condition => condition.ByName(tabName));
        if (element is null)
        {
            notes.Add($"Tab `{tabName}` skipped because `{automationId}` was not found.");
            return;
        }

        element.AsTabItem().Select();
        notes.Add($"Selected `{tabName}` tab.");
    }

    private static void TryMoveWindow(Window window, ICollection<string> notes)
    {
        try
        {
            window.Move(0, 0);
            notes.Add("Moved main window to 0,0 for more deterministic screenshots. Size uses XAML defaults.");
        }
        catch (Exception exception)
        {
            notes.Add($"Could not move main window: {exception.Message}");
        }
    }

    private static void WriteReport(string directory, string appPath, IReadOnlyCollection<string> notes, bool isSuccess)
    {
        string[] primaryScreenshots =
        [
            "01-startup.png",
            "02-dashboard-after-refresh.png",
            "03-dashboard-period-change.png",
            "04-settings.png"
        ];
        string[] optionalScreenshots =
        [
            "summary-cards.png",
            "chart-area.png",
            "recent-sessions.png",
            "live-events.png"
        ];
        var lines = new List<string>
        {
            "# WPF UI Snapshot Report",
            "",
            $"Status: {(isSuccess ? "PASS" : "FAIL")}",
            $"Generated at UTC: {DateTimeOffset.UtcNow:O}",
            $"App: `{appPath}`",
            "",
            "## Primary Screenshots",
            ""
        };

        foreach (string screenshot in primaryScreenshots)
        {
            lines.Add(File.Exists(Path.Combine(directory, screenshot))
                ? $"- [{screenshot}]({screenshot})"
                : $"- {screenshot} missing");
        }

        lines.Add("");
        lines.Add("## Optional Crops");
        lines.Add("");
        foreach (string screenshot in optionalScreenshots)
        {
            lines.Add(File.Exists(Path.Combine(directory, screenshot))
                ? $"- [{screenshot}]({screenshot})"
                : $"- {screenshot} not captured");
        }

        lines.Add("");
        lines.Add("## Notes");
        lines.Add("");
        foreach (string note in notes)
        {
            lines.Add($"- {note}");
        }

        File.WriteAllLines(Path.Combine(directory, "report.md"), lines);
    }

    private static void ReplaceLatest(string outputRoot, string runDirectory)
    {
        string latestDirectory = Path.Combine(outputRoot, "latest");
        if (Directory.Exists(latestDirectory))
        {
            Directory.Delete(latestDirectory, recursive: true);
        }

        CopyDirectory(runDirectory, latestDirectory);
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);
        foreach (string sourceFile in Directory.EnumerateFiles(sourceDirectory))
        {
            string destinationFile = Path.Combine(destinationDirectory, Path.GetFileName(sourceFile));
            File.Copy(sourceFile, destinationFile, overwrite: true);
        }
    }
}

internal sealed record UiSnapshotOptions(
    string RepositoryRoot,
    string AppPath,
    string OutputRoot,
    string RunDirectory,
    TimeSpan Timeout)
{
    private const string AppFileName = "Woong.MonitorStack.Windows.App.exe";

    public static UiSnapshotOptions Parse(string[] args)
    {
        string repositoryRoot = FindRepositoryRoot();
        string? appPath = null;
        string? outputRoot = null;
        var timeout = TimeSpan.FromSeconds(20);

        for (var index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            switch (arg)
            {
                case "--app":
                    appPath = ReadValue(args, ref index, arg);
                    break;
                case "--output-root":
                    outputRoot = ReadValue(args, ref index, arg);
                    break;
                case "--timeout-seconds":
                    string timeoutValue = ReadValue(args, ref index, arg);
                    if (!int.TryParse(timeoutValue, out int timeoutSeconds) || timeoutSeconds <= 0)
                    {
                        throw new ArgumentException("--timeout-seconds must be a positive integer.");
                    }

                    timeout = TimeSpan.FromSeconds(timeoutSeconds);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        appPath ??= Path.Combine(
            repositoryRoot,
            "src",
            "Woong.MonitorStack.Windows.App",
            "bin",
            "Debug",
            "net10.0-windows",
            AppFileName);
        outputRoot ??= Path.Combine(repositoryRoot, "artifacts", "ui-snapshots");
        string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
        string runDirectory = Path.Combine(outputRoot, timestamp);

        return new UiSnapshotOptions(
            repositoryRoot,
            Path.GetFullPath(appPath),
            Path.GetFullPath(outputRoot),
            Path.GetFullPath(runDirectory),
            timeout);
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

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Woong.MonitorStack.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from the tool output directory.");
    }
}
