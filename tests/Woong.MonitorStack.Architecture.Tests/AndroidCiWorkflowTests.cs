using System.Diagnostics;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class AndroidCiWorkflowTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void AndroidCiWorkflow_UsesAndroidGradleWrapperBuildsApksAndUploadsArtifactsOnFailure()
    {
        string workflowPath = Path.Combine(RepositoryRoot, ".github", "workflows", "android-ci.yml");
        string validationScriptPath = Path.Combine(RepositoryRoot, "scripts", "validate-android-ci.ps1");

        Assert.True(File.Exists(workflowPath), "Android CI workflow must exist.");
        Assert.True(File.Exists(validationScriptPath), "Android CI workflow validator must exist.");

        string workflow = File.ReadAllText(workflowPath);

        Assert.Contains("name: Android CI", workflow, StringComparison.Ordinal);
        Assert.Contains("push:", workflow, StringComparison.Ordinal);
        Assert.Contains("pull_request:", workflow, StringComparison.Ordinal);
        Assert.Contains("permissions:", workflow, StringComparison.Ordinal);
        Assert.Contains("contents: read", workflow, StringComparison.Ordinal);
        Assert.Contains("actions/checkout@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("actions/setup-java@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("android-actions/setup-android@v3", workflow, StringComparison.Ordinal);
        Assert.Contains("gradle/actions/setup-gradle@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("chmod +x ./gradlew", workflow, StringComparison.Ordinal);
        Assert.Contains("working-directory: android", workflow, StringComparison.Ordinal);
        Assert.Contains("./gradlew testDebugUnitTest assembleDebug assembleRelease assembleDebugAndroidTest --no-daemon --stacktrace", workflow, StringComparison.Ordinal);
        Assert.Contains("android/app/build/outputs/apk/debug/*.apk", workflow, StringComparison.Ordinal);
        Assert.Contains("android/app/build/outputs/apk/androidTest/debug/*.apk", workflow, StringComparison.Ordinal);
        Assert.Contains("android/app/build/reports/tests/testDebugUnitTest/**", workflow, StringComparison.Ordinal);
        Assert.Contains("android/app/build/test-results/testDebugUnitTest/**", workflow, StringComparison.Ordinal);

        Assert.Contains("name: woong-monitor-android-debug-apk", workflow, StringComparison.Ordinal);
        Assert.Contains("name: woong-monitor-android-test-apk", workflow, StringComparison.Ordinal);
        Assert.Contains("name: woong-monitor-android-unit-test-report", workflow, StringComparison.Ordinal);
        Assert.Contains("if: always()", workflow, StringComparison.Ordinal);
        Assert.Contains("if-no-files-found: ignore", workflow, StringComparison.Ordinal);

        Assert.DoesNotContain("connectedDebugAndroidTest", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("reactivecircus/android-emulator-runner", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("emulator", workflow, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AndroidCiWorkflowValidationScript_PassesAgainstWorkflow()
    {
        RunPowerShell(
            "-NoProfile -ExecutionPolicy Bypass -File scripts\\validate-android-ci.ps1 -WorkflowPath .github\\workflows\\android-ci.yml");
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
