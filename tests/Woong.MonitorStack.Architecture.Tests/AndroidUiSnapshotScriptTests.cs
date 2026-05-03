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
        Assert.Contains("locationMiniMapView", script, StringComparison.Ordinal);
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
            "11-main-shell-settings.png",
            "12-main-shell-report.png",
            "13-permission-onboarding.png",
            "14-app-detail.png"
        ];

        foreach (string screen in expectedFeatureScreens)
        {
            Assert.Contains(screen, script);
        }

        Assert.Contains("featureScreens", script);
        Assert.Contains("dashboard overview", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("settings location permission", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("main shell", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("permission onboarding", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("app detail", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AndroidUiSnapshotScript_CapturesCanonicalFigmaSevenScreenSet()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-ui-snapshots.ps1");
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

        Assert.True(File.Exists(scriptPath), "Android UI snapshot script must exist.");
        Assert.True(File.Exists(captureTestPath), "Android screenshot capture instrumentation must exist.");
        string script = File.ReadAllText(scriptPath);
        string captureTest = File.ReadAllText(captureTestPath);

        string[] canonicalFigmaScreens =
        [
            "figma-01-splash.png",
            "figma-02-permission.png",
            "figma-03-dashboard.png",
            "figma-04-sessions.png",
            "figma-05-app-detail.png",
            "figma-06-report.png",
            "figma-07-settings.png"
        ];

        foreach (string screen in canonicalFigmaScreens)
        {
            Assert.Contains(screen, script);
            Assert.Contains(screen, captureTest);
        }

        Assert.Contains("Figma 7-screen parity", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("captureCanonicalFigmaScreens", captureTest, StringComparison.Ordinal);
        Assert.Contains("MainActivity.usageAccessGateFactory", captureTest, StringComparison.Ordinal);
    }

    [Fact]
    public void AndroidUiSnapshotScript_CopiesCanonicalScreensToBeginnerReviewBeforeAfterNames()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-ui-snapshots.ps1");

        Assert.True(File.Exists(scriptPath), "Android UI snapshot script must exist.");
        string script = File.ReadAllText(scriptPath);

        string[] beginnerReviewScreens =
        [
            "01-splash-before.png",
            "01-splash-after.png",
            "02-permission-before.png",
            "02-permission-after.png",
            "03-dashboard-before.png",
            "03-dashboard-after.png",
            "04-sessions-before.png",
            "04-sessions-after.png",
            "05-app-detail-before.png",
            "05-app-detail-after.png",
            "06-report-before.png",
            "06-report-after.png",
            "07-settings-before.png",
            "07-settings-after.png"
        ];

        foreach (string screen in beginnerReviewScreens)
        {
            Assert.Contains(screen, script);
        }

        Assert.Contains("beginnerReviewAliases", script);
        Assert.Contains("Copy-AndroidBeginnerReviewAliases", script);
        Assert.Contains("copy", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Privacy Boundary", script);
    }

    [Fact]
    public void AndroidUiSnapshotScript_CapturesExpandedSessionsAndReportEvidence()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-ui-snapshots.ps1");
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

        Assert.True(File.Exists(scriptPath), "Android UI snapshot script must exist.");
        Assert.True(File.Exists(captureTestPath), "Android screenshot capture instrumentation must exist.");
        string script = File.ReadAllText(scriptPath);
        string captureTest = File.ReadAllText(captureTestPath);

        string[] expandedEvidenceScreens =
        [
            "18-sessions-default.png",
            "19-sessions-filtered.png",
            "20-report-7d.png",
            "21-report-30d.png",
            "22-report-90d.png",
            "23-report-custom-valid.png",
            "24-report-custom-invalid.png"
        ];

        foreach (string screen in expandedEvidenceScreens)
        {
            Assert.Contains(screen, script);
            Assert.Contains(screen, captureTest);
        }

        Assert.Contains("sessions default", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sessions filtered", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("report custom invalid", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reportCustomRangeErrorText", captureTest, StringComparison.Ordinal);
        Assert.Contains("invalid-date", captureTest, StringComparison.Ordinal);
    }

    [Fact]
    public void AndroidUiSnapshotScript_ClearsExternalSystemDialogsBeforeCapture()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-ui-snapshots.ps1");

        Assert.True(File.Exists(scriptPath), "Android UI snapshot script must exist.");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("Clear-AndroidSnapshotInterference", script, StringComparison.Ordinal);
        Assert.Contains("am", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("force-stop", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("com.android.chrome", script, StringComparison.Ordinal);
        Assert.Contains("CLOSE_SYSTEM_DIALOGS", script, StringComparison.Ordinal);
        Assert.Contains("before launching screenshot instrumentation", script, StringComparison.OrdinalIgnoreCase);
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
    public void AndroidUiSnapshotReportValidator_RejectsPassReportWithoutHierarchyEvidence()
    {
        string repoRoot = FindRepositoryRoot();
        string validatorPath = Path.Combine(repoRoot, "scripts", "validate-android-ui-snapshot-report.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-snapshot-validator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            string reportPath = Path.Combine(tempRoot, "report.md");
            string manifestPath = Path.Combine(tempRoot, "manifest.json");
            WritePassSnapshotReport(reportPath);
            WritePassSnapshotManifest(manifestPath, tempRoot);
            foreach (string screen in RequiredSnapshotPixelScreens)
            {
                File.WriteAllBytes(Path.Combine(tempRoot, screen), NonBlankPngBytes);
            }

            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{validatorPath}\" -ReportPath \"{reportPath}\" -ManifestPath \"{manifestPath}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = repoRoot
            });
            Assert.NotNull(process);
            process.WaitForExit(30_000);

            string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            Assert.NotEqual(0, process.ExitCode);
            Assert.Contains("UI hierarchy", output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("figma-01-splash.xml", output, StringComparison.OrdinalIgnoreCase);
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
        string fakeHierarchy = Path.Combine(tempRoot, "fake-hierarchy.xml");
        string fakeScreenshot = Path.Combine(tempRoot, "fake-screenshot.png");
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(fakeHierarchy, AndroidSnapshotHierarchyFixture);
        File.WriteAllBytes(fakeScreenshot, NonBlankPngBytes);
        File.WriteAllText(fakeAdb, $$"""
@echo off
echo %*>>"{{adbLog}}"
if "%1"=="devices" (
  echo List of devices attached
  echo emulator-5554 device product:test model:FakeDevice
  exit /b 0
)
if "%1"=="pull" (
  if /I "%~x3"==".xml" (
    copy /Y "{{fakeHierarchy}}" "%3" >nul
    exit /b 0
  )
  copy /Y "{{fakeScreenshot}}" "%3" >nul
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
                "11-main-shell-settings.png",
                "12-main-shell-report.png",
                "13-permission-onboarding.png",
                "14-app-detail.png",
                "01-splash-before.png",
                "01-splash-after.png",
                "02-permission-before.png",
                "02-permission-after.png",
                "03-dashboard-before.png",
                "03-dashboard-after.png",
                "04-sessions-before.png",
                "04-sessions-after.png",
                "05-app-detail-before.png",
                "05-app-detail-after.png",
                "06-report-before.png",
                "06-report-after.png",
                "07-settings-before.png",
                "07-settings-after.png"
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
            Assert.Contains("12-main-shell-report.png", manifestText);
            Assert.Contains("13-permission-onboarding.png", manifestText);
            Assert.Contains("14-app-detail.png", manifestText);
            Assert.Contains("beginnerReviewAliases", manifestText);
            Assert.Contains("01-splash-before.png", manifestText);
            Assert.Contains("07-settings-after.png", manifestText);

            string report = File.ReadAllText(Path.Combine(latest, "report.md"));
            Assert.Contains("## Canonical Figma Screen Status", report);
            Assert.Contains("| Figma Splash | PASS | `figma-01-splash.png` |", report);
            Assert.Contains("| Figma Permission | PASS | `figma-02-permission.png` |", report);
            Assert.Contains("| Figma Dashboard | PASS | `figma-03-dashboard.png` |", report);
            Assert.Contains("| Figma Sessions | PASS | `figma-04-sessions.png` |", report);
            Assert.Contains("| Figma App Detail | PASS | `figma-05-app-detail.png` |", report);
            Assert.Contains("| Figma Report | PASS | `figma-06-report.png` |", report);
            Assert.Contains("| Figma Settings | PASS | `figma-07-settings.png` |", report);
            Assert.Contains("screenStatuses", manifestText);
            Assert.Contains("Figma Splash", manifestText);

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
            Assert.Contains("/sdcard/Android/data/com.woong.monitorstack/files/ui-snapshots/12-main-shell-report.png", commands);
            Assert.Contains("/sdcard/Android/data/com.woong.monitorstack/files/ui-snapshots/13-permission-onboarding.png", commands);
            Assert.Contains("/sdcard/Android/data/com.woong.monitorstack/files/ui-snapshots/14-app-detail.png", commands);
            Assert.Contains("shell am force-stop com.android.chrome", commands);
            Assert.Contains("shell am broadcast -a android.intent.action.CLOSE_SYSTEM_DIALOGS", commands);
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

    [Fact]
    public void AndroidUiSnapshotScript_DeviceSerialPinsInstallInstrumentationAndPullCommands()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-ui-snapshots.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-snapshots-{Guid.NewGuid():N}");
        string fakeAdb = Path.Combine(tempRoot, "fake-adb.cmd");
        string fakeGradle = Path.Combine(tempRoot, "gradlew.bat");
        string adbLog = Path.Combine(tempRoot, "adb.log");
        string fakeHierarchy = Path.Combine(tempRoot, "fake-hierarchy.xml");
        string fakeScreenshot = Path.Combine(tempRoot, "fake-screenshot.png");
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(fakeHierarchy, AndroidSnapshotHierarchyFixture);
        File.WriteAllBytes(fakeScreenshot, NonBlankPngBytes);
        File.WriteAllText(fakeAdb, $$"""
@echo off
echo %*>>"{{adbLog}}"
if "%1"=="devices" (
  echo List of devices attached
  echo emulator-5554 device product:test model:FakeDevice
  echo emulator-5556 device product:test model:OtherFakeDevice
  exit /b 0
)
if "%1"=="-s" if "%3"=="pull" (
  if /I "%~x5"==".xml" (
    copy /Y "{{fakeHierarchy}}" "%5" >nul
    exit /b 0
  )
  copy /Y "{{fakeScreenshot}}" "%5" >nul
  exit /b 0
)
exit /b 0
""");
        File.WriteAllText(fakeGradle, "@echo off\r\nexit /b 0\r\n");

        try
        {
            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{tempRoot}\" -AdbPath \"{fakeAdb}\" -GradleWrapperPath \"{fakeGradle}\" -SkipBuild -DeviceSerial emulator-5554")
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

            string commands = File.ReadAllText(adbLog);
            Assert.Contains("-s emulator-5554 shell am instrument -w -e class com.woong.monitorstack.snapshots.SnapshotSeedTest", commands);
            Assert.Contains("-s emulator-5554 shell am instrument -w -e class com.woong.monitorstack.snapshots.SnapshotCaptureTest", commands);
            Assert.Contains("-s emulator-5554 pull /sdcard/Android/data/com.woong.monitorstack/files/ui-snapshots/14-app-detail.png", commands);
            Assert.DoesNotContain("-s emulator-5556", commands);
            Assert.DoesNotContain("No connected Android device", stdout + stderr);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static readonly string[] CanonicalFigmaScreens =
    [
        "figma-01-splash.png",
        "figma-02-permission.png",
        "figma-03-dashboard.png",
        "figma-04-sessions.png",
        "figma-05-app-detail.png",
        "figma-06-report.png",
        "figma-07-settings.png"
    ];

    private static readonly string[] RequiredSnapshotPixelScreens =
    [
        "figma-01-splash.png",
        "figma-02-permission.png",
        "figma-03-dashboard.png",
        "figma-04-sessions.png",
        "figma-05-app-detail.png",
        "figma-06-report.png",
        "figma-07-settings.png",
        "02-dashboard-summary-location.png",
        "06-settings-location-permission.png",
        "09-main-shell.png",
        "10-main-shell-sessions.png",
        "11-main-shell-settings.png",
        "12-main-shell-report.png"
    ];

    private const string NonBlankPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAUSURBVBhXY/jPAEb/gQhIMDD8BwBOwwj4zb9+BwAAAABJRU5ErkJggg==";

    private static readonly byte[] NonBlankPngBytes = Convert.FromBase64String(NonBlankPngBase64);

    private const string AndroidSnapshotHierarchyFixture = """
        <hierarchy>
          <node resource-id="com.woong.monitorstack:id/splashRoot" bounds="[0,0][1080,2200]" />
          <node resource-id="com.woong.monitorstack:id/splashLogoContainer" bounds="[499,320][581,402]" />
          <node resource-id="com.woong.monitorstack:id/appTitleText" text="Woong Monitor" bounds="[330,450][750,520]" />
          <node resource-id="com.woong.monitorstack:id/appSubtitleText" text="Android Focus Tracker" bounds="[350,532][730,570]" />
          <node resource-id="com.woong.monitorstack:id/loadingIndicator" bounds="[512,720][568,776]" />
          <node resource-id="com.woong.monitorstack:id/permissionScrollRoot" bounds="[0,0][1080,2200]" />
          <node resource-id="com.woong.monitorstack:id/permissionTitle" text="Usage Access permission" bounds="[0,0][1080,64]" />
          <node resource-id="com.woong.monitorstack:id/permissionCollectedMetadataText" text="Collects: app name, package name, start time, end time, and duration." bounds="[64,500][1016,540]" />
          <node resource-id="com.woong.monitorstack:id/permissionNotCollectedDataText" text="Does not collect: keyboard input, screen contents, passwords, or touch coordinates." bounds="[64,548][1016,590]" />
          <node resource-id="com.woong.monitorstack:id/openUsageAccessSettingsButton" text="Open Usage Access settings" bounds="[64,1200][1016,1260]" />
          <node resource-id="com.woong.monitorstack:id/currentFocusCard" bounds="[24,160][1056,520]" />
          <node resource-id="com.woong.monitorstack:id/currentFocusTitle" text="Current Focus" bounds="[48,184][320,224]" />
          <node resource-id="com.woong.monitorstack:id/currentForegroundLabel" text="Current foreground app" bounds="[120,240][520,268]" />
          <node resource-id="com.woong.monitorstack:id/currentAppText" text="Woong Monitor" bounds="[120,274][520,306]" />
          <node resource-id="com.woong.monitorstack:id/currentPackageText" text="com.woong.monitorstack" bounds="[120,312][620,338]" />
          <node resource-id="com.woong.monitorstack:id/latestCollectedExternalAppText" text="Chrome" bounds="[120,360][620,390]" />
          <node resource-id="com.woong.monitorstack:id/lastCollectedText" text="Last collected 09:00" bounds="[720,274][1032,306]" />
          <node resource-id="com.woong.monitorstack:id/locationContextCard" bounds="[24,620][1056,1040]" />
          <node resource-id="com.woong.monitorstack:id/locationStatusText" text="Location context captured" bounds="[48,660][840,692]" />
          <node resource-id="com.woong.monitorstack:id/locationMiniMapView" content-desc="No location statistics" bounds="[48,720][1032,864]" />
          <node resource-id="com.woong.monitorstack:id/locationMapProviderStatusText" text="Google Maps API key not configured. Showing local map preview." bounds="[48,876][1032,904]" />
          <node resource-id="com.woong.monitorstack:id/locationLatitudeText" text="Latitude 37.5665" bounds="[48,912][1032,940]" />
          <node resource-id="com.woong.monitorstack:id/locationLongitudeText" text="Longitude 126.9780" bounds="[48,944][1032,972]" />
          <node resource-id="com.woong.monitorstack:id/locationCapturedAtText" text="Captured 09:00" bounds="[48,976][1032,1004]" />
          <node resource-id="com.woong.monitorstack:id/reportTitle" text="Report" bounds="[24,96][420,150]" />
          <node resource-id="com.woong.monitorstack:id/reportSevenDayButton" text="7d" bounds="[24,172][160,228]" />
          <node resource-id="com.woong.monitorstack:id/reportTrendChartCard" bounds="[24,420][1056,620]" />
          <node resource-id="com.woong.monitorstack:id/reportTopAppsCard" bounds="[24,640][1056,900]" />
          <node resource-id="com.woong.monitorstack:id/settingsTitle" text="Settings" bounds="[24,96][420,150]" />
          <node resource-id="com.woong.monitorstack:id/permissionsSettingsCard" bounds="[24,160][1056,360]" />
          <node resource-id="com.woong.monitorstack:id/collectionSettingsCard" bounds="[24,380][1056,520]" />
          <node resource-id="com.woong.monitorstack:id/syncSettingsCard" bounds="[24,540][1056,760]" />
          <node resource-id="com.woong.monitorstack:id/locationSettingsCard" bounds="[24,780][1056,1040]" />
          <node resource-id="com.woong.monitorstack:id/locationContextDefaultText" text="Location context is off by default." bounds="[48,820][1032,850]" />
          <node resource-id="com.woong.monitorstack:id/locationCoordinateBoundaryText" text="Latitude/longitude are not stored unless location context is enabled." bounds="[48,856][1032,886]" />
          <node resource-id="com.woong.monitorstack:id/preciseLocationOptInText" text="Precise latitude/longitude requires a separate explicit opt-in." bounds="[48,892][1032,922]" />
          <node resource-id="com.woong.monitorstack:id/locationContextCheckBox" text="Store optional location context" bounds="[48,928][1032,980]" />
          <node resource-id="com.woong.monitorstack:id/preciseLatitudeLongitudeCheckBox" text="Store precise latitude/longitude" bounds="[48,984][1032,1036]" />
          <node resource-id="com.woong.monitorstack:id/requestLocationPermissionButton" text="Allow location permission" bounds="[48,1040][1032,1092]" />
          <node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]">
            <node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" text="Dashboard" bounds="[40,2210][240,2240]" />
            <node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" text="Sessions" bounds="[310,2210][510,2240]" />
            <node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" text="Report" bounds="[580,2210][780,2240]" />
            <node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" text="Settings" bounds="[850,2210][1050,2240]" />
          </node>
        </hierarchy>
        """;

    private static void WritePassSnapshotReport(string path)
    {
        List<string> lines =
        [
            "# Android UI Snapshot Report",
            "",
            "Status: PASS",
            "",
            "## Canonical Figma Screen Status",
            "",
            "| Screen | Status | Artifact | Note |",
            "|---|---|---|---|"
        ];

        foreach (string screen in CanonicalFigmaScreens)
        {
            string displayName = screen
                .Replace("figma-0", "Figma ", StringComparison.Ordinal)
                .Replace(".png", "", StringComparison.Ordinal);
            lines.Add($"| {displayName} | PASS | `{screen}` | Captured non-empty local screenshot. |");
        }

        File.WriteAllLines(path, lines);
    }

    private static void WritePassSnapshotManifest(string path, string outputRoot)
    {
        var manifest = new
        {
            status = "PASS",
            output = outputRoot,
            screenStatuses = CanonicalFigmaScreens.Select(screen => new
            {
                name = screen,
                status = "PASS",
                fileName = screen,
                note = "Captured non-empty local screenshot."
            }).ToArray(),
            screenshots = RequiredSnapshotPixelScreens.Select(screen => new
            {
                name = screen,
                fileName = screen,
                path = Path.Combine(outputRoot, screen),
                capture = "test"
            }).ToArray(),
            hierarchies = Array.Empty<object>()
        };

        File.WriteAllText(path, JsonSerializer.Serialize(manifest));
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
