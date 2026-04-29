using System.IO;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class WpfUiAcceptanceScriptTests
{
    [Fact]
    public void UiAcceptanceScript_ComposesSemanticRealStartAndSnapshotEvidence()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-wpf-ui-acceptance.ps1");

        Assert.True(File.Exists(scriptPath), "WPF UI acceptance script must exist.");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("Woong.MonitorStack.Windows.RealStartAcceptance", script);
        Assert.Contains("Woong.MonitorStack.Windows.UiSnapshots", script);
        Assert.Contains("focus_session row was persisted", script);
        Assert.Contains("sync_outbox row was queued", script);
        Assert.Contains("It will not record keystrokes.", script);
        Assert.Contains("It will not capture screen contents as product telemetry.", script);
        Assert.Contains("AllowServerSync", script);
        Assert.Contains("artifacts/wpf-ui-acceptance", script);
        Assert.Contains("WOONG_MONITOR_ACCEPTANCE_MODE", script);
        Assert.Contains("TrackingPipeline", script);
        Assert.Contains("manifest.json", script);
        Assert.Contains("visual-review-prompt.md", script);
        Assert.Contains("$LASTEXITCODE", script);
        Assert.Contains("TrackingPipeline UI snapshot acceptance failed.", script);
    }

    [Fact]
    public void UiSnapshotsTool_SupportsTrackingPipelineSemanticArtifacts()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("--mode", tool);
        Assert.Contains("TrackingPipeline", tool);
        Assert.Contains("StartTrackingButton", tool);
        Assert.Contains("github.com", tool);
        Assert.Contains("chatgpt.com", tool);
        Assert.Contains("05-after-sync.png", tool);
        Assert.Contains("06-settings.png", tool);
        Assert.Contains("recent-web-sessions.png", tool);
        Assert.Contains("manifest.json", tool);
        Assert.Contains("visual-review-prompt.md", tool);
        Assert.Contains("PASS/FAIL/WARN", tool);
    }

    [Fact]
    public void UiSnapshotsTool_ToleratesAutoStartedTrackingPipeline()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("EnsureTrackingRunning", tool);
        Assert.Contains("TrackingStatusText", tool);
        Assert.Contains("Tracking already running", tool);
        Assert.Contains("StartTrackingButton is disabled because auto-start already ran", tool);
    }

    [Fact]
    public void UiSnapshotsTool_DoesNotRequireCodeAsInitialCurrentAppWhenAutoStartAlreadyAdvanced()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("CheckContainsAny", tool);
        Assert.Contains("CurrentAppNameText start", tool);
        Assert.Contains("\"Code.exe\", \"chrome.exe\"", tool);
    }

    [Fact]
    public void UiAcceptanceScript_RequestsRequiredViewportSnapshotMatrix()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-wpf-ui-acceptance.ps1");

        Assert.True(File.Exists(scriptPath), "WPF UI acceptance script must exist.");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("--viewport-widths", script);
        Assert.Contains("1920,1366,1024", script);
    }

    [Fact]
    public void UiSnapshotsTool_SupportsRequiredViewportMatrixAndArtifacts()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("--viewport-widths", tool);
        Assert.Contains("ViewportWidths", tool);
        Assert.Contains("viewport-1920-dashboard.png", tool);
        Assert.Contains("viewport-1366-dashboard.png", tool);
        Assert.Contains("viewport-1024-dashboard.png", tool);
        Assert.Contains("manifest.json", tool);
        Assert.Contains("summary-cards", tool);
        Assert.Contains("recent-sessions", tool);
        Assert.Contains("recent-web-sessions", tool);
        Assert.Contains("live-events", tool);
    }

    [Fact]
    public void UiSnapshotsTool_BringsSectionsIntoViewBeforeCapturingCrops()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("TryBringElementIntoViewBeforeCapture", tool);
        Assert.Contains("ChartArea", tool);
        Assert.Contains("RecentAppSessionsList", tool);
        Assert.Contains("RecentWebSessionsList", tool);
        Assert.Contains("LiveEventsList", tool);
    }

    [Fact]
    public void UiSnapshotsTool_UsesStableDashboardViewAutomationIdsForAcceptanceSelectors()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        string[] stableAcceptanceAutomationIds =
        [
            "SummaryCardsContainer",
            "ChartArea",
            "CurrentActivityPanel",
            "RecentAppSessionsList",
            "RecentWebSessionsList",
            "LiveEventsList"
        ];

        foreach (string automationId in stableAcceptanceAutomationIds)
        {
            Assert.Contains(automationId, tool);
        }
    }

    [Fact]
    public void UiSnapshotsTool_ReportManifestAndVisualPromptIncludeRuntimeSelectorEvidence()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("WriteReport(context, isSuccess);", tool);
        Assert.Contains("WriteManifest(context, isSuccess);", tool);
        Assert.Contains("WriteVisualReviewPrompt(context);", tool);
        Assert.Contains("visual-review-prompt.md", tool);
        Assert.Contains("## PASS/FAIL/WARN Table", tool);
        Assert.Contains("checks = context.Results.Select", tool);

        string[] requiredRuntimeSelectorChecks =
        [
            "RequireExists(mainWindow, \"StartTrackingButton\", context);",
            "RequireExists(mainWindow, \"StopTrackingButton\", context);",
            "RequireExists(mainWindow, \"SyncNowButton\", context);",
            "RequireExists(mainWindow, \"RecentAppSessionsList\", context);",
            "RequireExists(mainWindow, \"RecentWebSessionsList\", context);"
        ];

        foreach (string requiredRuntimeSelectorCheck in requiredRuntimeSelectorChecks)
        {
            Assert.Contains(requiredRuntimeSelectorCheck, tool);
        }
    }

    [Fact]
    public void UiSnapshotsTool_TrackingPipelineVerifiesLiveEventRuntimeSemantics()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("SelectTabIfAvailable(mainWindow, \"LiveEventsTab\", \"Live Event Log\", context);", tool);
        Assert.Contains("CaptureElementIfAvailable(mainWindow, \"LiveEventsList\", \"live-events.png\", context);", tool);
        Assert.Contains("ReadLiveEventLogTextAcrossPages(mainWindow, context)", tool);
        Assert.Contains("DetailsNextPageButton", tool);
        Assert.Contains("GetElementVisibleText(window, \"LiveEventsList\")", tool);

        string[] requiredLiveEventChecks =
        [
            "context.CheckContains(\"LiveEventLog shows tracking started\", \"Tracking started\", liveEventEvidenceText);",
            "context.CheckContains(\"LiveEventLog shows focus session semantics\", \"FocusSession\", liveEventEvidenceText);",
            "context.CheckContains(\"LiveEventLog shows web session semantics\", \"Web\", liveEventEvidenceText);",
            "context.CheckContains(\"LiveEventLog shows github.com web event\", \"github.com\", liveEventEvidenceText);",
            "context.CheckContains(\"LiveEventLog shows chatgpt.com web event\", \"chatgpt.com\", liveEventEvidenceText);",
            "context.CheckContains(\"LiveEventLog shows outbox semantics\", \"Outbox\", liveEventEvidenceText);",
            "context.CheckContains(\"LiveEventLog shows sync skipped\", \"Sync skipped\", liveEventEvidenceText);",
            "context.CheckContains(\"LiveEventLog shows tracking stopped\", \"Tracking stopped\", liveEventEvidenceText);"
        ];

        foreach (string requiredLiveEventCheck in requiredLiveEventChecks)
        {
            Assert.Contains(requiredLiveEventCheck, tool);
        }
    }

    [Fact]
    public void UiSnapshotsTool_ManifestIncludesViewportAndSkippedScreenshotReasons()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("viewportWidths", tool);
        Assert.Contains("skippedScreenshotReasons", tool);
        Assert.Contains("ViewportWidth", tool);
        Assert.Contains("Reason", tool);
    }

    [Fact]
    public void UiSnapshotsTool_TrackingPipelineQueriesTempSqliteDatabase()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");
        string projectPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Woong.MonitorStack.Windows.UiSnapshots.csproj");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        Assert.True(File.Exists(projectPath), "WPF UI snapshot project must exist.");
        string tool = File.ReadAllText(toolPath);
        string project = File.ReadAllText(projectPath);

        Assert.Contains("Microsoft.Data.Sqlite", project);
        Assert.Contains("VerifyTrackingPipelineDatabase", tool);
        Assert.Contains("CountRows", tool);
        Assert.Contains("focus_session", tool);
        Assert.Contains("web_session", tool);
        Assert.Contains("sync_outbox", tool);
    }

    [Fact]
    public void UiSnapshotsTool_ReportIncludesTrackingPipelineSqliteEvidence()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("## SQLite Evidence", tool);
        Assert.Contains("TrackingPipeline focus_session rows", tool);
        Assert.Contains("TrackingPipeline web_session rows", tool);
        Assert.Contains("TrackingPipeline sync_outbox rows", tool);
    }

    [Fact]
    public void UiSnapshotsTool_ManifestIncludesTrackingPipelineSqliteEvidence()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("databaseEvidence", tool);
        Assert.Contains("FocusSessionRows", tool);
        Assert.Contains("WebSessionRows", tool);
        Assert.Contains("SyncOutboxRows", tool);
        Assert.Contains("DatabasePath", tool);
    }

    [Fact]
    public void UiSnapshotsTool_EmptyDataModeDisablesAutoStartAndVerifiesZeroSqliteRows()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("empty-data.db", tool);
        Assert.Contains("WOONG_MONITOR_AUTO_START_TRACKING", tool);
        Assert.Contains("VerifyEmptyDataDatabase", tool);
        Assert.Contains("EmptyData focus_session rows", tool);
        Assert.Contains("EmptyData web_session rows", tool);
        Assert.Contains("EmptyData sync_outbox rows", tool);
        Assert.Contains("= 0", tool);
    }

    [Fact]
    public void UiSnapshotsTool_SupportsSampleDashboardModeAcceptance()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-ui-snapshots.ps1");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        Assert.True(File.Exists(scriptPath), "WPF UI snapshot script must exist.");
        string tool = File.ReadAllText(toolPath);
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("SampleDashboard", tool);
        Assert.Contains("RunSampleDashboardAcceptance", tool);
        Assert.Contains("SampleDashboard shows Chrome", tool);
        Assert.Contains("SampleDashboard shows github.com", tool);
        Assert.Contains("sample-dashboard.db", tool);
        Assert.Contains("--mode", script);
        Assert.Contains("SampleDashboard", script);
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

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
