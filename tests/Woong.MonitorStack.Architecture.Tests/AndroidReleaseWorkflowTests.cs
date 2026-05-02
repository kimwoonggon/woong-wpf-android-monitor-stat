using System.Diagnostics;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class AndroidReleaseWorkflowTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void AndroidReleaseWorkflow_BuildsTestsRequiresSigningSecretsAndPublishesOnlySignedReleaseApk()
    {
        string workflowPath = Path.Combine(RepositoryRoot, ".github", "workflows", "android-release.yml");
        string validationScriptPath = Path.Combine(RepositoryRoot, "scripts", "validate-android-release-workflow.ps1");

        Assert.True(File.Exists(workflowPath), "Android release workflow must exist.");
        Assert.True(File.Exists(validationScriptPath), "Android release workflow validator must exist.");

        string workflow = File.ReadAllText(workflowPath);

        Assert.Contains("name: Android Release", workflow, StringComparison.Ordinal);
        Assert.Contains("tags:", workflow, StringComparison.Ordinal);
        Assert.Contains("\"android-v*\"", workflow, StringComparison.Ordinal);
        Assert.Contains("workflow_dispatch:", workflow, StringComparison.Ordinal);
        Assert.Contains("runs-on: ubuntu-latest", workflow, StringComparison.Ordinal);
        Assert.Contains("actions/checkout@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("actions/setup-java@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("android-actions/setup-android@v3", workflow, StringComparison.Ordinal);
        Assert.Contains("gradle/actions/setup-gradle@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("chmod +x ./gradlew", workflow, StringComparison.Ordinal);
        Assert.Contains("./gradlew testDebugUnitTest assembleDebug assembleRelease assembleDebugAndroidTest --no-daemon --stacktrace", workflow, StringComparison.Ordinal);
        Assert.Contains("working-directory: android", workflow, StringComparison.Ordinal);
        Assert.Contains("Fail when Android release signing secrets are missing", workflow, StringComparison.Ordinal);
        Assert.Contains("throw \"Android releases require ANDROID_KEYSTORE_BASE64, ANDROID_KEYSTORE_PASSWORD, ANDROID_KEY_ALIAS, and ANDROID_KEY_PASSWORD.\"", workflow, StringComparison.Ordinal);
        Assert.Contains("app-release-unsigned.apk", workflow, StringComparison.Ordinal);
        Assert.Contains("$env:RUNNER_TEMP/android-release-aligned.apk", workflow, StringComparison.Ordinal);
        Assert.Contains("woong-monitor-android-release-signed.apk", workflow, StringComparison.Ordinal);
        Assert.Contains("artifacts/android-release/woong-monitor-android-release-signed.apk", workflow, StringComparison.Ordinal);
        Assert.Contains("woong-monitor-android-release-${{ github.ref_name }}", workflow, StringComparison.Ordinal);
        Assert.Contains("android/app/build/reports/tests/testDebugUnitTest/**", workflow, StringComparison.Ordinal);
        Assert.Contains("softprops/action-gh-release@v2", workflow, StringComparison.Ordinal);

        Assert.Contains("ANDROID_KEYSTORE_BASE64", workflow, StringComparison.Ordinal);
        Assert.Contains("ANDROID_KEYSTORE_PASSWORD", workflow, StringComparison.Ordinal);
        Assert.Contains("ANDROID_KEY_ALIAS", workflow, StringComparison.Ordinal);
        Assert.Contains("ANDROID_KEY_PASSWORD", workflow, StringComparison.Ordinal);
        Assert.Contains("apksigner verify --verbose", workflow, StringComparison.Ordinal);
        Assert.Contains("release-readiness.json", workflow, StringComparison.Ordinal);
        Assert.Contains("versionCode", workflow, StringComparison.Ordinal);
        Assert.Contains("versionName", workflow, StringComparison.Ordinal);
        Assert.Contains("signedApkSha256", workflow, StringComparison.Ordinal);
        Assert.Contains("productionSyncBaseUrlConfigured", workflow, StringComparison.Ordinal);
        Assert.Contains("syncDefaultOptIn", workflow, StringComparison.Ordinal);
        Assert.Contains("playPublishingMode", workflow, StringComparison.Ordinal);
        Assert.Contains("emulatorEvidenceRequiredBeforePublicRelease", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("woong-monitor-android-debug.apk", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("woong-monitor-android-test.apk", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("woong-monitor-android-release-unsigned.apk", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("artifacts/android-release/woong-monitor-android-release-aligned.apk", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("steps.signing.outputs.enabled", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("*.jks", workflow, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AndroidReleaseWorkflowValidationScript_PassesAgainstWorkflow()
    {
        RunPowerShell(
            "-NoProfile -ExecutionPolicy Bypass -File scripts\\validate-android-release-workflow.ps1 -WorkflowPath .github\\workflows\\android-release.yml");
    }

    [Fact]
    public void AndroidReleaseDocs_DistinguishInternalArtifactsFromPlayPublishingAndListOperatorSteps()
    {
        string readme = NormalizeWhitespace(File.ReadAllText(Path.Combine(RepositoryRoot, "README.md")));
        string releaseChecklist = NormalizeWhitespace(File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "docs",
            "release-checklist.md")));

        foreach (string document in new[] { readme, releaseChecklist })
        {
            Assert.Contains("ANDROID_KEYSTORE_BASE64", document, StringComparison.Ordinal);
            Assert.Contains("ANDROID_KEYSTORE_PASSWORD", document, StringComparison.Ordinal);
            Assert.Contains("ANDROID_KEY_ALIAS", document, StringComparison.Ordinal);
            Assert.Contains("ANDROID_KEY_PASSWORD", document, StringComparison.Ordinal);
            Assert.Contains("unsigned, debug, and androidTest APKs", document, StringComparison.Ordinal);
            Assert.Contains("internal validation", document, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Play Console track", document, StringComparison.Ordinal);
            Assert.Contains("versionCode", document, StringComparison.Ordinal);
            Assert.Contains("versionName", document, StringComparison.Ordinal);
            Assert.Contains("download the GitHub Release archive", document, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("verify `woong-monitor-android-release-signed.apk`", document, StringComparison.Ordinal);
            Assert.Contains("upload the signed APK to the approved Play Console track", document, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("record the GitHub tag, artifact name, Play track, release approver", document, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("release-readiness.json", document, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("signed APK SHA-256", document, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("sync default opt-in remains false", document, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("emulator evidence path", document, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void RunPowerShell(string arguments)
    {
        using Process process = Process.Start(new ProcessStartInfo(
            "powershell.exe",
            arguments)
        {
            WorkingDirectory = RepositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("Could not start PowerShell.");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(
            process.ExitCode == 0,
            $"PowerShell command failed with exit code {process.ExitCode}.{Environment.NewLine}STDOUT:{Environment.NewLine}{output}{Environment.NewLine}STDERR:{Environment.NewLine}{error}");
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

    private static string NormalizeWhitespace(string value)
        => string.Join(
            " ",
            value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
