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

    [Fact]
    public void AcceptanceScript_RefusesToCleanupNonTempChromeProfiles()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "run-chrome-native-message-acceptance.ps1");

        string script = File.ReadAllText(scriptPath);

        Assert.Contains("Assert-SandboxChromeProfilePath", script, StringComparison.Ordinal);
        Assert.Contains("woong-chrome-native-", script, StringComparison.Ordinal);
        Assert.Contains("Refusing to stop Chrome because the profile path is not the acceptance temp sandbox", script, StringComparison.Ordinal);
        Assert.Contains("Assert-SandboxChromeProfilePath $ProfilePath", script, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptanceScript_WritesChromeAndNativeHostDiagnosticsToArtifacts()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-chrome-native-message-acceptance.ps1");
        string hostProgramPath = Path.Combine(repoRoot, "tools", "Woong.MonitorStack.ChromeNativeHost", "Program.cs");

        string script = File.ReadAllText(scriptPath);
        string hostProgram = File.ReadAllText(hostProgramPath);

        Assert.Contains("$chromeLogPath = Join-Path $runRoot \"chrome.log\"", script, StringComparison.Ordinal);
        Assert.Contains("$nativeHostLogPath = Join-Path $runRoot \"native-host.log\"", script, StringComparison.Ordinal);
        Assert.Contains("--log-file=$chromeLogPath", script, StringComparison.Ordinal);
        Assert.Contains("$env:WOONG_MONITOR_NATIVE_HOST_LOG = $nativeHostLogPath", script, StringComparison.Ordinal);
        Assert.Contains("chromeLogPath", script, StringComparison.Ordinal);
        Assert.Contains("nativeHostLogPath", script, StringComparison.Ordinal);
        Assert.Contains("WOONG_MONITOR_NATIVE_HOST_LOG", hostProgram, StringComparison.Ordinal);
        Assert.Contains("WriteDiagnostic", hostProgram, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptanceScript_QuotesHostResolverRulesSoChromeReceivesOneArgument()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "run-chrome-native-message-acceptance.ps1");

        string script = File.ReadAllText(scriptPath);

        Assert.Contains("$quotedResolverRules = \"--host-resolver-rules=`\"$resolverRules`\"\"", script, StringComparison.Ordinal);
        Assert.Contains("$quotedResolverRules", script, StringComparison.Ordinal);
        Assert.DoesNotContain("\"--host-resolver-rules=$resolverRules\"", script, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptanceScript_UsesChromeForTestingBecauseStableChromeBlocksLoadExtension()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-chrome-native-message-acceptance.ps1");
        string installScriptPath = Path.Combine(repoRoot, "scripts", "install-chrome-for-testing.ps1");

        string script = File.ReadAllText(scriptPath);

        Assert.True(File.Exists(installScriptPath), "Chrome acceptance needs a local Chrome for Testing installer.");
        Assert.Contains("InstallChromeForTesting", script, StringComparison.Ordinal);
        Assert.Contains("install-chrome-for-testing.ps1", script, StringComparison.Ordinal);
        Assert.Contains("if ([string]::IsNullOrWhiteSpace($RepoRoot))", File.ReadAllText(installScriptPath), StringComparison.Ordinal);
        Assert.Contains("Chrome for Testing", script, StringComparison.Ordinal);
        Assert.Contains("Official Google Chrome stable builds", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("block command-line unpacked extension loading", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".cache/chrome-for-testing", File.ReadAllText(Path.Combine(repoRoot, ".gitignore")), StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptanceScript_DoesNotFallbackToUserInstalledChromeUnlessExplicitlyAllowed()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "run-chrome-native-message-acceptance.ps1");

        string script = File.ReadAllText(scriptPath);

        Assert.Contains("AllowInstalledChromeFallback", script, StringComparison.Ordinal);
        Assert.Contains("if ($AllowInstalledChromeFallback)", script, StringComparison.Ordinal);
        Assert.Contains("Chrome for Testing executable was not found", script, StringComparison.Ordinal);
        Assert.Contains("Use -AllowInstalledChromeFallback", script, StringComparison.Ordinal);
        Assert.Contains("Using installed Chrome fallback by explicit request", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Google Chrome executable was not found", script, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptanceScript_DryRunCleanupKeepsUninstallInDryRunMode()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "run-chrome-native-message-acceptance.ps1");

        string script = File.ReadAllText(scriptPath);

        Assert.Contains("-DryRun:$DryRun", script, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptanceScript_CleanupOnlyRunsBeforeChromeResolution()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "run-chrome-native-message-acceptance.ps1");

        string script = File.ReadAllText(scriptPath);

        int cleanupOnlyIndex = script.IndexOf("if ($CleanupOnly)", StringComparison.Ordinal);
        int chromeResolutionIndex = script.IndexOf("$resolvedChromePath = Find-Chrome", StringComparison.Ordinal);

        Assert.True(cleanupOnlyIndex >= 0, "CleanupOnly branch must exist.");
        Assert.True(chromeResolutionIndex >= 0, "Chrome resolution must remain explicit for non-cleanup acceptance runs.");
        Assert.True(
            cleanupOnlyIndex < chromeResolutionIndex,
            "CleanupOnly must not require Chrome for Testing or installed Chrome discovery.");
    }

    [Fact]
    public void AcceptanceScript_CleanupOnlyDoesNotRunNativeHostCleanupTwice()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "run-chrome-native-message-acceptance.ps1");

        string script = File.ReadAllText(scriptPath);

        Assert.Contains("$nativeHostCleanupAlreadyRan", script, StringComparison.Ordinal);
        Assert.Contains("$nativeHostCleanupAlreadyRan = $true", script, StringComparison.Ordinal);
        Assert.Contains("if (-not $nativeHostCleanupAlreadyRan)", script, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptanceScript_ReportAndManifestIncludeGroupedSandboxSafetyEvidence()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "run-chrome-native-message-acceptance.ps1");

        string script = File.ReadAllText(scriptPath);

        Assert.Contains("New-NativeMessagingSafetyEvidence", script, StringComparison.Ordinal);
        Assert.Contains("## Sandbox Safety Evidence", script, StringComparison.Ordinal);
        Assert.Contains("| Claim | Expected | Actual | Status |", script, StringComparison.Ordinal);
        Assert.Contains("nativeMessagingSafetyEvidence", script, StringComparison.Ordinal);
        Assert.Contains("claim = $item.Claim", script, StringComparison.Ordinal);
        Assert.Contains("expected = $item.Expected", script, StringComparison.Ordinal);
        Assert.Contains("actual = $item.Actual", script, StringComparison.Ordinal);
        Assert.Contains("status = $item.Status", script, StringComparison.Ordinal);
        Assert.Contains("Sandboxed Chrome profile", script, StringComparison.Ordinal);
        Assert.Contains("User Chrome windows preserved", script, StringComparison.Ordinal);
        Assert.Contains("Scoped HKCU test host", script, StringComparison.Ordinal);
        Assert.Contains("Temp acceptance DB", script, StringComparison.Ordinal);
        Assert.Contains("Cleanup restore behavior", script, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptanceScript_ReportsSandboxChromeAndTempProfileCleanupFailuresInArtifacts()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "run-chrome-native-message-acceptance.ps1");

        string script = File.ReadAllText(scriptPath);

        Assert.Contains("$cleanupFailures = New-Object System.Collections.Generic.List[string]", script, StringComparison.Ordinal);
        Assert.Contains("Add-CleanupFailure", script, StringComparison.Ordinal);
        Assert.Contains("Sandbox Chrome process cleanup failed", script, StringComparison.Ordinal);
        Assert.Contains("Temp profile cleanup failed", script, StringComparison.Ordinal);
        Assert.Contains("Temp work root cleanup failed", script, StringComparison.Ordinal);
        Assert.Contains("cleanupFailures = @($cleanupFailures)", script, StringComparison.Ordinal);
        Assert.Contains("Cleanup failures", script, StringComparison.Ordinal);
        Assert.Contains("Status = $(if ($cleanupFailures.Count -eq 0) { \"Pass\" } else { \"Warn\" })", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Stop-SandboxChromeProcesses $tempProfile\r\n    } catch {}", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Remove-Item -Recurse -Force $tempProfile\r\n    } catch {}", script, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptanceScript_EnumeratesSqliteJsonRowsForWaitCondition()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "run-chrome-native-message-acceptance.ps1");

        string script = File.ReadAllText(scriptPath);

        Assert.Contains("$parsed = $output | ConvertFrom-Json", script, StringComparison.Ordinal);
        Assert.Contains("foreach ($item in $parsed)", script, StringComparison.Ordinal);
        Assert.Contains("$raw.Count -ge 2", script, StringComparison.Ordinal);
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
