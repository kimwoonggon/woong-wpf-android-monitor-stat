using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Microsoft.Data.Sqlite;

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
            Console.Error.WriteLine("Usage: dotnet run --project tools/Woong.MonitorStack.Windows.UiSnapshots -- [--app <path>] [--output-root <path>] [--db <path>] [--mode EmptyData|SampleDashboard|TrackingPipeline] [--timeout-seconds <seconds>] [--viewport-widths <csv>] [--allow-server-sync]");
            return 2;
        }

        var context = new UiSnapshotContext(options);
        Process? process = null;
        try
        {
            if (!File.Exists(options.AppPath))
            {
                throw new FileNotFoundException(
                    $"WPF app executable was not found. Build the app first or pass --app. Expected: {options.AppPath}",
                    options.AppPath);
            }

            Directory.CreateDirectory(options.RunDirectory);
            ProcessStartInfo startInfo = CreateStartInfo(options);
            process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to launch WPF app.");

            using var automation = new UIA3Automation();
            using Application application = Application.Attach(process);
            Window mainWindow = application.GetMainWindow(automation, options.Timeout)
                ?? throw new InvalidOperationException($"Main window for '{AppFileName}' did not appear within {options.Timeout.TotalSeconds:N0} seconds.");

            string? automationId = TryGetAutomationId(mainWindow, context);
            if (!string.Equals(automationId, MainWindowAutomationId, StringComparison.Ordinal))
            {
                context.Warn(
                    "MainWindow AutomationId",
                    MainWindowAutomationId,
                    automationId ?? "<unsupported>",
                    "Continuing because the window was found by process.");
            }

            mainWindow.Focus();
            TryMoveWindow(mainWindow, context);
            Thread.Sleep(500);
            VerifyHeaderBadgeSemanticNames(mainWindow, context);
            VerifyCurrentFocusSemanticEvidence(mainWindow, context);

            if (options.Mode == UiSnapshotMode.TrackingPipeline)
            {
                RunTrackingPipelineAcceptance(mainWindow, context);
            }
            else if (options.Mode == UiSnapshotMode.SampleDashboard)
            {
                RunSampleDashboardAcceptance(mainWindow, context);
            }
            else
            {
                RunEmptyDataSnapshots(mainWindow, context);
            }

            WriteArtifacts(context, isSuccess: context.Results.All(result => result.Status != CheckStatus.Fail));
            ReplaceLatest(options.OutputRoot, options.RunDirectory);
            Console.WriteLine($"UI snapshots saved to: {options.RunDirectory}");
            Console.WriteLine($"Latest snapshots copied to: {Path.Combine(options.OutputRoot, "latest")}");

            return context.Results.All(result => result.Status != CheckStatus.Fail) ? 0 : 1;
        }
        catch (Exception exception)
        {
            context.Fail("Tool execution", "No unhandled exception", $"{exception.GetType().Name}: {exception.Message}");
            WriteArtifacts(context, isSuccess: false);
            Console.Error.WriteLine($"[FAIL] UI snapshot automation failed: {exception.Message}");
            Console.Error.WriteLine($"Report written to: {Path.Combine(options.RunDirectory, "report.md")}");

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
                Console.Error.WriteLine($"[WARN] Failed to close WPF app cleanly: {exception.Message}");
            }
        }
    }

    private static void VerifyHeaderBadgeSemanticNames(Window window, UiSnapshotContext context)
    {
        context.CheckContains(
            "Header TrackingStatusBadge readable name",
            "Tracking",
            GetElementName(window, "TrackingStatusBadge"));
        context.CheckContains(
            "Header SyncStatusBadge readable name",
            "Sync",
            GetElementName(window, "SyncStatusBadge"));
        context.CheckContains(
            "Header PrivacyStatusBadge readable name",
            "Privacy",
            GetElementName(window, "PrivacyStatusBadge"));
    }

    private static void VerifyCurrentFocusSemanticEvidence(Window window, UiSnapshotContext context)
    {
        CurrentFocusSemanticField[] fields =
        [
            new(
                "CurrentAppNameText",
                "Current app",
                "Current Focus CurrentAppNameText readable name",
                "Current Focus CurrentAppNameText runtime status"),
            new(
                "CurrentProcessNameText",
                "Current process",
                "Current Focus CurrentProcessNameText readable name",
                "Current Focus CurrentProcessNameText runtime status"),
            new(
                "CurrentWindowTitleText",
                "Current window title",
                "Current Focus CurrentWindowTitleText readable name",
                "Current Focus CurrentWindowTitleText runtime status"),
            new(
                "CurrentBrowserDomainText",
                "Current browser domain",
                "Current Focus CurrentBrowserDomainText readable name",
                "Current Focus CurrentBrowserDomainText runtime status"),
            new(
                "CurrentSessionDurationText",
                "Current session duration",
                "Current Focus CurrentSessionDurationText readable name",
                "Current Focus CurrentSessionDurationText runtime status"),
            new(
                "LastPollTimeText",
                "Last poll time",
                "Current Focus LastPollTimeText readable name",
                "Current Focus LastPollTimeText runtime status"),
            new(
                "LastDbWriteTimeText",
                "Last DB write time",
                "Current Focus LastDbWriteTimeText readable name",
                "Current Focus LastDbWriteTimeText runtime status"),
            new(
                "LastPersistedSessionText",
                "Last persisted session",
                "Current Focus LastPersistedSessionText readable name",
                "Current Focus LastPersistedSessionText runtime status"),
            new(
                "LastSyncStatusText",
                "Sync state",
                "Current Focus LastSyncStatusText readable name",
                "Current Focus LastSyncStatusText runtime status")
        ];

        foreach (CurrentFocusSemanticField field in fields)
        {
            VerifyCurrentFocusField(window, context, field);
        }
    }

    private static void VerifyCurrentFocusField(
        Window window,
        UiSnapshotContext context,
        CurrentFocusSemanticField field)
    {
        string automationId = field.AutomationId;
        string readableName = GetElementName(window, automationId);
        context.CheckContains(
            field.ReadableNameCheck,
            field.ReadableName,
            readableName);

        string runtimeStatus = GetElementText(window, automationId);
        CheckStatus readableNameStatus = readableName.Contains(
            field.ReadableName,
            StringComparison.OrdinalIgnoreCase)
                ? CheckStatus.Pass
                : CheckStatus.Warn;
        CheckStatus runtimeStatusStatus = string.IsNullOrWhiteSpace(runtimeStatus) ? CheckStatus.Warn : CheckStatus.Pass;

        context.Add(
            field.RuntimeStatusCheck,
            "Non-empty runtime value from AutomationProperties.ItemStatus or text",
            string.IsNullOrWhiteSpace(runtimeStatus) ? "<empty>" : runtimeStatus,
            string.IsNullOrWhiteSpace(runtimeStatus) ? CheckStatus.Fail : CheckStatus.Pass);
        context.CurrentFocusSemanticEvidence.Add(new CurrentFocusSemanticEvidence(
            field.ReadableName,
            automationId,
            readableName,
            string.IsNullOrWhiteSpace(runtimeStatus) ? "<empty>" : runtimeStatus,
            CombineStatus(readableNameStatus, runtimeStatusStatus)));
    }

    private static CheckStatus CombineStatus(CheckStatus readableNameStatus, CheckStatus runtimeStatusStatus)
        => readableNameStatus == CheckStatus.Pass && runtimeStatusStatus == CheckStatus.Pass
            ? CheckStatus.Pass
            : CheckStatus.Warn;

    private static ProcessStartInfo CreateStartInfo(UiSnapshotOptions options)
    {
        var startInfo = new ProcessStartInfo(options.AppPath)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(options.AppPath) ?? Environment.CurrentDirectory
        };

        if (options.DatabasePath is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(options.DatabasePath)!);
            startInfo.Environment["WOONG_MONITOR_LOCAL_DB"] = options.DatabasePath;
            startInfo.Environment["WOONG_MONITOR_DEVICE_ID"] = "ui-acceptance-local";
        }

        if (options.Mode == UiSnapshotMode.TrackingPipeline)
        {
            startInfo.Environment["WOONG_MONITOR_ACCEPTANCE_MODE"] = "TrackingPipeline";
            startInfo.Environment["WOONG_MONITOR_ALLOW_SERVER_SYNC"] = options.AllowServerSync ? "1" : "0";
            startInfo.Environment["WOONG_MONITOR_AUTO_START_TRACKING"] = "1";
        }
        else if (options.Mode == UiSnapshotMode.SampleDashboard)
        {
            startInfo.Environment["WOONG_MONITOR_ACCEPTANCE_MODE"] = "SampleDashboard";
            startInfo.Environment["WOONG_MONITOR_AUTO_START_TRACKING"] = "0";
        }
        else
        {
            startInfo.Environment["WOONG_MONITOR_AUTO_START_TRACKING"] = "0";
        }

        return startInfo;
    }

    private static void RunEmptyDataSnapshots(Window mainWindow, UiSnapshotContext context)
    {
        CaptureWindow(mainWindow, "01-startup.png", context);
        CaptureElementIfAvailable(mainWindow, "SummaryCardsContainer", "summary-cards.png", context);
        CaptureElementIfAvailable(mainWindow, "ChartArea", "chart-area.png", context);
        CaptureElementIfAvailable(mainWindow, "RecentAppSessionsList", "recent-sessions.png", context);

        InvokeIfAvailable(mainWindow, "RefreshButton", "Refresh", context);
        Thread.Sleep(500);
        CaptureWindow(mainWindow, "02-dashboard-after-refresh.png", context);

        InvokeIfAvailable(mainWindow, "Last6HoursPeriodButton", "Last 6 hours period", context);
        Thread.Sleep(500);
        CaptureWindow(mainWindow, "03-dashboard-period-change.png", context);
        CaptureViewportMatrix(mainWindow, context);

        SelectTabIfAvailable(mainWindow, "SettingsTab", "Settings", context);
        Thread.Sleep(500);
        CaptureWindow(mainWindow, "04-settings.png", context);
        VerifyEmptyDataDatabase(context);
    }

    private static void RunSampleDashboardAcceptance(Window mainWindow, UiSnapshotContext context)
    {
        CaptureWindow(mainWindow, "01-startup.png", context);
        RequireExists(mainWindow, "RefreshButton", context);
        InvokeRequired(mainWindow, "RefreshButton");
        Thread.Sleep(500);
        context.Pass("RefreshButton", "Invoked", "Invoked");
        CaptureWindow(mainWindow, "02-dashboard-after-refresh.png", context);
        CaptureViewportMatrix(mainWindow, context);

        string dashboardText = GetAllVisibleText(mainWindow);
        context.CheckContains("SampleDashboard shows Chrome", "chrome.exe", dashboardText);
        context.CheckContains("SampleDashboard shows VS Code", "Code.exe", dashboardText);
        context.CheckContains("SampleDashboard shows active focus", "3h 38m", dashboardText);
        context.CheckContains("SampleDashboard shows web focus", "1h 32m", dashboardText);

        SelectTabIfAvailable(mainWindow, "WebSessionsTab", "Web Sessions", context);
        Thread.Sleep(300);
        CaptureElementIfAvailable(mainWindow, "RecentWebSessionsList", "recent-web-sessions.png", context);
        string webText = GetAllVisibleText(mainWindow);
        context.CheckContains("SampleDashboard shows github.com", "github.com", webText);
        context.CheckContains("SampleDashboard shows chatgpt.com", "chatgpt.com", webText);
        context.CheckContains("SampleDashboard shows docs.microsoft.com", "docs.microsoft.com", webText);

        SelectTabIfAvailable(mainWindow, "AppSessionsTab", "App Sessions", context);
        Thread.Sleep(300);
        CaptureElementIfAvailable(mainWindow, "RecentAppSessionsList", "recent-sessions.png", context);

        SelectTabIfAvailable(mainWindow, "LiveEventsTab", "Live Event Log", context);
        Thread.Sleep(300);
        CaptureElementIfAvailable(mainWindow, "LiveEventsList", "live-events.png", context);

        CaptureElementIfAvailable(mainWindow, "SummaryCardsContainer", "summary-cards.png", context);
        CaptureElementIfAvailable(mainWindow, "ChartArea", "chart-area.png", context);
        VerifySampleDashboardDatabase(context);
    }

    private static void RunTrackingPipelineAcceptance(Window mainWindow, UiSnapshotContext context)
    {
        CaptureWindow(mainWindow, "01-startup.png", context);
        RequireExists(mainWindow, "StartTrackingButton", context);
        RequireExists(mainWindow, "StopTrackingButton", context);
        RequireExists(mainWindow, "SyncNowButton", context);
        RequireExists(mainWindow, "SummaryCardsContainer", context);
        WarnIfMissing(mainWindow, "ChartArea", context, "Chart area may be below the current scroll viewport.");

        EnsureTrackingRunning(mainWindow, context);
        Thread.Sleep(300);
        context.CheckContains("TrackingStatusText", "Running", GetElementText(mainWindow, "TrackingStatusText"));
        context.CheckContainsAny("CurrentAppNameText start", GetElementText(mainWindow, "CurrentAppNameText"), "Code.exe", "chrome.exe");
        CaptureWindow(mainWindow, "02-after-start.png", context);
        CaptureElementOrWindowFallback(mainWindow, "CurrentActivityPanel", "current-activity.png", context);

        Thread.Sleep(1500);
        context.CheckContains("CurrentAppNameText after generated activity", "chrome.exe", GetElementText(mainWindow, "CurrentAppNameText"));
        context.CheckContains("LastPersistedSessionText", "Code.exe", GetElementText(mainWindow, "LastPersistedSessionText"));
        CaptureWindow(mainWindow, "03-after-generated-activity.png", context);

        InvokeRequired(mainWindow, "StopTrackingButton", "Stop tracking", context);
        Thread.Sleep(700);
        context.Pass("StopTrackingButton", "Invoked", "Invoked");
        context.CheckContains("TrackingStatusText stopped", "Stopped", GetElementText(mainWindow, "TrackingStatusText"));
        CaptureWindow(mainWindow, "04-after-stop.png", context);
        CaptureViewportMatrix(mainWindow, context);

        SelectTabIfAvailable(mainWindow, "AppSessionsTab", "App Sessions", context);
        Thread.Sleep(300);
        RequireExists(mainWindow, "RecentAppSessionsList", context);
        CaptureElementIfAvailable(mainWindow, "RecentAppSessionsList", "recent-sessions.png", context);
        string appText = GetAllVisibleText(mainWindow);
        context.CheckContains("TrackingPipeline shows Visual Studio Code process", "Code.exe", appText);
        context.CheckContains("TrackingPipeline shows Chrome process", "chrome.exe", appText);
        context.CheckContains("SummaryCards show expected duration", "15m", appText);

        SelectTabIfAvailable(mainWindow, "WebSessionsTab", "Web Sessions", context);
        Thread.Sleep(300);
        RequireExists(mainWindow, "RecentWebSessionsList", context);
        CaptureElementIfAvailable(mainWindow, "RecentWebSessionsList", "recent-web-sessions.png", context);
        string webText = GetAllVisibleText(mainWindow);
        context.CheckContains("TrackingPipeline shows github.com", "github.com", webText);
        context.CheckContains("TrackingPipeline shows chatgpt.com", "chatgpt.com", webText);

        InvokeRequired(mainWindow, "SyncNowButton", "Sync local-only", context);
        Thread.Sleep(500);
        context.CheckContains("SyncNow local-only skipped status", "Sync skipped", GetElementText(mainWindow, "LastSyncStatusText"));

        SelectTabIfAvailable(mainWindow, "LiveEventsTab", "Live Event Log", context);
        Thread.Sleep(300);
        CaptureElementIfAvailable(mainWindow, "LiveEventsList", "live-events.png", context);
        string liveEventEvidenceText = ReadLiveEventLogTextAcrossPages(mainWindow, context);
        context.CheckContains("LiveEventLog shows focus activity", "Focus", liveEventEvidenceText);
        context.CheckContains("LiveEventLog shows browser visit", "Web", liveEventEvidenceText);
        context.CheckContains("LiveEventLog shows tracking started", "Tracking started", liveEventEvidenceText);
        context.CheckContains("LiveEventLog shows focus session semantics", "FocusSession", liveEventEvidenceText);
        context.CheckContains("LiveEventLog shows web session semantics", "Web", liveEventEvidenceText);
        context.CheckContains("LiveEventLog shows github.com web event", "github.com", liveEventEvidenceText);
        context.CheckContains("LiveEventLog shows chatgpt.com web event", "chatgpt.com", liveEventEvidenceText);
        context.CheckContains("LiveEventLog shows outbox semantics", "Outbox", liveEventEvidenceText);
        context.CheckContains("LiveEventLog shows sync skipped", "Sync skipped", liveEventEvidenceText);
        context.CheckContains("LiveEventLog shows tracking stopped", "Tracking stopped", liveEventEvidenceText);

        CaptureElementIfAvailable(mainWindow, "SummaryCardsContainer", "summary-cards.png", context);
        CaptureElementIfAvailable(mainWindow, "ChartArea", "chart-area.png", context);

        SelectTabIfAvailable(mainWindow, "SettingsTab", "Settings", context);
        Thread.Sleep(300);
        RequireExists(mainWindow, "WindowTitleVisibleCheckBox", context);
        RequireExists(mainWindow, "SyncEnabledCheckBox", context);
        EnableCheckBox(mainWindow, "SyncEnabledCheckBox", context);
        InvokeRequired(mainWindow, "SyncNowButton", "Sync enabled", context);
        Thread.Sleep(500);
        context.CheckContains("SyncNow fake sync status", "Fake sync completed", GetElementText(mainWindow, "LastSyncStatusText"));
        CaptureWindow(mainWindow, "05-after-sync.png", context);
        CaptureWindow(mainWindow, "06-settings.png", context);
        VerifyTrackingPipelineDatabase(context);
    }

    private static void EnsureTrackingRunning(Window window, UiSnapshotContext context)
    {
        string trackingStatus = GetElementText(window, "TrackingStatusText");
        if (trackingStatus.Contains("Running", StringComparison.OrdinalIgnoreCase))
        {
            context.RecordControlAction(
                "Start tracking",
                "StartTrackingButton",
                "Tracking already running; StartTrackingButton is disabled because auto-start already ran.",
                CheckStatus.Pass);
            context.Pass(
                "StartTrackingButton",
                "Tracking starts by button or auto-start",
                "Tracking already running; StartTrackingButton is disabled because auto-start already ran.");
            return;
        }

        AutomationElement? startButton = window.FindFirstDescendant("StartTrackingButton");
        if (startButton is null)
        {
            context.Fail("StartTrackingButton", "Exists and can start tracking", "Missing");
            throw new InvalidOperationException("Could not find required control `StartTrackingButton`.");
        }

        if (!startButton.IsEnabled)
        {
            string message =
                $"StartTrackingButton is disabled because auto-start already ran, but TrackingStatusText was `{trackingStatus}`.";
            context.Fail("StartTrackingButton", "Enabled unless tracking already running", message);
            throw new InvalidOperationException(message);
        }

        startButton.AsButton().Invoke();
        context.RecordControlAction("Start tracking", "StartTrackingButton", "Invoked", CheckStatus.Pass);
        context.Pass("StartTrackingButton", "Invoked or already running", "Invoked");
    }

    private static void RequireEnabled(Window window, string automationId, UiSnapshotContext context)
    {
        AutomationElement? element = window.FindFirstDescendant(automationId);
        if (element is null)
        {
            context.Fail(automationId, "Visible and enabled", "Missing");
            return;
        }

        context.Add(
            automationId,
            "Visible and enabled",
            element.IsEnabled ? "Visible and enabled" : "Visible but disabled",
            element.IsEnabled ? CheckStatus.Pass : CheckStatus.Fail);
    }

    private static void RequireExists(Window window, string automationId, UiSnapshotContext context)
    {
        AutomationElement? element = window.FindFirstDescendant(automationId);
        context.Add(
            automationId,
            "Exists",
            element is null ? "Missing" : "Exists",
            element is null ? CheckStatus.Fail : CheckStatus.Pass);
    }

    private static void WarnIfMissing(Window window, string automationId, UiSnapshotContext context, string note)
    {
        AutomationElement? element = window.FindFirstDescendant(automationId);
        context.Add(
            automationId,
            "Exists or is reachable by scrolling",
            element is null ? "Missing from current viewport" : "Exists",
            element is null ? CheckStatus.Warn : CheckStatus.Pass);
        if (element is null)
        {
            context.Notes.Add(note);
        }
    }

    private static void CaptureWindow(Window window, string fileName, UiSnapshotContext context)
    {
        string path = Path.Combine(context.Options.RunDirectory, fileName);
        window.CaptureToFile(path);
        context.RecordScreenshot(fileName);
        context.Notes.Add($"Captured `{fileName}`.");
    }

    private static string? TryGetAutomationId(AutomationElement element, UiSnapshotContext context)
    {
        try
        {
            return element.AutomationId;
        }
        catch (Exception exception)
        {
            context.Notes.Add($"Main window AutomationId could not be read: {exception.Message}");
            return null;
        }
    }

    private static void CaptureElementIfAvailable(
        Window window,
        string automationId,
        string fileName,
        UiSnapshotContext context)
    {
        AutomationElement? element = window.FindFirstDescendant(automationId);
        if (element is null)
        {
            context.RecordSkippedScreenshot(fileName, automationId, $"`{automationId}` was not visible.");
            context.Warn(fileName, "Element screenshot", $"Skipped because `{automationId}` was not visible.", "");
            return;
        }

        TryBringElementIntoViewBeforeCapture(element, context);
        element.CaptureToFile(Path.Combine(context.Options.RunDirectory, fileName));
        context.RecordScreenshot(fileName);
        context.Notes.Add($"Captured optional crop `{fileName}` from `{automationId}`.");
    }

    private static void CaptureElementOrWindowFallback(
        Window window,
        string automationId,
        string fileName,
        UiSnapshotContext context)
    {
        AutomationElement? element = window.FindFirstDescendant(automationId);
        if (element is not null)
        {
            TryBringElementIntoViewBeforeCapture(element, context);
            element.CaptureToFile(Path.Combine(context.Options.RunDirectory, fileName));
            context.RecordScreenshot(fileName);
            context.Notes.Add($"Captured `{fileName}` from `{automationId}`.");
            return;
        }

        window.CaptureToFile(Path.Combine(context.Options.RunDirectory, fileName));
        context.RecordScreenshot(fileName);
        context.Warn(
            fileName,
            $"Crop `{automationId}`",
            "Captured full window fallback",
            $"`{automationId}` was not exposed as a UI Automation element, so `{fileName}` uses the full window.");
    }

    private static void InvokeIfAvailable(Window window, string automationId, string label, UiSnapshotContext context)
    {
        AutomationElement? element = window.FindFirstDescendant(automationId);
        if (element is null)
        {
            context.Warn(label, "Control can be invoked", $"Skipped because `{automationId}` was not found.", "");
            return;
        }

        element.AsButton().Invoke();
        context.Pass(label, "Control invoked", "Control invoked");
    }

    private static void InvokeRequired(Window window, string automationId)
    {
        AutomationElement? element = window.FindFirstDescendant(automationId)
            ?? throw new InvalidOperationException($"Could not find required control `{automationId}`.");

        element.AsButton().Invoke();
    }

    private static void InvokeRequired(
        Window window,
        string automationId,
        string actionName,
        UiSnapshotContext context)
    {
        InvokeRequired(window, automationId);
        context.RecordControlAction(actionName, automationId, "Invoked", CheckStatus.Pass);
    }

    private static void EnableCheckBox(Window window, string automationId, UiSnapshotContext context)
    {
        AutomationElement? element = window.FindFirstDescendant(automationId);
        if (element is null)
        {
            context.Fail(automationId, "Checkbox exists", "Missing");
            return;
        }

        CheckBox checkBox = element.AsCheckBox();
        if (checkBox.ToggleState == ToggleState.Off)
        {
            checkBox.Toggle();
        }

        context.CheckContains(automationId, "On", checkBox.ToggleState.ToString());
    }

    private static void SelectTabIfAvailable(Window window, string automationId, string tabName, UiSnapshotContext context)
    {
        AutomationElement? element = window.FindFirstDescendant(automationId)
            ?? window.FindFirstDescendant(condition => condition.ByName(tabName));
        if (element is null)
        {
            context.Warn(tabName, "Tab selected", $"Skipped because `{automationId}` was not found.", "");
            return;
        }

        element.AsTabItem().Select();
        context.Pass(tabName, "Tab selected", "Tab selected");
    }

    private static void CaptureViewportMatrix(Window mainWindow, UiSnapshotContext context)
    {
        foreach (int viewportWidth in context.Options.ViewportWidths)
        {
            ApplyViewport(mainWindow, viewportWidth, context);
            Thread.Sleep(300);
            CaptureWindow(mainWindow, GetViewportDashboardFileName(viewportWidth), context);
            CaptureViewportSection(mainWindow, "SummaryCardsContainer", viewportWidth, "summary-cards", context);
            CaptureViewportSection(mainWindow, "ChartArea", viewportWidth, "chart-area", context);

            SelectTabIfAvailable(mainWindow, "AppSessionsTab", "App Sessions", context);
            Thread.Sleep(150);
            CaptureViewportSection(mainWindow, "RecentAppSessionsList", viewportWidth, "recent-sessions", context);

            SelectTabIfAvailable(mainWindow, "WebSessionsTab", "Web Sessions", context);
            Thread.Sleep(150);
            CaptureViewportSection(mainWindow, "RecentWebSessionsList", viewportWidth, "recent-web-sessions", context);

            SelectTabIfAvailable(mainWindow, "LiveEventsTab", "Live Event Log", context);
            Thread.Sleep(150);
            CaptureViewportSection(mainWindow, "LiveEventsList", viewportWidth, "live-events", context);
        }
    }

    private static void CaptureViewportSection(
        Window mainWindow,
        string automationId,
        int viewportWidth,
        string sectionName,
        UiSnapshotContext context)
        => CaptureElementIfAvailable(
            mainWindow,
            automationId,
            $"viewport-{viewportWidth}-{sectionName}.png",
            context);

    private static string GetViewportDashboardFileName(int viewportWidth)
        => viewportWidth switch
        {
            1920 => "viewport-1920-dashboard.png",
            1366 => "viewport-1366-dashboard.png",
            1024 => "viewport-1024-dashboard.png",
            _ => $"viewport-{viewportWidth}-dashboard.png"
        };

    private static void ApplyViewport(Window mainWindow, int viewportWidth, UiSnapshotContext context)
    {
        int viewportHeight = viewportWidth <= 1024 ? 768 : 900;

        try
        {
            var handle = new IntPtr(mainWindow.Properties.NativeWindowHandle.Value);
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Native window handle is zero.");
            }

            bool moved = NativeMethods.MoveWindow(handle, 0, 0, viewportWidth, viewportHeight, repaint: true);
            if (!moved)
            {
                throw new InvalidOperationException($"MoveWindow failed with Win32 error {Marshal.GetLastWin32Error()}.");
            }

            context.Notes.Add($"Applied viewport {viewportWidth}x{viewportHeight}.");
        }
        catch (Exception exception)
        {
            context.Warn(
                $"Viewport {viewportWidth}",
                $"Window resized to {viewportWidth}x{viewportHeight}",
                $"Resize skipped: {exception.Message}",
                "Viewport screenshot may use the previous window size.");
        }
    }

    private static void TryBringElementIntoViewBeforeCapture(AutomationElement element, UiSnapshotContext context)
    {
        try
        {
            element.Focus();
            context.Notes.Add($"Brought `{element.AutomationId}` into view before capture.");
        }
        catch (Exception exception)
        {
            context.Notes.Add($"Could not bring `{element.AutomationId}` into view before capture: {exception.Message}");
        }
    }

    private static string GetElementName(Window window, string automationId)
    {
        AutomationElement? element = window.FindFirstDescendant(automationId);
        return element?.Name ?? "";
    }

    private static string GetElementText(Window window, string automationId)
    {
        AutomationElement? element = window.FindFirstDescendant(automationId);
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

    private static string ReadLiveEventLogTextAcrossPages(Window window, UiSnapshotContext context)
    {
        var pageTexts = new List<string> { GetElementVisibleText(window, "LiveEventsList") };

        const int maximumAdditionalPages = 5;
        for (var pageIndex = 0; pageIndex < maximumAdditionalPages; pageIndex++)
        {
            AutomationElement? nextButton = window.FindFirstDescendant("DetailsNextPageButton");
            if (nextButton is null || !nextButton.IsEnabled)
            {
                break;
            }

            nextButton.AsButton().Invoke();
            Thread.Sleep(300);
            context.Pass("LiveEventLog details pagination", "Older live events reachable", "Selected next page");
            pageTexts.Add(GetElementVisibleText(window, "LiveEventsList"));
        }

        return string.Join(
            Environment.NewLine,
            pageTexts.Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static string GetElementVisibleText(Window window, string automationId)
    {
        AutomationElement? element = window.FindFirstDescendant(automationId);
        return element is null ? "" : GetVisibleText(element);
    }

    private static string GetAllVisibleText(Window window)
        => GetVisibleText(window);

    private static string GetVisibleText(AutomationElement root)
        => string.Join(
            Environment.NewLine,
            root
                .FindAllDescendants()
                .Select(element => element.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal));

    private static void TryMoveWindow(Window window, UiSnapshotContext context)
    {
        try
        {
            window.Move(0, 0);
            context.Notes.Add("Moved main window to 0,0 for more deterministic screenshots. Size uses XAML defaults.");
        }
        catch (Exception exception)
        {
            context.Notes.Add($"Could not move main window: {exception.Message}");
        }
    }

    private static void VerifyTrackingPipelineDatabase(UiSnapshotContext context)
    {
        string? databasePath = context.Options.DatabasePath;
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            context.Fail("TrackingPipeline SQLite database", "Temp database path is configured", "Missing");
            return;
        }

        DatabaseEvidence evidence = ReadDatabaseEvidence("TrackingPipeline", databasePath);
        context.DatabaseEvidence = evidence;

        context.Add(
            "TrackingPipeline focus_session rows",
            "> 0",
            evidence.FocusSessionRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
            evidence.FocusSessionRows > 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.RecordSqliteRuntimeEvidence(
            "focus_session",
            "> 0",
            evidence.FocusSessionRows,
            evidence.FocusSessionRows > 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.Add(
            "TrackingPipeline web_session rows",
            "> 0",
            evidence.WebSessionRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
            evidence.WebSessionRows > 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.RecordSqliteRuntimeEvidence(
            "web_session",
            "> 0",
            evidence.WebSessionRows,
            evidence.WebSessionRows > 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.Add(
            "TrackingPipeline sync_outbox rows",
            "> 0",
            evidence.SyncOutboxRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
            evidence.SyncOutboxRows > 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.RecordSqliteRuntimeEvidence(
            "sync_outbox",
            "> 0",
            evidence.SyncOutboxRows,
            evidence.SyncOutboxRows > 0 ? CheckStatus.Pass : CheckStatus.Fail);
        VerifyBrowserDomainPrivacyEvidence(context, databasePath);
    }

    private static void VerifyBrowserDomainPrivacyEvidence(UiSnapshotContext context, string databasePath)
    {
        int githubDomainRows = CountRowsWhere(databasePath, "web_session", "domain = 'github.com'");
        int chatGptDomainRows = CountRowsWhere(databasePath, "web_session", "domain = 'chatgpt.com'");
        int fullUrlRows = CountRowsWhere(databasePath, "web_session", "url IS NOT NULL AND TRIM(url) <> ''");
        int pageTitleRows = CountRowsWhere(databasePath, "web_session", "page_title IS NOT NULL AND TRIM(page_title) <> ''");
        bool hasPageContentColumn = ColumnExists(databasePath, "web_session", "page_content")
            || ColumnExists(databasePath, "web_session", "content")
            || ColumnExists(databasePath, "web_session", "body");

        context.RecordBrowserDomainPrivacyEvidence(
            "Domain github.com persisted",
            "> 0 web_session rows",
            githubDomainRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
            githubDomainRows > 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.RecordBrowserDomainPrivacyEvidence(
            "Domain chatgpt.com persisted",
            "> 0 web_session rows",
            chatGptDomainRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
            chatGptDomainRows > 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.RecordBrowserDomainPrivacyEvidence(
            "Full URL values absent",
            "0 non-empty web_session.url rows",
            fullUrlRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
            fullUrlRows == 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.RecordBrowserDomainPrivacyEvidence(
            "Page title values absent",
            "0 non-empty web_session.page_title rows",
            pageTitleRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
            pageTitleRows == 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.RecordBrowserDomainPrivacyEvidence(
            "Page content storage absent",
            "No page_content/content/body columns on web_session",
            hasPageContentColumn ? "Content-like column exists" : "No content-like column",
            hasPageContentColumn ? CheckStatus.Fail : CheckStatus.Pass);
    }

    private static void VerifyEmptyDataDatabase(UiSnapshotContext context)
    {
        string? databasePath = context.Options.DatabasePath;
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            context.Fail("EmptyData SQLite database", "Temp database path is configured", "Missing");
            return;
        }

        DatabaseEvidence evidence = ReadDatabaseEvidence("EmptyData", databasePath);
        context.DatabaseEvidence = evidence;

        context.Add(
            "EmptyData focus_session rows",
            "= 0",
            evidence.FocusSessionRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
            evidence.FocusSessionRows == 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.RecordSqliteRuntimeEvidence(
            "focus_session",
            "= 0",
            evidence.FocusSessionRows,
            evidence.FocusSessionRows == 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.Add(
            "EmptyData web_session rows",
            "= 0",
            evidence.WebSessionRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
            evidence.WebSessionRows == 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.RecordSqliteRuntimeEvidence(
            "web_session",
            "= 0",
            evidence.WebSessionRows,
            evidence.WebSessionRows == 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.Add(
            "EmptyData sync_outbox rows",
            "= 0",
            evidence.SyncOutboxRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
            evidence.SyncOutboxRows == 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.RecordSqliteRuntimeEvidence(
            "sync_outbox",
            "= 0",
            evidence.SyncOutboxRows,
            evidence.SyncOutboxRows == 0 ? CheckStatus.Pass : CheckStatus.Fail);
    }

    private static void VerifySampleDashboardDatabase(UiSnapshotContext context)
    {
        string? databasePath = context.Options.DatabasePath;
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            context.Fail("SampleDashboard SQLite database", "Temp database path is configured", "Missing");
            return;
        }

        DatabaseEvidence evidence = ReadDatabaseEvidence("SampleDashboard", databasePath);
        context.DatabaseEvidence = evidence;

        context.Add(
            "SampleDashboard focus_session rows",
            "= 0",
            evidence.FocusSessionRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
            evidence.FocusSessionRows == 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.RecordSqliteRuntimeEvidence(
            "focus_session",
            "= 0",
            evidence.FocusSessionRows,
            evidence.FocusSessionRows == 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.Add(
            "SampleDashboard web_session rows",
            "= 0",
            evidence.WebSessionRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
            evidence.WebSessionRows == 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.RecordSqliteRuntimeEvidence(
            "web_session",
            "= 0",
            evidence.WebSessionRows,
            evidence.WebSessionRows == 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.Add(
            "SampleDashboard sync_outbox rows",
            "= 0",
            evidence.SyncOutboxRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
            evidence.SyncOutboxRows == 0 ? CheckStatus.Pass : CheckStatus.Fail);
        context.RecordSqliteRuntimeEvidence(
            "sync_outbox",
            "= 0",
            evidence.SyncOutboxRows,
            evidence.SyncOutboxRows == 0 ? CheckStatus.Pass : CheckStatus.Fail);
    }

    private static DatabaseEvidence ReadDatabaseEvidence(string scenarioName, string databasePath)
        => new(
            ScenarioName: scenarioName,
            DatabasePath: databasePath,
            FocusSessionRows: CountRows(databasePath, "focus_session"),
            WebSessionRows: CountRows(databasePath, "web_session"),
            SyncOutboxRows: CountRows(databasePath, "sync_outbox"));

    private static int CountRows(string databasePath, string tableName)
    {
        string safeTableName = tableName switch
        {
            "focus_session" => tableName,
            "web_session" => tableName,
            "sync_outbox" => tableName,
            _ => throw new ArgumentException($"Unsupported table name: {tableName}.", nameof(tableName))
        };

        if (!File.Exists(databasePath))
        {
            return 0;
        }

        try
        {
            using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
            connection.Open();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {safeTableName};";

            return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (SqliteException exception) when (
            exception.SqliteErrorCode == 1
            && exception.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }
    }

    private static int CountRowsWhere(string databasePath, string tableName, string whereClause)
    {
        string safeTableName = tableName switch
        {
            "web_session" => tableName,
            _ => throw new ArgumentException($"Unsupported table name: {tableName}.", nameof(tableName))
        };
        string safeWhereClause = whereClause switch
        {
            "domain = 'github.com'" => whereClause,
            "domain = 'chatgpt.com'" => whereClause,
            "url IS NOT NULL AND TRIM(url) <> ''" => whereClause,
            "page_title IS NOT NULL AND TRIM(page_title) <> ''" => whereClause,
            _ => throw new ArgumentException($"Unsupported where clause: {whereClause}.", nameof(whereClause))
        };

        if (!File.Exists(databasePath))
        {
            return 0;
        }

        try
        {
            using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
            connection.Open();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {safeTableName} WHERE {safeWhereClause};";

            return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (SqliteException exception) when (
            exception.SqliteErrorCode == 1
            && exception.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }
    }

    private static bool ColumnExists(string databasePath, string tableName, string columnName)
    {
        string safeTableName = tableName switch
        {
            "web_session" => tableName,
            _ => throw new ArgumentException($"Unsupported table name: {tableName}.", nameof(tableName))
        };
        string safeColumnName = columnName switch
        {
            "page_content" => columnName,
            "content" => columnName,
            "body" => columnName,
            _ => throw new ArgumentException($"Unsupported column name: {columnName}.", nameof(columnName))
        };

        if (!File.Exists(databasePath))
        {
            return false;
        }

        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({safeTableName});";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            string name = reader.GetString(1);
            if (string.Equals(name, safeColumnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void WriteArtifacts(UiSnapshotContext context, bool isSuccess)
    {
        WriteReport(context, isSuccess);
        WriteManifest(context, isSuccess);
        WriteVisualReviewPrompt(context);
    }

    private static void WriteReport(UiSnapshotContext context, bool isSuccess)
    {
        var lines = new List<string>
        {
            "# WPF UI Snapshot Report",
            "",
            $"Status: {(isSuccess ? "PASS" : "FAIL")}",
            $"Generated at UTC: {DateTimeOffset.UtcNow:O}",
            $"Mode: `{context.Options.Mode}`",
            $"App: `{context.Options.AppPath}`",
            "",
            "## PASS/FAIL/WARN Table",
            "",
            "| Check | Expected | Actual | Status |",
            "|:---|:---|:---|:---|"
        };

        foreach (CheckResult result in context.Results)
        {
            lines.Add($"| {Escape(result.Name)} | {Escape(result.Expected)} | {Escape(result.Actual)} | {result.Status} |");
        }

        lines.Add("");
        lines.Add("## Current Focus Runtime Semantic Evidence");
        lines.Add("");
        lines.Add("| Field | AutomationId | Readable Name | Runtime Value | Status |");
        lines.Add("|:---|:---|:---|:---|:---|");
        if (context.CurrentFocusSemanticEvidence.Count == 0)
        {
            lines.Add("| Not collected |  |  |  | Warn |");
        }
        else
        {
            foreach (CurrentFocusSemanticEvidence evidence in context.CurrentFocusSemanticEvidence)
            {
                lines.Add(
                    $"| {Escape(evidence.Field)} | {Escape(evidence.AutomationId)} | {Escape(evidence.ReadableName)} | {Escape(evidence.RuntimeValue)} | {evidence.Status} |");
            }
        }

        lines.Add("");
        lines.Add("## Section Screenshot Evidence");
        lines.Add("");
        lines.Add("| Section | AutomationId | Screenshot | Skipped Reason | Status |");
        lines.Add("|:---|:---|:---|:---|:---|");
        if (context.SectionScreenshotEvidence.Count == 0)
        {
            lines.Add("| Not collected |  |  |  | Warn |");
        }
        else
        {
            foreach (SectionScreenshotEvidence evidence in context.SectionScreenshotEvidence)
            {
                lines.Add(
                    $"| {Escape(evidence.Section)} | {Escape(evidence.AutomationId)} | {Escape(evidence.Screenshot)} | {Escape(evidence.SkippedReason)} | {evidence.Status} |");
            }
        }

        lines.Add("");
        lines.Add("## Control Action Evidence");
        lines.Add("");
        lines.Add("| Action | AutomationId | Result | Status |");
        lines.Add("|:---|:---|:---|:---|");
        if (context.ControlActionEvidence.Count == 0)
        {
            lines.Add("| Not collected |  |  | Warn |");
        }
        else
        {
            foreach (ControlActionEvidence evidence in context.ControlActionEvidence)
            {
                lines.Add(
                    $"| {Escape(evidence.Action)} | {Escape(evidence.AutomationId)} | {Escape(evidence.Result)} | {evidence.Status} |");
            }
        }

        lines.Add("");
        lines.Add("## Screenshots");
        lines.Add("");
        foreach (string screenshot in context.Screenshots.Distinct(StringComparer.Ordinal))
        {
            lines.Add($"- [{screenshot}]({screenshot})");
        }

        lines.Add("");
        lines.Add("## Skipped Screenshots");
        lines.Add("");
        if (context.SkippedScreenshots.Count == 0)
        {
            lines.Add("- None");
        }
        else
        {
            foreach (string skipped in context.SkippedScreenshots)
            {
                lines.Add($"- {skipped}");
            }
        }

        lines.Add("");
        lines.Add("## SQLite Evidence");
        lines.Add("");
        if (context.DatabaseEvidence is null)
        {
            lines.Add("- Not collected.");
        }
        else
        {
            lines.Add($"- Scenario: `{context.DatabaseEvidence.ScenarioName}`");
            lines.Add($"- Database: `{context.DatabaseEvidence.DatabasePath}`");
            lines.Add($"- {context.DatabaseEvidence.ScenarioName} focus_session rows: {context.DatabaseEvidence.FocusSessionRows}");
            lines.Add($"- {context.DatabaseEvidence.ScenarioName} web_session rows: {context.DatabaseEvidence.WebSessionRows}");
            lines.Add($"- {context.DatabaseEvidence.ScenarioName} sync_outbox rows: {context.DatabaseEvidence.SyncOutboxRows}");
        }

        lines.Add("");
        lines.Add("## SQLite Runtime Evidence");
        lines.Add("");
        lines.Add("| Store | Expected | Actual Rows | Status |");
        lines.Add("|:---|:---|---:|:---|");
        if (context.SqliteRuntimeEvidence.Count == 0)
        {
            lines.Add("| Not collected |  |  | Warn |");
        }
        else
        {
            foreach (SqliteRuntimeEvidence evidence in context.SqliteRuntimeEvidence)
            {
                lines.Add(
                    $"| {Escape(evidence.Store)} | {Escape(evidence.Expected)} | {evidence.ActualRows.ToString(System.Globalization.CultureInfo.InvariantCulture)} | {evidence.Status} |");
            }
        }

        lines.Add("");
        lines.Add("## Browser Domain Privacy Evidence");
        lines.Add("");
        lines.Add("| Claim | Expected | Actual | Status |");
        lines.Add("|:---|:---|:---|:---|");
        if (context.BrowserDomainPrivacyEvidence.Count == 0)
        {
            lines.Add("| Not collected |  |  | Warn |");
        }
        else
        {
            foreach (BrowserDomainPrivacyEvidence evidence in context.BrowserDomainPrivacyEvidence)
            {
                lines.Add(
                    $"| {Escape(evidence.Claim)} | {Escape(evidence.Expected)} | {Escape(evidence.Actual)} | {evidence.Status} |");
            }
        }

        lines.Add("");
        lines.Add("## Notes");
        lines.Add("");
        foreach (string note in context.Notes)
        {
            lines.Add($"- {note}");
        }

        lines.Add("");
        lines.Add("## Next Recommended Fixes");
        lines.Add("");
        if (isSuccess)
        {
            lines.Add("- Continue expanding semantic fake-pipeline checks before adding strict pixel comparison.");
        }
        else
        {
            lines.Add("- Fix failed semantic checks before trusting screenshots.");
        }

        File.WriteAllLines(Path.Combine(context.Options.RunDirectory, "report.md"), lines);
    }

    private static void WriteManifest(UiSnapshotContext context, bool isSuccess)
    {
        var manifest = new
        {
            status = isSuccess ? "PASS" : "FAIL",
            generatedAtUtc = DateTimeOffset.UtcNow,
            mode = context.Options.Mode.ToString(),
            appPath = context.Options.AppPath,
            databasePath = context.Options.DatabasePath,
            databaseEvidence = context.DatabaseEvidence is null
                ? null
                : new
                {
                    context.DatabaseEvidence.ScenarioName,
                    context.DatabaseEvidence.DatabasePath,
                    context.DatabaseEvidence.FocusSessionRows,
                    context.DatabaseEvidence.WebSessionRows,
                    context.DatabaseEvidence.SyncOutboxRows
                },
            sqliteRuntimeEvidence = context.SqliteRuntimeEvidence.Select(evidence => new
            {
                store = evidence.Store,
                expected = evidence.Expected,
                actualRows = evidence.ActualRows,
                status = evidence.Status.ToString()
            }).ToArray(),
            browserDomainPrivacyEvidence = context.BrowserDomainPrivacyEvidence.Select(evidence => new
            {
                claim = evidence.Claim,
                expected = evidence.Expected,
                actual = evidence.Actual,
                status = evidence.Status.ToString()
            }).ToArray(),
            viewportWidths = context.Options.ViewportWidths.ToArray(),
            screenshots = context.Screenshots.Distinct(StringComparer.Ordinal).ToArray(),
            skippedScreenshots = context.SkippedScreenshots.ToArray(),
            skippedScreenshotReasons = context.SkippedScreenshots.Select(skipped => new
            {
                ViewportWidth = (int?)null,
                Reason = skipped
            }).ToArray(),
            currentFocusRuntimeEvidence = context.CurrentFocusSemanticEvidence.Select(evidence => new
            {
                field = evidence.Field,
                readableName = evidence.ReadableName,
                automationId = evidence.AutomationId,
                runtimeValue = evidence.RuntimeValue,
                status = evidence.Status.ToString()
            }).ToArray(),
            sectionScreenshotEvidence = context.SectionScreenshotEvidence.Select(evidence => new
            {
                section = evidence.Section,
                automationId = evidence.AutomationId,
                screenshot = evidence.Screenshot,
                skippedReason = evidence.SkippedReason,
                status = evidence.Status.ToString()
            }).ToArray(),
            controlActionEvidence = context.ControlActionEvidence.Select(evidence => new
            {
                action = evidence.Action,
                automationId = evidence.AutomationId,
                result = evidence.Result,
                status = evidence.Status.ToString()
            }).ToArray(),
            checks = context.Results.Select(result => new
            {
                result.Name,
                result.Expected,
                result.Actual,
                status = result.Status.ToString()
            }).ToArray()
        };

        string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(context.Options.RunDirectory, "manifest.json"), json);
    }

    private static void WriteVisualReviewPrompt(UiSnapshotContext context)
    {
        string[] lines =
        [
            "# Manual WPF Visual Review Prompt",
            "",
            "Review the local screenshots in this folder. Do not upload them automatically.",
            "",
            "Check:",
            "",
            "- Current activity is readable.",
            "- Start/Stop state is clear.",
            "- Expected app names appear: Visual Studio Code / Code.exe and Chrome / chrome.exe.",
            "- Expected domains appear: github.com and chatgpt.com.",
            "- Summary values match the fake TrackingPipeline data.",
            "- Lists are not clipped in a way that hides required content.",
            "- Chart area is visible when expected.",
            "- Settings/privacy controls are readable.",
            "- Content is not overlapped or offscreen.",
            "",
            $"Mode: `{context.Options.Mode}`"
        ];

        File.WriteAllLines(Path.Combine(context.Options.RunDirectory, "visual-review-prompt.md"), lines);
    }

    private static string Escape(string value)
        => value.Replace("|", "\\|", StringComparison.Ordinal).ReplaceLineEndings(" ");

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

internal sealed class UiSnapshotContext
{
    public UiSnapshotContext(UiSnapshotOptions options)
    {
        Options = options;
    }

    public UiSnapshotOptions Options { get; }

    public List<CheckResult> Results { get; } = [];

    public List<CurrentFocusSemanticEvidence> CurrentFocusSemanticEvidence { get; } = [];

    public List<SectionScreenshotEvidence> SectionScreenshotEvidence { get; } = [];

    public List<ControlActionEvidence> ControlActionEvidence { get; } = [];

    public List<SqliteRuntimeEvidence> SqliteRuntimeEvidence { get; } = [];

    public List<BrowserDomainPrivacyEvidence> BrowserDomainPrivacyEvidence { get; } = [];

    public List<string> Notes { get; } = [];

    public List<string> Screenshots { get; } = [];

    public List<string> SkippedScreenshots { get; } = [];

    public DatabaseEvidence? DatabaseEvidence { get; set; }

    public void Pass(string name, string expected, string actual)
        => Add(name, expected, actual, CheckStatus.Pass);

    public void Warn(string name, string expected, string actual, string note)
    {
        Add(name, expected, actual, CheckStatus.Warn);
        if (!string.IsNullOrWhiteSpace(note))
        {
            Notes.Add(note);
        }
    }

    public void Fail(string name, string expected, string actual)
        => Add(name, expected, actual, CheckStatus.Fail);

    public void CheckContains(string name, string expected, string actual)
        => Add(
            name,
            $"Contains `{expected}`",
            string.IsNullOrWhiteSpace(actual) ? "<empty>" : actual,
            actual.Contains(expected, StringComparison.OrdinalIgnoreCase) ? CheckStatus.Pass : CheckStatus.Fail);

    public void CheckContainsAny(string name, string actual, params string[] expectedValues)
    {
        string expected = string.Join("`, `", expectedValues);
        Add(
            name,
            $"Contains any of `{expected}`",
            string.IsNullOrWhiteSpace(actual) ? "<empty>" : actual,
            expectedValues.Any(expectedValue => actual.Contains(expectedValue, StringComparison.OrdinalIgnoreCase))
                ? CheckStatus.Pass
                : CheckStatus.Fail);
    }

    public void Add(string name, string expected, string actual, CheckStatus status)
        => Results.Add(new CheckResult(name, expected, actual, status));

    public void RecordScreenshot(string fileName)
    {
        Screenshots.Add(fileName);
        AddSectionScreenshotEvidence(fileName, skippedReason: "", CheckStatus.Pass);
    }

    public void RecordSkippedScreenshot(string fileName, string automationId, string skippedReason)
    {
        SkippedScreenshots.Add($"{fileName}: {skippedReason}");
        AddSectionScreenshotEvidence(fileName, skippedReason, CheckStatus.Warn, automationId);
    }

    public void RecordControlAction(string action, string automationId, string result, CheckStatus status)
        => ControlActionEvidence.Add(new ControlActionEvidence(action, automationId, result, status));

    public void RecordSqliteRuntimeEvidence(string store, string expected, int actualRows, CheckStatus status)
        => SqliteRuntimeEvidence.Add(new SqliteRuntimeEvidence(store, expected, actualRows, status));

    public void RecordBrowserDomainPrivacyEvidence(string claim, string expected, string actual, CheckStatus status)
        => BrowserDomainPrivacyEvidence.Add(new BrowserDomainPrivacyEvidence(claim, expected, actual, status));

    private void AddSectionScreenshotEvidence(
        string fileName,
        string skippedReason,
        CheckStatus status,
        string? fallbackAutomationId = null)
    {
        SectionScreenshotDefinition? definition = SectionScreenshotDefinition.FromFileName(fileName);
        if (definition is null)
        {
            return;
        }

        SectionScreenshotEvidence.Add(new SectionScreenshotEvidence(
            definition.Section,
            fallbackAutomationId ?? definition.AutomationId,
            status == CheckStatus.Pass ? fileName : "",
            skippedReason,
            status));
    }
}

internal sealed record CheckResult(string Name, string Expected, string Actual, CheckStatus Status);

internal sealed record CurrentFocusSemanticEvidence(
    string Field,
    string AutomationId,
    string ReadableName,
    string RuntimeValue,
    CheckStatus Status);

internal sealed record SectionScreenshotEvidence(
    string Section,
    string AutomationId,
    string Screenshot,
    string SkippedReason,
    CheckStatus Status);

internal sealed record ControlActionEvidence(
    string Action,
    string AutomationId,
    string Result,
    CheckStatus Status);

internal sealed record SqliteRuntimeEvidence(
    string Store,
    string Expected,
    int ActualRows,
    CheckStatus Status);

internal sealed record BrowserDomainPrivacyEvidence(
    string Claim,
    string Expected,
    string Actual,
    CheckStatus Status);

internal sealed record SectionScreenshotDefinition(string Section, string AutomationId)
{
    public static SectionScreenshotDefinition? FromFileName(string fileName)
        => fileName switch
        {
            "current-activity.png" => new("Current activity", "CurrentActivityPanel"),
            "summary-cards.png" => new("Summary cards", "SummaryCardsContainer"),
            "recent-sessions.png" => new("Sessions", "RecentAppSessionsList"),
            "recent-web-sessions.png" => new("Web sessions", "RecentWebSessionsList"),
            "live-events.png" => new("Live events", "LiveEventsList"),
            "chart-area.png" => new("Chart area", "ChartArea"),
            "04-settings.png" or "06-settings.png" => new("Settings", "SettingsTab"),
            _ => null
        };
}

internal sealed record CurrentFocusSemanticField(
    string AutomationId,
    string ReadableName,
    string ReadableNameCheck,
    string RuntimeStatusCheck);

internal sealed record DatabaseEvidence(
    string ScenarioName,
    string DatabasePath,
    int FocusSessionRows,
    int WebSessionRows,
    int SyncOutboxRows);

internal enum CheckStatus
{
    Pass,
    Fail,
    Warn
}

internal sealed record UiSnapshotOptions(
    string RepositoryRoot,
    string AppPath,
    string OutputRoot,
    string RunDirectory,
    string? DatabasePath,
    UiSnapshotMode Mode,
    TimeSpan Timeout,
    IReadOnlyList<int> ViewportWidths,
    bool AllowServerSync)
{
    private const string AppFileName = "Woong.MonitorStack.Windows.App.exe";

    public static UiSnapshotOptions Parse(string[] args)
    {
        string repositoryRoot = FindRepositoryRoot();
        string? appPath = null;
        string? outputRoot = null;
        string? databasePath = null;
        UiSnapshotMode mode = UiSnapshotMode.EmptyData;
        var timeout = TimeSpan.FromSeconds(20);
        var allowServerSync = false;
        IReadOnlyList<int> viewportWidths = [];

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
                case "--db":
                    databasePath = ReadValue(args, ref index, arg);
                    break;
                case "--mode":
                    string modeValue = ReadValue(args, ref index, arg);
                    if (!Enum.TryParse(modeValue, ignoreCase: true, out mode))
                    {
                        throw new ArgumentException($"Unsupported --mode value: {modeValue}.");
                    }

                    break;
                case "--timeout-seconds":
                    string timeoutValue = ReadValue(args, ref index, arg);
                    if (!int.TryParse(timeoutValue, out int timeoutSeconds) || timeoutSeconds <= 0)
                    {
                        throw new ArgumentException("--timeout-seconds must be a positive integer.");
                    }

                    timeout = TimeSpan.FromSeconds(timeoutSeconds);
                    break;
                case "--viewport-widths":
                    viewportWidths = ParseViewportWidths(ReadValue(args, ref index, arg));
                    break;
                case "--allow-server-sync":
                    allowServerSync = true;
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
        if (mode == UiSnapshotMode.TrackingPipeline)
        {
            databasePath ??= Path.Combine(runDirectory, "tracking-pipeline.db");
        }
        else if (mode == UiSnapshotMode.SampleDashboard)
        {
            databasePath ??= Path.Combine(runDirectory, "sample-dashboard.db");
        }
        else
        {
            databasePath ??= Path.Combine(runDirectory, "empty-data.db");
        }

        return new UiSnapshotOptions(
            repositoryRoot,
            Path.GetFullPath(appPath),
            Path.GetFullPath(outputRoot),
            Path.GetFullPath(runDirectory),
            databasePath is null ? null : Path.GetFullPath(databasePath),
            mode,
            timeout,
            viewportWidths,
            allowServerSync);
    }

    private static IReadOnlyList<int> ParseViewportWidths(string value)
    {
        int[] widths = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(width =>
            {
                if (!int.TryParse(width, out int parsedWidth) || parsedWidth <= 0)
                {
                    throw new ArgumentException($"Invalid viewport width: {width}.");
                }

                return parsedWidth;
            })
            .ToArray();

        if (widths.Length == 0)
        {
            throw new ArgumentException("--viewport-widths must include at least one positive integer.");
        }

        return widths;
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

internal enum UiSnapshotMode
{
    EmptyData = 0,
    TrackingPipeline = 1,
    SampleDashboard = 2
}

internal static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MoveWindow(
        IntPtr hWnd,
        int x,
        int y,
        int width,
        int height,
        [MarshalAs(UnmanagedType.Bool)] bool repaint);
}
