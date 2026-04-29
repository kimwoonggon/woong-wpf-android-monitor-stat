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
