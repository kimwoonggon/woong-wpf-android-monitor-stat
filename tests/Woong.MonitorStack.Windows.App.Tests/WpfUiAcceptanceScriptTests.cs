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
    public void UiSnapshotsTool_ReportAndManifestIncludeMinimumSizeReachabilityEvidence()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("## Minimum Size Reachability Evidence", tool);
        Assert.Contains("| Viewport | Section | AutomationId | Screenshot | Status |", tool);
        Assert.Contains("minimumSizeReachabilityEvidence", tool);
        Assert.Contains("context.MinimumSizeReachabilityEvidence.Select", tool);
        Assert.Contains("viewport = evidence.Viewport", tool);
        Assert.Contains("section = evidence.Section", tool);
        Assert.Contains("automationId = evidence.AutomationId", tool);
        Assert.Contains("screenshot = evidence.Screenshot", tool);
        Assert.Contains("status = evidence.Status.ToString()", tool);
        Assert.Contains("VerifyMinimumSizeReachability", tool);
        Assert.Contains("RecordMinimumSizeReachability", tool);
        Assert.Contains("1024x768", tool);

        string[] requiredMinimumSizeSelectors =
        [
            "HeaderStatusBar",
            "ControlBar",
            "CurrentActivityPanel",
            "AppSessionsTab",
            "RecentAppSessionsList",
            "WebSessionsTab",
            "RecentWebSessionsList",
            "LiveEventsTab",
            "LiveEventsList",
            "SettingsTab",
            "SettingsPanel"
        ];

        foreach (string selector in requiredMinimumSizeSelectors)
        {
            Assert.Contains(selector, tool);
        }
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
    public void UiSnapshotsTool_ReportAndManifestIncludeHeaderBadgeSemanticEvidence()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("VerifyHeaderBadgeSemanticNames", tool);
        Assert.Contains("Header TrackingStatusBadge readable name", tool);
        Assert.Contains("Header SyncStatusBadge readable name", tool);
        Assert.Contains("Header PrivacyStatusBadge readable name", tool);
        Assert.Contains("TrackingStatusBadge", tool);
        Assert.Contains("SyncStatusBadge", tool);
        Assert.Contains("PrivacyStatusBadge", tool);
        Assert.Contains("checks = context.Results.Select", tool);
        Assert.Contains("## PASS/FAIL/WARN Table", tool);
    }

    [Fact]
    public void UiSnapshotsTool_ReportAndManifestIncludeCurrentFocusSemanticEvidence()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("VerifyCurrentFocusSemanticEvidence", tool);
        Assert.Contains("Current Focus CurrentAppNameText readable name", tool);
        Assert.Contains("Current Focus CurrentProcessNameText readable name", tool);
        Assert.Contains("Current Focus CurrentWindowTitleText readable name", tool);
        Assert.Contains("Current Focus CurrentBrowserDomainText readable name", tool);
        Assert.Contains("Current Focus CurrentSessionDurationText readable name", tool);
        Assert.Contains("Current Focus LastPollTimeText readable name", tool);
        Assert.Contains("Current Focus LastDbWriteTimeText readable name", tool);
        Assert.Contains("Current Focus LastPersistedSessionText readable name", tool);
        Assert.Contains("Current Focus LastSyncStatusText readable name", tool);
        Assert.Contains("Current Focus CurrentAppNameText runtime status", tool);
        Assert.Contains("Current Focus LastSyncStatusText runtime status", tool);
        Assert.Contains("GetElementName(window, automationId)", tool);
        Assert.Contains("GetElementText(window, automationId)", tool);
        Assert.Contains("checks = context.Results.Select", tool);
        Assert.Contains("## PASS/FAIL/WARN Table", tool);
    }

    [Fact]
    public void UiSnapshotsTool_ReportIncludesHumanReadableCurrentFocusSemanticTable()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("## Current Focus Runtime Semantic Evidence", tool);
        Assert.Contains("| Field | AutomationId | Readable Name | Runtime Value | Status |", tool);
        Assert.Contains("CurrentFocusSemanticEvidence", tool);
        Assert.Contains("Current app", tool);
        Assert.Contains("CurrentAppNameText", tool);
        Assert.Contains("Current process", tool);
        Assert.Contains("CurrentProcessNameText", tool);
        Assert.Contains("Current window title", tool);
        Assert.Contains("CurrentWindowTitleText", tool);
        Assert.Contains("Current browser domain", tool);
        Assert.Contains("CurrentBrowserDomainText", tool);
        Assert.Contains("Current session duration", tool);
        Assert.Contains("CurrentSessionDurationText", tool);
        Assert.Contains("Last poll time", tool);
        Assert.Contains("LastPollTimeText", tool);
        Assert.Contains("Last DB write time", tool);
        Assert.Contains("LastDbWriteTimeText", tool);
        Assert.Contains("Last persisted session", tool);
        Assert.Contains("LastPersistedSessionText", tool);
        Assert.Contains("Sync state", tool);
        Assert.Contains("LastSyncStatusText", tool);
    }

    [Fact]
    public void UiSnapshotsTool_ManifestIncludesGroupedCurrentFocusRuntimeEvidence()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("currentFocusRuntimeEvidence", tool);
        Assert.Contains("context.CurrentFocusSemanticEvidence.Select", tool);
        Assert.Contains("field = evidence.Field", tool);
        Assert.Contains("readableName = evidence.ReadableName", tool);
        Assert.Contains("automationId = evidence.AutomationId", tool);
        Assert.Contains("runtimeValue = evidence.RuntimeValue", tool);
        Assert.Contains("status = evidence.Status.ToString()", tool);
        Assert.Contains("checks = context.Results.Select", tool);

        string[] requiredRuntimeEvidenceFields =
        [
            "Current app",
            "CurrentAppNameText",
            "Current process",
            "CurrentProcessNameText",
            "Current window title",
            "CurrentWindowTitleText",
            "Current browser domain",
            "CurrentBrowserDomainText",
            "Current session duration",
            "CurrentSessionDurationText",
            "Last poll time",
            "LastPollTimeText",
            "Last DB write time",
            "LastDbWriteTimeText",
            "Last persisted session",
            "LastPersistedSessionText",
            "Sync state",
            "LastSyncStatusText"
        ];

        foreach (string requiredRuntimeEvidenceField in requiredRuntimeEvidenceFields)
        {
            Assert.Contains(requiredRuntimeEvidenceField, tool);
        }
    }

    [Fact]
    public void UiSnapshotsTool_ReportAndManifestIncludeGroupedSectionScreenshotEvidence()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("## Section Screenshot Evidence", tool);
        Assert.Contains("| Section | AutomationId | Screenshot | Skipped Reason | Status |", tool);
        Assert.Contains("sectionScreenshotEvidence", tool);
        Assert.Contains("context.SectionScreenshotEvidence.Select", tool);
        Assert.Contains("section = evidence.Section", tool);
        Assert.Contains("automationId = evidence.AutomationId", tool);
        Assert.Contains("screenshot = evidence.Screenshot", tool);
        Assert.Contains("skippedReason = evidence.SkippedReason", tool);
        Assert.Contains("status = evidence.Status.ToString()", tool);

        string[] requiredSectionEvidence =
        [
            "Current activity",
            "CurrentActivityPanel",
            "current-activity.png",
            "Summary cards",
            "SummaryCardsContainer",
            "summary-cards.png",
            "Sessions",
            "RecentAppSessionsList",
            "recent-sessions.png",
            "Web sessions",
            "RecentWebSessionsList",
            "recent-web-sessions.png",
            "Live events",
            "LiveEventsList",
            "live-events.png",
            "Chart area",
            "ChartArea",
            "chart-area.png",
            "Settings",
            "SettingsTab",
            "06-settings.png"
        ];

        foreach (string requiredSection in requiredSectionEvidence)
        {
            Assert.Contains(requiredSection, tool);
        }
    }

    [Fact]
    public void UiSnapshotsTool_ReportAndManifestIncludeGroupedControlActionEvidence()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("## Control Action Evidence", tool);
        Assert.Contains("| Action | AutomationId | Result | Status |", tool);
        Assert.Contains("controlActionEvidence", tool);
        Assert.Contains("context.ControlActionEvidence.Select", tool);
        Assert.Contains("action = evidence.Action", tool);
        Assert.Contains("automationId = evidence.AutomationId", tool);
        Assert.Contains("result = evidence.Result", tool);
        Assert.Contains("status = evidence.Status.ToString()", tool);
        Assert.Contains("RecordControlAction", tool);
        Assert.Contains("Start tracking", tool);
        Assert.Contains("StartTrackingButton", tool);
        Assert.Contains("Stop tracking", tool);
        Assert.Contains("StopTrackingButton", tool);
        Assert.Contains("Sync local-only", tool);
        Assert.Contains("Sync enabled", tool);
        Assert.Contains("SyncNowButton", tool);
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
    public void UiSnapshotsTool_ReportAndManifestIncludeGroupedSqliteRuntimeEvidence()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("## SQLite Runtime Evidence", tool);
        Assert.Contains("| Store | Expected | Actual Rows | Status |", tool);
        Assert.Contains("sqliteRuntimeEvidence", tool);
        Assert.Contains("context.SqliteRuntimeEvidence.Select", tool);
        Assert.Contains("store = evidence.Store", tool);
        Assert.Contains("expected = evidence.Expected", tool);
        Assert.Contains("actualRows = evidence.ActualRows", tool);
        Assert.Contains("status = evidence.Status.ToString()", tool);
        Assert.Contains("RecordSqliteRuntimeEvidence", tool);
        Assert.Contains("focus_session", tool);
        Assert.Contains("web_session", tool);
        Assert.Contains("sync_outbox", tool);
        Assert.Contains("TrackingPipeline focus_session rows", tool);
        Assert.Contains("TrackingPipeline web_session rows", tool);
        Assert.Contains("TrackingPipeline sync_outbox rows", tool);
    }

    [Fact]
    public void UiSnapshotsTool_ReportAndManifestIncludeGroupedBrowserDomainPrivacyEvidence()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.UiSnapshots", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF UI snapshot tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("## Browser Domain Privacy Evidence", tool);
        Assert.Contains("| Claim | Expected | Actual | Status |", tool);
        Assert.Contains("browserDomainPrivacyEvidence", tool);
        Assert.Contains("context.BrowserDomainPrivacyEvidence.Select", tool);
        Assert.Contains("claim = evidence.Claim", tool);
        Assert.Contains("expected = evidence.Expected", tool);
        Assert.Contains("actual = evidence.Actual", tool);
        Assert.Contains("status = evidence.Status.ToString()", tool);
        Assert.Contains("RecordBrowserDomainPrivacyEvidence", tool);
        Assert.Contains("VerifyBrowserDomainPrivacyEvidence", tool);
        Assert.Contains("Domain github.com persisted", tool);
        Assert.Contains("Domain chatgpt.com persisted", tool);
        Assert.Contains("Full URL values absent", tool);
        Assert.Contains("Page title values absent", tool);
        Assert.Contains("Page content storage absent", tool);
        Assert.Contains("CountRowsWhere", tool);
        Assert.Contains("ColumnExists", tool);
    }

    [Fact]
    public void RealStartAcceptanceTool_WritesLocalDbPersistenceEvidenceArtifacts()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.RealStartAcceptance", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF real-start acceptance tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("real-start-report.md", tool);
        Assert.Contains("real-start-manifest.json", tool);
        Assert.Contains("WriteRealStartArtifacts", tool);
        Assert.Contains("## RealStart Local DB Evidence", tool);
        Assert.Contains("| Claim | Expected | Actual | Status |", tool);
        Assert.Contains("realStartEvidence", tool);
        Assert.Contains("evidence.Select", tool);
        Assert.Contains("claim = item.Claim", tool);
        Assert.Contains("expected = item.Expected", tool);
        Assert.Contains("actual = item.Actual", tool);
        Assert.Contains("status = item.Status.ToString()", tool);
        Assert.Contains("focus_session persisted", tool);
        Assert.Contains("sync_outbox queued", tool);
        Assert.Contains("latest focus session app/process readable", tool);
        Assert.Contains("server sync disabled unless explicitly allowed", tool);
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
