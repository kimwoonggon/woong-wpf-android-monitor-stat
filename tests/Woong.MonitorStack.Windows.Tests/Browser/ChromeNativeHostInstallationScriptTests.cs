namespace Woong.MonitorStack.Windows.Tests.Browser;

public sealed class ChromeNativeHostInstallationScriptTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(
        Path.GetTempPath(),
        $"woong-native-host-script-{Guid.NewGuid():N}");

    [Fact]
    public void InstallScript_RegistersStableChromeNativeHostForCurrentUser()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "install-chrome-native-host.ps1");

        string script = File.ReadAllText(scriptPath);

        Assert.Contains("com.woong.monitorstack.chrome", script, StringComparison.Ordinal);
        Assert.Contains("ChromeExtensionId", script, StringComparison.Ordinal);
        Assert.Contains("NativeMessagingHosts", script, StringComparison.Ordinal);
        Assert.Contains("Woong.MonitorStack.ChromeNativeHost", script, StringComparison.Ordinal);
        Assert.Contains("HKCU", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HKLM", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InstallScript_SupportsChromeEdgeBraveAndFirefoxNativeMessagingPaths()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "install-chrome-native-host.ps1");

        string script = File.ReadAllText(scriptPath);

        Assert.Contains("ValidateSet(\"Chrome\", \"Edge\", \"Brave\", \"Firefox\")", script, StringComparison.Ordinal);
        Assert.Contains(@"Software\Google\Chrome\NativeMessagingHosts", script, StringComparison.Ordinal);
        Assert.Contains(@"Software\Microsoft\Edge\NativeMessagingHosts", script, StringComparison.Ordinal);
        Assert.Contains(@"Software\BraveSoftware\Brave-Browser\NativeMessagingHosts", script, StringComparison.Ordinal);
        Assert.Contains(@"Software\Mozilla\NativeMessagingHosts", script, StringComparison.Ordinal);
        Assert.Contains("allowed_extensions", script, StringComparison.Ordinal);
        Assert.Contains("allowed_origins", script, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeHostRegistryInstaller_UsesHkcuOnly()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "install-chrome-native-host.ps1");

        string script = File.ReadAllText(scriptPath);

        Assert.Contains("HKCU:", script, StringComparison.Ordinal);
        Assert.DoesNotContain("HKLM", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NativeHostRegistryInstaller_DoesNotDeleteParentKey()
    {
        string installScript = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "scripts",
            "install-chrome-native-host.ps1"));
        string uninstallScript = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "scripts",
            "uninstall-chrome-native-host.ps1"));

        Assert.DoesNotContain("Remove-Item -Path $registryRoot", installScript, StringComparison.Ordinal);
        Assert.DoesNotContain("Remove-Item -Path $registryRoot", uninstallScript, StringComparison.Ordinal);
        Assert.Contains("Remove-Item -Path $registryPath", uninstallScript, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeHostRegistryInstaller_BacksUpExistingValue()
    {
        string script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "scripts", "install-chrome-native-host.ps1"));

        Assert.Contains("$previousValue", script, StringComparison.Ordinal);
        Assert.Contains("Get-ItemPropertyValue", script, StringComparison.Ordinal);
        Assert.Contains("previousDefaultValue", script, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeHostRegistryInstaller_RestoresExistingValueOnCleanup()
    {
        string script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "scripts", "uninstall-chrome-native-host.ps1"));

        Assert.Contains("PreviousDefaultValue", script, StringComparison.Ordinal);
        Assert.Contains("Set-Item -Path $registryPath -Value $PreviousDefaultValue", script, StringComparison.Ordinal);
        Assert.Contains("Restored", script, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeHostRegistryInstaller_RemovesOnlyTestHostKeyWhenNoPreviousValue()
    {
        string script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "scripts", "uninstall-chrome-native-host.ps1"));

        Assert.Contains("Remove-Item -Path $registryPath -Recurse -Force", script, StringComparison.Ordinal);
        Assert.Contains("com.woong.monitorstack.chrome_test", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Remove-Item -Path $registryRoot", script, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeHostManifest_AllowedOriginsContainsOnlyTestExtensionId()
    {
        string script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "scripts", "run-chrome-native-message-acceptance.ps1"));

        Assert.Contains("com.woong.monitorstack.chrome_test", script, StringComparison.Ordinal);
        Assert.Contains("$allowed_origins = @(\"chrome-extension://$extensionId/\")", script, StringComparison.Ordinal);
        Assert.Contains("-HostName $hostName", script, StringComparison.Ordinal);
        Assert.Contains(".Replace(\"com.woong.monitorstack.chrome\", $hostName)", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ChromeAcceptance_UsesTempDbNotUserDb()
    {
        string script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "scripts", "run-chrome-native-message-acceptance.ps1"));

        Assert.Contains("$dbPath = Join-Path $runRoot \"chrome-native-acceptance.db\"", script, StringComparison.Ordinal);
        Assert.Contains("$env:WOONG_MONITOR_LOCAL_DB = $dbPath", script, StringComparison.Ordinal);
        Assert.DoesNotContain("windows-local.db", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InstallAndUninstallScripts_DryRunDoesNotWriteRegistry()
    {
        Directory.CreateDirectory(_tempRoot);
        string repoRoot = FindRepositoryRoot();
        string installScript = Path.Combine(repoRoot, "scripts", "install-chrome-native-host.ps1");
        string uninstallScript = Path.Combine(repoRoot, "scripts", "uninstall-chrome-native-host.ps1");
        string installOutput = RunPowerShell(
            installScript,
            $"-Browser Chrome -ChromeExtensionId abcdefghijklabcdefghijklmnop -RepoRoot \"{repoRoot}\" -InstallRoot \"{_tempRoot}\" -DryRun");
        string uninstallOutput = RunPowerShell(
            uninstallScript,
            $"-Browser Chrome -HostName com.woong.monitorstack.chrome_test -HadPreviousValue -PreviousDefaultValue \"C:\\old\\host.json\" -DryRun");

        Assert.Contains("DRY RUN", installOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DRY RUN", uninstallOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HKCU\\Software\\Google\\Chrome\\NativeMessagingHosts\\com.woong.monitorstack.chrome_test", installOutput);
        Assert.Contains("Restored", uninstallOutput);
    }

    [Fact]
    public void NativeHostScripts_RejectBlankHostNameBeforeRegistryPathCanTargetParent()
    {
        Directory.CreateDirectory(_tempRoot);
        string repoRoot = FindRepositoryRoot();
        string installScript = Path.Combine(repoRoot, "scripts", "install-chrome-native-host.ps1");
        string uninstallScript = Path.Combine(repoRoot, "scripts", "uninstall-chrome-native-host.ps1");
        string acceptanceScript = Path.Combine(repoRoot, "scripts", "run-chrome-native-message-acceptance.ps1");

        string installOutput = RunPowerShellExpectingFailure(
            installScript,
            $"-Browser Chrome -ChromeExtensionId abcdefghijklabcdefghijklmnop -RepoRoot \"{repoRoot}\" -InstallRoot \"{_tempRoot}\" -HostName \"\" -DryRun");
        string uninstallOutput = RunPowerShellExpectingFailure(
            uninstallScript,
            "-Browser Chrome -HostName \"\" -DryRun");
        string acceptanceOutput = RunPowerShellExpectingFailure(
            acceptanceScript,
            "-HostName \"\" -CleanupOnly -DryRun");

        Assert.Contains("HostName must be a scoped native messaging host name", installOutput);
        Assert.Contains("HostName must be a scoped native messaging host name", uninstallOutput);
        Assert.Contains("HostName must be a scoped native messaging host name", acceptanceOutput);
        Assert.DoesNotContain("HKCU\\Software\\Google\\Chrome\\NativeMessagingHosts\\", installOutput);
    }

    [Fact]
    public void ChromeAcceptance_RequiresExplicitTempDatabaseForNativeHost()
    {
        string script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "scripts", "run-chrome-native-message-acceptance.ps1"));
        string hostProgram = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "tools", "Woong.MonitorStack.ChromeNativeHost", "Program.cs"));

        Assert.Contains("WOONG_MONITOR_REQUIRE_EXPLICIT_DB", script, StringComparison.Ordinal);
        Assert.Contains("$env:WOONG_MONITOR_REQUIRE_EXPLICIT_DB = \"1\"", script, StringComparison.Ordinal);
        Assert.Contains("WOONG_MONITOR_REQUIRE_EXPLICIT_DB", hostProgram, StringComparison.Ordinal);
        Assert.Contains("requires an explicit --db argument or WOONG_MONITOR_LOCAL_DB", hostProgram, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
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

    private static string RunPowerShell(string scriptPath, string arguments)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
            "powershell.exe",
            $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" {arguments}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = FindRepositoryRoot()
        });
        Assert.NotNull(process);
        process.WaitForExit(30_000);
        string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        Assert.Equal(0, process.ExitCode);
        return output;
    }

    private static string RunPowerShellExpectingFailure(string scriptPath, string arguments)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
            "powershell.exe",
            $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" {arguments}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = FindRepositoryRoot()
        });
        Assert.NotNull(process);
        process.WaitForExit(30_000);
        string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        Assert.NotEqual(0, process.ExitCode);
        return output;
    }
}
