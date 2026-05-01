using System.Diagnostics;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class AndroidManualEmulatorWorkflowTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void AndroidManualEmulatorWorkflow_IsWorkflowDispatchOnlyAndRunsConnectedTestsFromAndroid()
    {
        string workflowPath = Path.Combine(RepositoryRoot, ".github", "workflows", "android-emulator-manual.yml");
        string validationScriptPath = Path.Combine(RepositoryRoot, "scripts", "validate-android-emulator-workflow.ps1");
        string documentationPath = Path.Combine(RepositoryRoot, "docs", "android-emulator-ci.md");

        Assert.True(File.Exists(workflowPath), "Manual Android emulator workflow must exist.");
        Assert.True(File.Exists(validationScriptPath), "Manual Android emulator workflow validator must exist.");
        Assert.True(File.Exists(documentationPath), "Manual Android emulator workflow documentation must exist.");

        string workflow = File.ReadAllText(workflowPath);
        string documentation = File.ReadAllText(documentationPath);

        Assert.Contains("name: Android Emulator Manual", workflow, StringComparison.Ordinal);
        Assert.Contains("workflow_dispatch:", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("push:", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("pull_request:", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("schedule:", workflow, StringComparison.Ordinal);
        Assert.Contains("runs-on: ubuntu-latest", workflow, StringComparison.Ordinal);
        Assert.Contains("actions/checkout@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("actions/setup-java@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("android-actions/setup-android@v3", workflow, StringComparison.Ordinal);
        Assert.Contains("gradle/actions/setup-gradle@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("reactivecircus/android-emulator-runner@v2", workflow, StringComparison.Ordinal);
        Assert.Contains("working-directory: android", workflow, StringComparison.Ordinal);
        Assert.Contains("chmod +x ./gradlew", workflow, StringComparison.Ordinal);
        Assert.Contains("./gradlew connectedDebugAndroidTest --no-daemon --stacktrace", workflow, StringComparison.Ordinal);
        Assert.Contains("scripts/validate-android-emulator-workflow.ps1 -WorkflowPath .github/workflows/android-emulator-manual.yml", workflow, StringComparison.Ordinal);
        Assert.Contains("actions/upload-artifact@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("android/app/build/reports/androidTests/connected/**", workflow, StringComparison.Ordinal);
        Assert.Contains("Manual, optional emulator validation", workflow, StringComparison.Ordinal);
        Assert.Contains("runner capacity is acceptable", workflow, StringComparison.Ordinal);

        Assert.Contains("manual", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("optional", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("runner capacity is acceptable", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("connectedDebugAndroidTest", documentation, StringComparison.Ordinal);
    }

    [Fact]
    public void AndroidManualEmulatorWorkflowValidationScript_PassesAgainstWorkflow()
    {
        RunPowerShell(
            "-NoProfile -ExecutionPolicy Bypass -File scripts\\validate-android-emulator-workflow.ps1 -WorkflowPath .github\\workflows\\android-emulator-manual.yml");
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
