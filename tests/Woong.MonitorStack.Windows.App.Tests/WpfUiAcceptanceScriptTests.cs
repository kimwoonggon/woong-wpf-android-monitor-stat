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
