namespace Woong.MonitorStack.Windows.Tests.Browser;

public sealed class ChromeNativeMessagingAcceptanceScriptTests
{
    [Fact]
    public void AcceptanceScript_UsesExtensionNativeHostSqlitePipelineWithoutFlaUiOrAddressBarScraping()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "run-chrome-native-message-acceptance.ps1");

        Assert.True(File.Exists(scriptPath), "Chrome native messaging acceptance script must exist.");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("--load-extension", script, StringComparison.Ordinal);
        Assert.Contains("--user-data-dir", script, StringComparison.Ordinal);
        Assert.Contains("extensions\\chrome", script, StringComparison.Ordinal);
        Assert.Contains("install-chrome-native-host.ps1", script, StringComparison.Ordinal);
        Assert.Contains("browser_raw_event", script, StringComparison.Ordinal);
        Assert.Contains("web_session", script, StringComparison.Ordinal);
        Assert.Contains("github.html", script, StringComparison.Ordinal);
        Assert.Contains("chatgpt.html", script, StringComparison.Ordinal);
        Assert.Contains("artifacts/chrome-native-acceptance", script, StringComparison.Ordinal);
        Assert.DoesNotContain("FlaUI", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AddressBar", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AutomationElement", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AcceptanceScript_CreatesDeterministicExtensionIdAndCleansUpRegistryAndProfile()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "run-chrome-native-message-acceptance.ps1");

        string script = File.ReadAllText(scriptPath);

        Assert.Contains("New-DeterministicExtensionKey", script, StringComparison.Ordinal);
        Assert.Contains("Get-ChromiumExtensionId", script, StringComparison.Ordinal);
        Assert.Contains("allowed_origins", script, StringComparison.Ordinal);
        Assert.Contains("Remove-Item -Recurse -Force $tempProfile", script, StringComparison.Ordinal);
        Assert.Contains("uninstall-chrome-native-host.ps1", script, StringComparison.Ordinal);
        Assert.Contains("-HadPreviousValue:$previousHostKeyExisted", script, StringComparison.Ordinal);
        Assert.Contains("-PreviousDefaultValue \"$previousDefaultValue\"", script, StringComparison.Ordinal);
        Assert.Contains("report.md", script, StringComparison.Ordinal);
        Assert.Contains("manifest.json", script, StringComparison.Ordinal);
        Assert.Contains("full URL is not persisted", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Safety, dry-run, or cleanup-only path completed", script, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptanceScript_CleansUpOnlySandboxChromeProfileProcesses()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "run-chrome-native-message-acceptance.ps1");

        string script = File.ReadAllText(scriptPath);

        Assert.Contains("Stop-SandboxChromeProcesses", script, StringComparison.Ordinal);
        Assert.Contains("--user-data-dir=$tempProfile", script, StringComparison.Ordinal);
        Assert.Contains("Win32_Process", script, StringComparison.Ordinal);
        Assert.Contains("$tempProfile", script, StringComparison.Ordinal);
        Assert.Contains("Stopping only sandbox Chrome processes for temp profile", script, StringComparison.Ordinal);
        Assert.Contains("Existing user Chrome windows/profiles are outside the sandbox", script, StringComparison.Ordinal);
        Assert.Contains("tempProfilePath", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Get-Process chrome", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("$chromeProcessIdsBefore", script, StringComparison.Ordinal);
        Assert.DoesNotContain("taskkill", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Stop-Process -Name chrome", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Stop-Process -ProcessName chrome", script, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            if (File.Exists(Path.Combine(directory, "Woong.MonitorStack.sln")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
