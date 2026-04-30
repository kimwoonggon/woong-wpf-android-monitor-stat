using System.Diagnostics;
using System.Text.Json;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class AndroidResourceMeasurementScriptTests
{
    [Fact]
    public void AndroidResourceMeasurementScript_DocumentsLocalOnlyMeasurementContract()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-resource-measurement.ps1");

        Assert.True(File.Exists(scriptPath), "Android resource measurement script must exist.");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("artifacts/android-resource-measurements", script);
        Assert.Contains("dumpsys", script);
        Assert.Contains("meminfo", script);
        Assert.Contains("gfxinfo", script);
        Assert.Contains("com.woong.monitorstack", script);
        Assert.Contains("report.md", script);
        Assert.Contains("manifest.json", script);
        Assert.DoesNotContain("screencap", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("input text", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AndroidResourceMeasurementScript_WhenNoDeviceConnected_WritesBlockedArtifacts()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-resource-measurement.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-resource-{Guid.NewGuid():N}");
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

    [Fact]
    public void AndroidResourceMeasurementScript_WhenDeviceConnected_CollectsPackageScopedArtifacts()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-resource-measurement.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-resource-{Guid.NewGuid():N}");
        string fakeAdb = Path.Combine(tempRoot, "fake-adb.cmd");
        string fakeGradle = Path.Combine(tempRoot, "gradlew.bat");
        string adbLog = Path.Combine(tempRoot, "adb.log");
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(fakeAdb, $$"""
@echo off
echo %*>>"{{adbLog}}"
if "%1"=="devices" (
  echo List of devices attached
  echo emulator-5554 device product:test model:FakeDevice
  exit /b 0
)
if "%1"=="shell" (
  if "%2"=="pidof" (
    echo 12345
    exit /b 0
  )
  echo fake shell output for %*
  exit /b 0
)
exit /b 0
""");
        File.WriteAllText(fakeGradle, "@echo off\r\nexit /b 0\r\n");

        try
        {
            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{tempRoot}\" -AdbPath \"{fakeAdb}\" -GradleWrapperPath \"{fakeGradle}\" -SkipBuild -DurationSeconds 0")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = repoRoot
            });
            Assert.NotNull(process);
            process.WaitForExit(30_000);

            Assert.Equal(0, process.ExitCode);

            string latest = Path.Combine(tempRoot, "latest");
            Assert.True(File.Exists(Path.Combine(latest, "meminfo.txt")));
            Assert.True(File.Exists(Path.Combine(latest, "gfxinfo.txt")));
            Assert.True(File.Exists(Path.Combine(latest, "process.txt")));

            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(latest, "manifest.json")));
            Assert.Equal("PASS", manifest.RootElement.GetProperty("status").GetString());
            Assert.Equal("com.woong.monitorstack", manifest.RootElement.GetProperty("packageName").GetString());

            string commands = File.ReadAllText(adbLog);
            Assert.Contains("shell monkey -p com.woong.monitorstack", commands);
            Assert.Contains("shell dumpsys meminfo com.woong.monitorstack", commands);
            Assert.Contains("shell dumpsys gfxinfo com.woong.monitorstack", commands);
            Assert.DoesNotContain("screencap", commands, StringComparison.OrdinalIgnoreCase);
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
