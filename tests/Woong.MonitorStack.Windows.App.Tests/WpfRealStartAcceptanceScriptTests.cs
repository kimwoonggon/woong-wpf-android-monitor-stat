using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class WpfRealStartAcceptanceScriptTests
{
    [Fact]
    public void RealStartScript_DocumentsPrivacyWarningAndSafeDefaults()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-wpf-real-start-acceptance.ps1");

        Assert.True(File.Exists(scriptPath), "RealStart acceptance script must exist.");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("This will observe foreground window metadata for local testing.", script);
        Assert.Contains("It will not record keystrokes.", script);
        Assert.Contains("It will not capture screen contents.", script);
        Assert.Contains("temp DB", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AllowServerSync", script);
        Assert.Contains("WOONG_MONITOR_LOCAL_DB", script);
    }

    [Fact]
    public void RealStartScript_ExposesDatabaseOnlyWebSessionEvidenceMode()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-wpf-real-start-acceptance.ps1");

        Assert.True(File.Exists(scriptPath), "RealStart acceptance script must exist.");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("[switch]$VerifyDatabaseOnly", script);
        Assert.Contains("[string]$DatabasePath", script);
        Assert.Contains("--verify-db-only", script);
        Assert.Contains("does not launch browsers or capture external app screenshots", script);
    }

    [Fact]
    public void RealStartTool_VerifiesPersistedFocusSessionAppearsInRecentAppSessionsList()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.RealStartAcceptance", "Program.cs");

        Assert.True(File.Exists(toolPath), "RealStart acceptance tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("RecentAppSessionsList", tool);
        Assert.Contains("VerifyRecentAppSessionVisible", tool);
        Assert.Contains("ReadLatestFocusSessionProcessName", tool);
        Assert.Contains("persisted focus session appeared in RecentAppSessionsList", tool);
    }

    [Fact]
    public void RealStartTool_ToleratesAutoStartedTracking()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.RealStartAcceptance", "Program.cs");

        Assert.True(File.Exists(toolPath), "RealStart acceptance tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("EnsureTrackingRunning", tool);
        Assert.Contains("TrackingStatusText", tool);
        Assert.Contains("Tracking already running", tool);
        Assert.Contains("StartTrackingButton is disabled because auto-start already ran", tool);
    }

    [Fact]
    public void RealStartTool_VerifyDbOnlyReportsDomainOnlyWebSessionDurationEvidence()
    {
        string repoRoot = FindRepositoryRoot();
        string toolProject = Path.Combine(
            repoRoot,
            "tools",
            "Woong.MonitorStack.Windows.RealStartAcceptance",
            "Woong.MonitorStack.Windows.RealStartAcceptance.csproj");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-realstart-db-only-{Guid.NewGuid():N}");
        string databasePath = Path.Combine(tempRoot, "real-start.db");

        try
        {
            Directory.CreateDirectory(tempRoot);
            SeedDomainOnlyWebSessionDatabase(databasePath);

            ProcessResult result = RunProcess(
                repoRoot,
                "dotnet",
                $"run --project \"{toolProject}\" --no-restore -- --verify-db-only --db \"{databasePath}\"");

            Assert.Equal(0, result.ExitCode);

            string manifestPath = Path.Combine(tempRoot, "real-start-manifest.json");
            string reportPath = Path.Combine(tempRoot, "real-start-report.md");
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            JsonElement root = manifest.RootElement;

            Assert.Equal("PASS", root.GetProperty("status").GetString());
            Assert.Equal("database-only", root.GetProperty("mode").GetString());
            Assert.False(root.GetProperty("allowServerSync").GetBoolean());
            Assert.Contains(
                root.GetProperty("realStartEvidence").EnumerateArray(),
                item =>
                    item.GetProperty("claim").GetString() == "domain-only web_session duration persisted" &&
                    item.GetProperty("actual").GetString() == "github.com:120000ms");
            Assert.Contains(
                root.GetProperty("realStartEvidence").EnumerateArray(),
                item =>
                    item.GetProperty("claim").GetString() == "domain-only web_session privacy" &&
                    item.GetProperty("status").GetString() == "Pass");

            string report = File.ReadAllText(reportPath);
            Assert.Contains("domain-only web_session duration persisted", report);
            Assert.Contains("github.com:120000ms", report);
            Assert.DoesNotContain("https://github.com/org/private?token=secret", report, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Private Repository Title", report, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void RealStartTool_VerifyDbOnlyFailsWhenWebSessionStoresFullUrlOrPageTitle()
    {
        string repoRoot = FindRepositoryRoot();
        string toolProject = Path.Combine(
            repoRoot,
            "tools",
            "Woong.MonitorStack.Windows.RealStartAcceptance",
            "Woong.MonitorStack.Windows.RealStartAcceptance.csproj");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-realstart-db-only-privacy-fail-{Guid.NewGuid():N}");
        string databasePath = Path.Combine(tempRoot, "real-start.db");

        try
        {
            Directory.CreateDirectory(tempRoot);
            SeedDomainOnlyWebSessionDatabase(
                databasePath,
                url: "https://github.com/org/private?token=secret",
                pageTitle: "Private Repository Title");

            ProcessResult result = RunProcess(
                repoRoot,
                "dotnet",
                $"run --project \"{toolProject}\" --no-restore -- --verify-db-only --db \"{databasePath}\"");

            Assert.Equal(1, result.ExitCode);

            string manifestPath = Path.Combine(tempRoot, "real-start-manifest.json");
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            JsonElement root = manifest.RootElement;

            Assert.Equal("FAIL", root.GetProperty("status").GetString());
            Assert.Contains(
                root.GetProperty("realStartEvidence").EnumerateArray(),
                item =>
                    item.GetProperty("claim").GetString() == "domain-only web_session privacy" &&
                    item.GetProperty("status").GetString() == "Fail");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void RealStartTool_CleansUpThroughExplicitExitAndReportsFallbackKills()
    {
        string repoRoot = FindRepositoryRoot();
        string toolPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.Windows.RealStartAcceptance", "Program.cs");

        Assert.True(File.Exists(toolPath), "WPF real-start acceptance tool must exist.");
        string tool = File.ReadAllText(toolPath);

        Assert.Contains("WpfAppCleanupCoordinator.Cleanup", tool);
        Assert.Contains("RequestExplicitExitFromSettings", tool);
        Assert.Contains("ExitApplicationButton", tool);
        Assert.Contains("realStartCleanupEvidence", tool);
        Assert.Contains("## RealStart Cleanup Evidence", tool);
        Assert.Contains("ExplicitExitAttempted", tool);
        Assert.Contains("WasKilled", tool);
        Assert.Contains("X-close-to-tray", tool);
        Assert.DoesNotContain("CloseMainWindow", tool);
    }

    [Fact]
    public void RealStartScript_DocumentsExplicitExitVersusCloseToTrayCleanup()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-wpf-real-start-acceptance.ps1");

        Assert.True(File.Exists(scriptPath), "RealStart acceptance script must exist.");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("Exit app", script);
        Assert.Contains("X close", script);
        Assert.Contains("close-to-tray", script);
        Assert.Contains("realStartCleanupEvidence", script);
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

    private static void SeedDomainOnlyWebSessionDatabase(
        string databasePath,
        string? url = null,
        string? pageTitle = null)
    {
        string connectionString = $"Data Source={databasePath};Pooling=False";
        var focusRepository = new SqliteFocusSessionRepository(connectionString);
        var webRepository = new SqliteWebSessionRepository(connectionString);
        focusRepository.Initialize();
        webRepository.Initialize();

        var startedAtUtc = new DateTimeOffset(2026, 5, 2, 8, 0, 0, TimeSpan.Zero);
        var endedAtUtc = startedAtUtc.AddMinutes(2);
        focusRepository.Save(FocusSession.FromUtc(
            "focus-realstart-web",
            "real-start-local",
            "chrome.exe",
            startedAtUtc,
            endedAtUtc,
            "Asia/Seoul",
            isIdle: false,
            source: "foreground_window",
            processName: "chrome.exe",
            windowTitle: "Masked by default"));
        webRepository.Save(new WebSession(
            "focus-realstart-web",
            "Chrome",
            url: url,
            domain: "github.com",
            pageTitle: pageTitle,
            TimeRange.FromUtc(startedAtUtc, endedAtUtc),
            captureMethod: "ui_automation",
            captureConfidence: "domain",
            isPrivateOrUnknown: false));
    }

    private static ProcessResult RunProcess(string workingDirectory, string fileName, string arguments)
    {
        using Process process = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException($"Could not start {fileName}.");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(process.ExitCode, output, error);
    }

    private sealed record ProcessResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
