using System.Diagnostics;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class AndroidUiSnapshotScriptTests
{
    [Fact]
    public void AndroidUiSnapshotScript_DocumentsDeviceBlockedArtifactContract()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-ui-snapshots.ps1");

        Assert.True(File.Exists(scriptPath), "Android UI snapshot script must exist.");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("artifacts/android-ui-snapshots", script);
        Assert.Contains("report.md", script);
        Assert.Contains("manifest.json", script);
        Assert.Contains("visual-review-prompt.md", script);
        Assert.Contains("No connected Android device", script);
        Assert.Contains("dashboard", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("settings", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sessions", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("daily summary", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AndroidUiSnapshotScript_WhenNoDeviceConnected_WritesBlockedArtifacts()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-ui-snapshots.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-snapshots-{Guid.NewGuid():N}");
        string fakeAdb = Path.Combine(tempRoot, "fake-adb.cmd");
        string fakeGradle = Path.Combine(tempRoot, "gradlew.bat");
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(fakeAdb, "@echo off\r\necho List of devices attached\r\necho.\r\n");
        File.WriteAllText(fakeGradle, "@echo off\r\nexit /b 0\r\n");

        try
        {
            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{tempRoot}\" -AdbPath \"{fakeAdb}\" -GradleWrapperPath \"{fakeGradle}\" -SkipBuild")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = repoRoot
            });
            Assert.NotNull(process);
            process.WaitForExit(30_000);

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            Assert.Equal(0, process.ExitCode);
            Assert.Contains("No connected Android device", stdout + stderr);

            string latest = Path.Combine(tempRoot, "latest");
            Assert.True(File.Exists(Path.Combine(latest, "report.md")));
            Assert.True(File.Exists(Path.Combine(latest, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(latest, "visual-review-prompt.md")));
            Assert.Contains("BLOCKED", File.ReadAllText(Path.Combine(latest, "report.md")));
            Assert.Contains("No connected Android device", File.ReadAllText(Path.Combine(latest, "manifest.json")));
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
