namespace Woong.MonitorStack.Architecture.Tests;

public sealed class LocalIntegratedDashboardRunbookTests
{
    [Fact]
    public void LocalIntegratedDashboardScript_DocumentsRealWpfAndroidToBlazorFlow()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-local-integrated-dashboard.ps1");
        string toolProjectPath = Path.Combine(
            repoRoot,
            "tools",
            "Woong.MonitorStack.LocalDashboardBridge",
            "Woong.MonitorStack.LocalDashboardBridge.csproj");
        string docsPath = Path.Combine(repoRoot, "docs", "local-integrated-dashboard.md");

        Assert.True(File.Exists(scriptPath), "Local integrated dashboard script must exist.");
        Assert.True(File.Exists(toolProjectPath), "Local dashboard bridge tool project must exist.");
        Assert.True(File.Exists(docsPath), "Local integrated dashboard runbook must exist.");

        string script = File.ReadAllText(scriptPath);
        string docs = File.ReadAllText(docsPath);

        Assert.Contains("start-server-postgres.ps1", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Woong.MonitorStack.LocalDashboardBridge", script, StringComparison.Ordinal);
        Assert.Contains("adb", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("woong-monitor.db", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("windows-local.db", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/dashboard?userId=", script, StringComparison.Ordinal);
        Assert.Contains("WPF SQLite", docs, StringComparison.Ordinal);
        Assert.Contains("Android emulator Room", docs, StringComparison.Ordinal);
        Assert.Contains("PostgreSQL", docs, StringComparison.Ordinal);
        Assert.Contains("Blazor", docs, StringComparison.Ordinal);
        Assert.Contains("```mermaid", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LocalDashboardBridge", docs, StringComparison.Ordinal);
        Assert.Contains("Blazor does", docs, StringComparison.Ordinal);
        Assert.Contains("not poll WPF SQLite or Android Room directly", docs, StringComparison.Ordinal);
        Assert.Contains("1s", docs, StringComparison.Ordinal);
        Assert.Contains("5s", docs, StringComparison.Ordinal);
        Assert.Contains("10s", docs, StringComparison.Ordinal);
        Assert.Contains("1h", docs, StringComparison.Ordinal);
        Assert.Contains("does not read typed text", docs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LocalIntegratedDashboardScript_VerifiesDashboardDataPresenceAfterBridgeUpload()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-local-integrated-dashboard.ps1");

        Assert.True(File.Exists(scriptPath), "Local integrated dashboard script must exist.");

        string script = File.ReadAllText(scriptPath);
        int bridgeUploadIndex = script.IndexOf("Uploading local client data through API DTOs", StringComparison.Ordinal);
        int verificationIndex = script.IndexOf("$verification = Test-IntegratedDashboardDataPresence", StringComparison.Ordinal);

        Assert.True(bridgeUploadIndex >= 0, "Script must run the local dashboard bridge.");
        Assert.True(verificationIndex > bridgeUploadIndex, "Script must verify dashboard data presence after the bridge upload.");
        Assert.Contains("/api/dashboard/integrated", script, StringComparison.Ordinal);
        Assert.Contains("currentApps", script, StringComparison.Ordinal);
        Assert.Contains("$SkipWindows", script, StringComparison.Ordinal);
        Assert.Contains("$SkipAndroid", script, StringComparison.Ordinal);
        Assert.Contains("Missing required Windows data", script, StringComparison.Ordinal);
        Assert.Contains("Missing required Android data", script, StringComparison.Ordinal);
        Assert.Contains("Windows data present", script, StringComparison.Ordinal);
        Assert.Contains("Android data present", script, StringComparison.Ordinal);
        Assert.Contains("Integrated dashboard verification failed", script, StringComparison.Ordinal);
        Assert.Contains("report.md", script, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Woong.MonitorStack.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
