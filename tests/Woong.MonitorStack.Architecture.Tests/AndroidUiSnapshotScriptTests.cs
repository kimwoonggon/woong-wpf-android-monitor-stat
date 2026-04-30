using System.Diagnostics;
using System.Text.Json;

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
    public void AndroidUiSnapshotScript_DocumentsLocationContextScreenshotChecks()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-ui-snapshots.ps1");

        Assert.True(File.Exists(scriptPath), "Android UI snapshot script must exist.");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("Dashboard location card", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Settings location section", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("locationContextCard", script, StringComparison.Ordinal);
        Assert.Contains("locationContextCheckBox", script, StringComparison.Ordinal);
        Assert.Contains("preciseLatitudeLongitudeCheckBox", script, StringComparison.Ordinal);
        Assert.Contains("requestLocationPermissionButton", script, StringComparison.Ordinal);
    }

    [Fact]
    public void AndroidUiSnapshotScript_DocumentsFeatureByFeatureScreenshots()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-ui-snapshots.ps1");

        Assert.True(File.Exists(scriptPath), "Android UI snapshot script must exist.");
        string script = File.ReadAllText(scriptPath);

        string[] expectedFeatureScreens =
        [
            "01-dashboard-overview.png",
            "02-dashboard-summary-location.png",
            "03-dashboard-charts.png",
            "04-dashboard-recent-sessions.png",
            "05-settings-privacy-sync.png",
            "06-settings-location-permission.png",
            "07-sessions-list.png",
            "08-daily-summary.png",
            "09-main-shell.png",
            "10-main-shell-sessions.png",
            "11-main-shell-settings.png"
        ];

        foreach (string screen in expectedFeatureScreens)
        {
            Assert.Contains(screen, script);
        }

        Assert.Contains("featureScreens", script);
        Assert.Contains("dashboard overview", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("settings location permission", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("main shell", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AndroidSnapshotCapture_UsesNestedScrollViewDescendantCoordinates()
    {
        string repoRoot = FindRepositoryRoot();
        string captureTestPath = Path.Combine(
            repoRoot,
            "android",
            "app",
            "src",
            "androidTest",
            "java",
            "com",
            "woong",
            "monitorstack",
            "snapshots",
            "SnapshotCaptureTest.kt");

        string captureTest = File.ReadAllText(captureTestPath);

        Assert.Contains("offsetDescendantRectToMyCoords", captureTest);
        Assert.Contains("getDrawingRect", captureTest);
        Assert.DoesNotContain("scrollView.scrollTo(0, target.top)", captureTest);
    }

    [Fact]
    public void AndroidSnapshotCapture_ScrollsSettingsLocationSectionForLocationScreenshot()
    {
        string repoRoot = FindRepositoryRoot();
        string captureTestPath = Path.Combine(
            repoRoot,
            "android",
            "app",
            "src",
            "androidTest",
            "java",
            "com",
            "woong",
            "monitorstack",
            "snapshots",
            "SnapshotCaptureTest.kt");

        string captureTest = File.ReadAllText(captureTestPath);

        Assert.Contains("06-settings-location-permission.png", captureTest);
        Assert.Contains("scrollSettingsTo", captureTest);
        Assert.Contains("R.id.locationSettingsCard", captureTest);
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
            Assert.Contains("Dashboard location card", File.ReadAllText(Path.Combine(latest, "report.md")));
            Assert.Contains("Settings location section", File.ReadAllText(Path.Combine(latest, "report.md")));
            Assert.Contains("No connected Android device", File.ReadAllText(Path.Combine(latest, "manifest.json")));
            Assert.Contains("expectedLocationChecks", File.ReadAllText(Path.Combine(latest, "manifest.json")));
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
    public void AndroidUiSnapshotScript_WhenDeviceConnected_CapturesExpectedAppScreens()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-ui-snapshots.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-snapshots-{Guid.NewGuid():N}");
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
if "%1"=="pull" (
  echo fake png>"%3"
  exit /b 0
)
exit /b 0
""");
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
            Assert.DoesNotContain("capture is not implemented", stdout + stderr, StringComparison.OrdinalIgnoreCase);

            string latest = Path.Combine(tempRoot, "latest");
            string[] expectedScreenshots =
            [
                "dashboard.png",
                "settings.png",
                "sessions.png",
                "daily-summary.png",
                "01-dashboard-overview.png",
                "02-dashboard-summary-location.png",
                "03-dashboard-charts.png",
                "04-dashboard-recent-sessions.png",
                "05-settings-privacy-sync.png",
                "06-settings-location-permission.png",
                "07-sessions-list.png",
                "08-daily-summary.png",
                "09-main-shell.png",
                "10-main-shell-sessions.png",
                "11-main-shell-settings.png"
            ];
            foreach (string screenshot in expectedScreenshots)
            {
                Assert.True(File.Exists(Path.Combine(latest, screenshot)), $"{screenshot} should be captured.");
            }

            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(latest, "manifest.json")));
            Assert.Equal("PASS", manifest.RootElement.GetProperty("status").GetString());
            string manifestText = manifest.RootElement.GetRawText();
            Assert.Contains("dashboard.png", manifestText);
            Assert.Contains("settings.png", manifestText);
            Assert.Contains("sessions.png", manifestText);
            Assert.Contains("daily-summary.png", manifestText);
            Assert.Contains("featureScreens", manifestText);
            Assert.Contains("01-dashboard-overview.png", manifestText);
            Assert.Contains("08-daily-summary.png", manifestText);
            Assert.Contains("09-main-shell.png", manifestText);
            Assert.Contains("10-main-shell-sessions.png", manifestText);
            Assert.Contains("11-main-shell-settings.png", manifestText);

            string commands = File.ReadAllText(adbLog);
            Assert.Contains("am instrument -w -e class com.woong.monitorstack.snapshots.SnapshotSeedTest", commands);
            Assert.Contains("am instrument -w -e class com.woong.monitorstack.snapshots.SnapshotCaptureTest", commands);
            Assert.Contains("/sdcard/Android/data/com.woong.monitorstack/files/ui-snapshots/dashboard.png", commands);
            Assert.Contains("/sdcard/Android/data/com.woong.monitorstack/files/ui-snapshots/settings.png", commands);
            Assert.Contains("/sdcard/Android/data/com.woong.monitorstack/files/ui-snapshots/sessions.png", commands);
            Assert.Contains("/sdcard/Android/data/com.woong.monitorstack/files/ui-snapshots/daily-summary.png", commands);
            Assert.Contains("/sdcard/Android/data/com.woong.monitorstack/files/ui-snapshots/01-dashboard-overview.png", commands);
            Assert.Contains("/sdcard/Android/data/com.woong.monitorstack/files/ui-snapshots/08-daily-summary.png", commands);
            Assert.Contains("/sdcard/Android/data/com.woong.monitorstack/files/ui-snapshots/09-main-shell.png", commands);
            Assert.Contains("/sdcard/Android/data/com.woong.monitorstack/files/ui-snapshots/10-main-shell-sessions.png", commands);
            Assert.Contains("/sdcard/Android/data/com.woong.monitorstack/files/ui-snapshots/11-main-shell-settings.png", commands);
            Assert.DoesNotContain("am start", commands, StringComparison.OrdinalIgnoreCase);
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
