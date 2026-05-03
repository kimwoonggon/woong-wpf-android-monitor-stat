using System.Diagnostics;
using System.Text.Json;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class AndroidLocalEmulatorEvidenceRunbookTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void AndroidEmulatorEvidenceRunbook_DocumentsLocalScreenshotAndAppSwitchWorkflow()
    {
        string runbookPath = Path.Combine(RepositoryRoot, "docs", "android-emulator-runbook.md");
        string readmePath = Path.Combine(RepositoryRoot, "README.md");

        Assert.True(File.Exists(runbookPath), "Local Android emulator runbook must exist.");

        string runbook = File.ReadAllText(runbookPath);
        string readme = File.ReadAllText(readmePath);

        Assert.Contains("scripts\\start-android-emulator-stable.ps1 -AvdName Medium_Phone -Restart", runbook, StringComparison.Ordinal);
        Assert.Contains("adb devices -l", runbook, StringComparison.Ordinal);
        Assert.Contains("sys.boot_completed", runbook, StringComparison.Ordinal);
        Assert.Contains("-DeviceSerial emulator-5554", runbook, StringComparison.Ordinal);
        Assert.Contains("scripts\\run-android-ui-snapshots.ps1 -DeviceSerial emulator-5554", runbook, StringComparison.Ordinal);
        Assert.Contains("scripts\\run-android-app-switch-qa.ps1 -DeviceSerial emulator-5554", runbook, StringComparison.Ordinal);
        Assert.Contains("artifacts/android-ui-snapshots/latest/report.md", runbook, StringComparison.Ordinal);
        Assert.Contains("artifacts/android-app-switch-qa/latest/report.md", runbook, StringComparison.Ordinal);
        Assert.Contains("Status: BLOCKED", runbook, StringComparison.Ordinal);
        Assert.Contains("rerun after starting the emulator", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No Chrome screenshots", runbook, StringComparison.Ordinal);
        Assert.Contains("No Chrome UI hierarchy", runbook, StringComparison.Ordinal);
        Assert.Contains("typed text", runbook, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("docs/android-emulator-runbook.md", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void AndroidEvidenceScripts_WhenNoDeviceConnected_WriteActionableBlockedRerunGuidance()
    {
        AssertBlockedRerunGuidance(
            scriptFileName: "run-android-ui-snapshots.ps1",
            outputFolderName: "ui",
            expectedRerunCommand: "scripts\\run-android-ui-snapshots.ps1 -DeviceSerial emulator-5554",
            expectedArtifactPath: "artifacts/android-ui-snapshots/latest/report.md");

        AssertBlockedRerunGuidance(
            scriptFileName: "run-android-app-switch-qa.ps1",
            outputFolderName: "app-switch",
            expectedRerunCommand: "scripts\\run-android-app-switch-qa.ps1 -DeviceSerial emulator-5554",
            expectedArtifactPath: "artifacts/android-app-switch-qa/latest/report.md");
    }

    private static void AssertBlockedRerunGuidance(
        string scriptFileName,
        string outputFolderName,
        string expectedRerunCommand,
        string expectedArtifactPath)
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-evidence-runbook-{Guid.NewGuid():N}");
        string outputRoot = Path.Combine(tempRoot, outputFolderName);
        string fakeAdb = Path.Combine(tempRoot, "fake-adb.cmd");
        string fakeGradle = Path.Combine(tempRoot, "gradlew.bat");
        string scriptPath = Path.Combine(RepositoryRoot, "scripts", scriptFileName);
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(fakeAdb, "@echo off\r\necho List of devices attached\r\necho.\r\n");
        File.WriteAllText(fakeGradle, "@echo off\r\nexit /b 0\r\n");

        try
        {
            using Process process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{outputRoot}\" -AdbPath \"{fakeAdb}\" -GradleWrapperPath \"{fakeGradle}\" -SkipBuild")
            {
                WorkingDirectory = RepositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }) ?? throw new InvalidOperationException("Could not start PowerShell.");

            Assert.True(process.WaitForExit(30_000), $"{scriptFileName} no-device run should finish quickly.");
            string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();

            Assert.Equal(0, process.ExitCode);
            Assert.Contains("No connected Android device", output, StringComparison.Ordinal);
            Assert.Contains("Latest report", output, StringComparison.OrdinalIgnoreCase);

            string latest = Path.Combine(outputRoot, "latest");
            string reportPath = Path.Combine(latest, "report.md");
            string manifestPath = Path.Combine(latest, "manifest.json");
            Assert.True(File.Exists(reportPath), $"{scriptFileName} should write latest report.md.");
            Assert.True(File.Exists(manifestPath), $"{scriptFileName} should write latest manifest.json.");

            string report = File.ReadAllText(reportPath);
            Assert.Contains("Status: BLOCKED", report, StringComparison.Ordinal);
            Assert.Contains("scripts\\start-android-emulator-stable.ps1 -AvdName Medium_Phone -Restart", report, StringComparison.Ordinal);
            Assert.Contains("adb devices -l", report, StringComparison.Ordinal);
            Assert.Contains(expectedRerunCommand, report, StringComparison.Ordinal);
            Assert.Contains(expectedArtifactPath, report, StringComparison.Ordinal);
            Assert.Contains("rerun after starting the emulator", report, StringComparison.OrdinalIgnoreCase);

            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            Assert.Equal("BLOCKED", manifest.RootElement.GetProperty("status").GetString());
            string manifestText = manifest.RootElement.GetRawText();
            Assert.Contains("nextAction", manifestText, StringComparison.Ordinal);
            Assert.Contains("rerunCommands", manifestText, StringComparison.Ordinal);
            Assert.Contains("emulator-5554", manifestText, StringComparison.Ordinal);
            Assert.Contains(scriptFileName, manifestText, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
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
