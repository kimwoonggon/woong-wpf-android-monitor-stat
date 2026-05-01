using System.Diagnostics;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class AndroidReleaseWorkflowTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void AndroidReleaseWorkflow_BuildsTestsSignsWhenPossibleAndUploadsApkArtifacts()
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
        Assert.Contains("app-release-unsigned.apk", workflow, StringComparison.Ordinal);
        Assert.Contains("woong-monitor-android-debug.apk", workflow, StringComparison.Ordinal);
        Assert.Contains("woong-monitor-android-test.apk", workflow, StringComparison.Ordinal);
        Assert.Contains("woong-monitor-android-release-unsigned.apk", workflow, StringComparison.Ordinal);
        Assert.Contains("woong-monitor-android-apks-${{ github.ref_name }}", workflow, StringComparison.Ordinal);
        Assert.Contains("artifacts/android-release/*.apk", workflow, StringComparison.Ordinal);
        Assert.Contains("woong-monitor-android-release-${{ github.ref_name }}", workflow, StringComparison.Ordinal);
        Assert.Contains("android/app/build/reports/tests/testDebugUnitTest/**", workflow, StringComparison.Ordinal);
        Assert.Contains("softprops/action-gh-release@v2", workflow, StringComparison.Ordinal);

        Assert.Contains("ANDROID_KEYSTORE_BASE64", workflow, StringComparison.Ordinal);
        Assert.Contains("ANDROID_KEYSTORE_PASSWORD", workflow, StringComparison.Ordinal);
        Assert.Contains("ANDROID_KEY_ALIAS", workflow, StringComparison.Ordinal);
        Assert.Contains("ANDROID_KEY_PASSWORD", workflow, StringComparison.Ordinal);
        Assert.Contains("Android signing secrets are missing", workflow, StringComparison.Ordinal);
        Assert.Contains("apksigner verify --verbose", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("*.jks", workflow, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AndroidReleaseWorkflowValidationScript_PassesAgainstWorkflow()
    {
        RunPowerShell(
            "-NoProfile -ExecutionPolicy Bypass -File scripts\\validate-android-release-workflow.ps1 -WorkflowPath .github\\workflows\\android-release.yml");
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
}
