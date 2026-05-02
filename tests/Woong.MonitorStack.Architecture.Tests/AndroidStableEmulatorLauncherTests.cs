using System.Diagnostics;
using System.Text.Json;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class AndroidStableEmulatorLauncherTests
{
    [Fact]
    public void AndroidStableEmulatorLauncher_DocumentsRepeatableStableLaunchContract()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "start-android-emulator-stable.ps1");

        Assert.True(File.Exists(scriptPath), "Stable Android emulator launcher must exist.");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("artifacts/android-emulator-stable", script, StringComparison.Ordinal);
        Assert.Contains("report.md", script, StringComparison.Ordinal);
        Assert.Contains("manifest.json", script, StringComparison.Ordinal);
        Assert.Contains("-no-snapshot-load", script, StringComparison.Ordinal);
        Assert.Contains("-no-boot-anim", script, StringComparison.Ordinal);
        Assert.Contains("-memory", script, StringComparison.Ordinal);
        Assert.Contains("-gpu", script, StringComparison.Ordinal);
        Assert.Contains("sys.boot_completed", script, StringComparison.Ordinal);
        Assert.Contains("BLOCKED", script, StringComparison.Ordinal);
    }

    [Fact]
    public void AndroidStableEmulatorLauncher_WhenBootTimesOut_WritesBlockedArtifacts()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "start-android-emulator-stable.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-emulator-stable-{Guid.NewGuid():N}");
        string fakeSdk = Path.Combine(tempRoot, "sdk");
        string fakeAdb = Path.Combine(tempRoot, "fake-adb.cmd");
        string fakeEmulator = Path.Combine(tempRoot, "fake-emulator.cmd");
        Directory.CreateDirectory(fakeSdk);
        File.WriteAllText(fakeAdb, """
@echo off
if "%1"=="start-server" exit /b 0
if "%1"=="devices" (
  echo List of devices attached
  echo.
  exit /b 0
)
exit /b 0
""");
        File.WriteAllText(fakeEmulator, "@echo off\r\nexit /b 0\r\n");

        try
        {
            using Process process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{tempRoot}\" -AndroidSdkRoot \"{fakeSdk}\" -AdbPath \"{fakeAdb}\" -EmulatorPath \"{fakeEmulator}\" -AvdName Medium_Phone -DeviceSerial emulator-5554 -TimeoutSeconds 1")
            {
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }) ?? throw new InvalidOperationException("Could not start PowerShell.");

            Assert.True(process.WaitForExit(30_000), "Stable emulator launcher timeout scenario should finish quickly.");
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();

            Assert.Equal(0, process.ExitCode);
            Assert.Contains("BLOCKED", stdout + stderr);

            string latest = Path.Combine(tempRoot, "latest");
            string reportPath = Path.Combine(latest, "report.md");
            string manifestPath = Path.Combine(latest, "manifest.json");
            Assert.True(File.Exists(reportPath), "Blocked emulator launch should write report.md.");
            Assert.True(File.Exists(manifestPath), "Blocked emulator launch should write manifest.json.");
            Assert.Contains("BLOCKED", File.ReadAllText(reportPath));

            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            Assert.Equal("BLOCKED", manifest.RootElement.GetProperty("status").GetString());
            Assert.Equal("boot-timeout", manifest.RootElement.GetProperty("classification").GetString());
            Assert.Equal("Medium_Phone", manifest.RootElement.GetProperty("avdName").GetString());
            Assert.Equal("emulator-5554", manifest.RootElement.GetProperty("deviceSerial").GetString());
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
