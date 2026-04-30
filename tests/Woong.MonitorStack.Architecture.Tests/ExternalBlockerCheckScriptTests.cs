using System.Diagnostics;
using System.Text.Json;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class ExternalBlockerCheckScriptTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(
        Path.GetTempPath(),
        $"woong-external-blockers-{Guid.NewGuid():N}");

    [Fact]
    public void ExternalBlockerScript_DocumentsSafeReadOnlyChecks()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "check-external-blockers.ps1");

        Assert.True(File.Exists(scriptPath), "External blocker check script must exist.");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("artifacts/external-blockers", script, StringComparison.Ordinal);
        Assert.Contains("adb devices -l", script, StringComparison.Ordinal);
        Assert.Contains("docker ps", script, StringComparison.Ordinal);
        Assert.Contains("report.md", script, StringComparison.Ordinal);
        Assert.Contains("manifest.json", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Remove-Item", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HKCU", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("screencap", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("input text", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExternalBlockerScript_WhenOnlyEmulatorAndDockerDaemonUnavailable_WritesBlockedReport()
    {
        Directory.CreateDirectory(_tempRoot);
        string fakeAdb = Path.Combine(_tempRoot, "fake-adb.cmd");
        string fakeDocker = Path.Combine(_tempRoot, "fake-docker.cmd");
        File.WriteAllText(fakeAdb, """
@echo off
if "%1"=="devices" (
  echo List of devices attached
  echo emulator-5554 device product:test model:FakeEmulator
  exit /b 0
)
exit /b 1
""");
        File.WriteAllText(fakeDocker, """
@echo off
if "%1"=="ps" (
  echo Docker daemon unavailable 1>&2
  exit /b 1
)
exit /b 1
""");

        RunPowerShell(
            $"-OutputRoot \"{_tempRoot}\" -AdbPath \"{fakeAdb}\" -DockerPath \"{fakeDocker}\"");

        string latest = Path.Combine(_tempRoot, "latest");
        string report = File.ReadAllText(Path.Combine(latest, "report.md"));
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(latest, "manifest.json")));

        Assert.Equal("BLOCKED", manifest.RootElement.GetProperty("status").GetString());
        Assert.False(manifest.RootElement.GetProperty("physicalAndroidDeviceReady").GetBoolean());
        Assert.False(manifest.RootElement.GetProperty("dockerDaemonReady").GetBoolean());
        Assert.Contains("physical Android device", report);
        Assert.Contains("Docker daemon unavailable", report);
    }

    [Fact]
    public void ExternalBlockerScript_WhenPhysicalDeviceAndDockerDaemonReady_WritesPassReport()
    {
        Directory.CreateDirectory(_tempRoot);
        string fakeAdb = Path.Combine(_tempRoot, "fake-adb.cmd");
        string fakeDocker = Path.Combine(_tempRoot, "fake-docker.cmd");
        File.WriteAllText(fakeAdb, """
@echo off
if "%1"=="devices" (
  echo List of devices attached
  echo R58N123456 device product:test model:PhysicalPhone
  exit /b 0
)
exit /b 1
""");
        File.WriteAllText(fakeDocker, """
@echo off
if "%1"=="ps" (
  echo CONTAINER ID   IMAGE
  exit /b 0
)
exit /b 1
""");

        RunPowerShell(
            $"-OutputRoot \"{_tempRoot}\" -AdbPath \"{fakeAdb}\" -DockerPath \"{fakeDocker}\"");

        string latest = Path.Combine(_tempRoot, "latest");
        string report = File.ReadAllText(Path.Combine(latest, "report.md"));
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(latest, "manifest.json")));

        Assert.Equal("PASS", manifest.RootElement.GetProperty("status").GetString());
        Assert.True(manifest.RootElement.GetProperty("physicalAndroidDeviceReady").GetBoolean());
        Assert.True(manifest.RootElement.GetProperty("dockerDaemonReady").GetBoolean());
        Assert.Contains("PASS", report);
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

    private static void RunPowerShell(string arguments)
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "check-external-blockers.ps1");
        using var process = Process.Start(new ProcessStartInfo(
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
        Assert.True(process.ExitCode == 0, output);
    }
}
