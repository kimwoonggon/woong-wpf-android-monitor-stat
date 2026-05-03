using System.Diagnostics;
using System.Text.Json;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class AndroidAppSwitchQaScriptTests
{
    [Fact]
    public void AndroidAppSwitchQaScript_DocumentsPrivacySafeArtifactContract()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-app-switch-qa.ps1");
        string instrumentationPath = Path.Combine(
            repoRoot,
            "android",
            "app",
            "src",
            "androidTest",
            "java",
            "com",
            "woong",
            "monitorstack",
            "usage",
            "AppSwitchQaEvidenceTest.kt");

        Assert.True(File.Exists(scriptPath), "Android app-switch QA script must exist.");
        Assert.True(File.Exists(instrumentationPath), "Android app-switch QA instrumentation must exist.");

        string script = File.ReadAllText(scriptPath);
        string instrumentation = File.ReadAllText(instrumentationPath);

        string[] expectedArtifacts =
        [
            "artifacts/android-app-switch-qa",
            "report.md",
            "manifest.json",
            "room-assertions.json",
            "foreground-before.txt",
            "foreground-during-chrome.txt",
            "foreground-after-return.txt",
            "process-before.txt",
            "process-during-chrome.txt",
            "process-after-return.txt",
            "package-manager-preflight.txt",
            "logcat-crash.txt",
            "logcat-woong-app.txt",
            "meminfo-after-app-switch.txt",
            "gfxinfo-after-app-switch.txt",
            "dashboard-current-focus-evidence.json",
            "dashboard-current-focus-after-chrome-return.png",
            "dashboard-current-focus-after-chrome-return.xml",
            "dashboard-after-app-switch.png",
            "dashboard-after-app-switch.xml",
            "sessions-after-app-switch.png",
            "sessions-after-app-switch.xml"
        ];

        foreach (string artifact in expectedArtifacts)
        {
            Assert.Contains(artifact, script, StringComparison.Ordinal);
        }

        Assert.Contains("about:blank", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dumpsys window", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("logcat", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[int]$InstallTimeoutSeconds = 180", script, StringComparison.Ordinal);
        Assert.Contains("[int]$PackageManagerPreflightTimeoutSeconds = 10", script, StringComparison.Ordinal);
        Assert.Contains("install-debug-apk-stdout.txt", script, StringComparison.Ordinal);
        Assert.Contains("install-debug-apk-stderr.txt", script, StringComparison.Ordinal);
        Assert.Contains("install-android-test-apk-stdout.txt", script, StringComparison.Ordinal);
        Assert.Contains("install-android-test-apk-stderr.txt", script, StringComparison.Ordinal);
        Assert.Contains("package-manager-preflight.txt", script, StringComparison.Ordinal);
        Assert.Contains("AppSwitchQaEvidenceTest#collectUsageStatsAfterChromeReturnPersistsFocusSessionAndOutbox", script, StringComparison.Ordinal);
        Assert.Contains("AppSwitchQaEvidenceTest#captureWoongDashboardAndSessionsOnlyAfterReturn", script, StringComparison.Ordinal);
        Assert.Contains("AndroidUsageCollectionRunner.create", instrumentation, StringComparison.Ordinal);
        Assert.Contains("room-assertions.json", instrumentation, StringComparison.Ordinal);
        Assert.Contains("dashboard-after-app-switch.png", instrumentation, StringComparison.Ordinal);
        Assert.Contains("sessions-after-app-switch.png", instrumentation, StringComparison.Ordinal);

        string combined = script + Environment.NewLine + instrumentation;
        Assert.Contains("No Chrome screenshots", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("screencap", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("uiautomator dump", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("input text", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AndroidAppSwitchQaScript_WhenNoDeviceConnected_WritesBlockedArtifacts()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-app-switch-qa.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-app-switch-{Guid.NewGuid():N}");
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
            Assert.True(File.Exists(Path.Combine(latest, "room-assertions.json")));
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
    public void AndroidAppSwitchQaScript_WhenDeviceConnected_RunsAppSwitchInstrumentationAndPullsEvidence()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-app-switch-qa.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-app-switch-{Guid.NewGuid():N}");
        string fakeAdb = Path.Combine(tempRoot, "fake-adb.cmd");
        string fakeGradle = Path.Combine(tempRoot, "gradlew.bat");
        string adbLog = Path.Combine(tempRoot, "adb.log");
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(fakeAdb, FakeAdbScript(adbLog, serialAware: false));
        File.WriteAllText(fakeGradle, "@echo off\r\nexit /b 0\r\n");

        try
        {
            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{tempRoot}\" -AdbPath \"{fakeAdb}\" -GradleWrapperPath \"{fakeGradle}\" -SkipBuild -ChromeForegroundSeconds 0")
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
            Assert.Contains("Android app-switch QA artifacts", stdout + stderr);

            string latest = Path.Combine(tempRoot, "latest");
            string[] expectedFiles =
            [
                "report.md",
                "manifest.json",
                "room-assertions.json",
                "foreground-before.txt",
                "foreground-during-chrome.txt",
                "foreground-after-return.txt",
                "process-before.txt",
                "process-during-chrome.txt",
                "process-after-return.txt",
                "logcat-crash.txt",
                "logcat-woong-app.txt",
                "meminfo-after-app-switch.txt",
                "gfxinfo-after-app-switch.txt",
                "dashboard-current-focus-evidence.json",
                "dashboard-current-focus-after-chrome-return.png",
                "dashboard-current-focus-after-chrome-return.xml",
                "dashboard-after-app-switch.png",
                "dashboard-after-app-switch.xml",
                "sessions-after-app-switch.png",
                "sessions-after-app-switch.xml"
            ];

            foreach (string fileName in expectedFiles)
            {
                Assert.True(File.Exists(Path.Combine(latest, fileName)), $"{fileName} should exist.");
            }

            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(latest, "manifest.json")));
            Assert.Equal("PASS", manifest.RootElement.GetProperty("status").GetString());
            Assert.Equal("com.woong.monitorstack", manifest.RootElement.GetProperty("packageName").GetString());
            Assert.Equal("com.android.chrome", manifest.RootElement.GetProperty("chromePackageName").GetString());

            string commands = File.ReadAllText(adbLog);
            Assert.Contains("shell appops set com.woong.monitorstack GET_USAGE_STATS allow", commands);
            Assert.Contains("shell am force-stop com.woong.monitorstack", commands);
            Assert.Contains("shell am force-stop com.android.chrome", commands);
            Assert.Contains("shell am start -n com.woong.monitorstack/.MainActivity", commands);
            Assert.Contains("shell am start -W -n com.woong.monitorstack/.MainActivity", commands);
            Assert.Contains("shell am start -a android.intent.action.VIEW -d about:blank -p com.android.chrome", commands);
            Assert.Contains("shell am instrument -w -e class com.woong.monitorstack.usage.AppSwitchQaEvidenceTest#prepareCleanRoomForAppSwitchQa", commands);
            Assert.Contains("shell am instrument -w -e class com.woong.monitorstack.usage.AppSwitchQaEvidenceTest#collectUsageStatsAfterChromeReturnPersistsFocusSessionAndOutbox", commands);
            Assert.Contains("shell am instrument -w -e class com.woong.monitorstack.usage.AppSwitchQaEvidenceTest#dashboardAfterChromeReturnShowsWoongAsCurrentAndChromeAsLatestExternal", commands);
            Assert.Contains("shell am instrument -w -e class com.woong.monitorstack.usage.AppSwitchQaEvidenceTest#captureWoongDashboardAndSessionsOnlyAfterReturn", commands);
            Assert.Contains("pull /sdcard/Android/data/com.woong.monitorstack/files/app-switch-qa/room-assertions.json", commands);
            Assert.Contains("pull /sdcard/Android/data/com.woong.monitorstack/files/app-switch-qa/dashboard-current-focus-evidence.json", commands);
            Assert.Contains("pull /sdcard/Android/data/com.woong.monitorstack/files/app-switch-qa/dashboard-current-focus-after-chrome-return.xml", commands);
            Assert.Contains("pull /sdcard/Android/data/com.woong.monitorstack/files/app-switch-qa/dashboard-after-app-switch.png", commands);
            Assert.Contains("pull /sdcard/Android/data/com.woong.monitorstack/files/app-switch-qa/sessions-after-app-switch.xml", commands);
            Assert.Contains("logcat -d -b crash", commands);
            Assert.Contains("shell dumpsys meminfo com.woong.monitorstack", commands);
            Assert.Contains("shell dumpsys gfxinfo com.woong.monitorstack", commands);
            Assert.DoesNotContain("screencap", commands, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("uiautomator", commands, StringComparison.OrdinalIgnoreCase);

            string[] commandLines = File.ReadAllLines(adbLog);
            int chromeLaunchIndex = Array.FindIndex(
                commandLines,
                line => line.Contains(
                    "shell am start -a android.intent.action.VIEW -d about:blank -p com.android.chrome",
                    StringComparison.Ordinal));
            int woongReturnIndex = Array.FindIndex(
                commandLines,
                chromeLaunchIndex + 1,
                line => line.Contains(
                    "shell am start -W -n com.woong.monitorstack/.MainActivity",
                    StringComparison.Ordinal));
            int foregroundAfterReturnIndex = Array.FindIndex(
                commandLines,
                woongReturnIndex + 1,
                line => line.Contains("dumpsys window", StringComparison.Ordinal));
            int captureIndex = Array.FindIndex(
                commandLines,
                woongReturnIndex + 1,
                line => line.Contains(
                    "AppSwitchQaEvidenceTest#captureWoongDashboardAndSessionsOnlyAfterReturn",
                    StringComparison.Ordinal));

            Assert.True(chromeLaunchIndex >= 0, "Chrome must launch during app-switch QA.");
            Assert.True(woongReturnIndex > chromeLaunchIndex, "Woong must be explicitly relaunched after Chrome.");
            Assert.True(
                foregroundAfterReturnIndex > woongReturnIndex,
                "Foreground-after-return proof must be captured after the explicit Woong return command.");
            Assert.True(
                captureIndex > foregroundAfterReturnIndex,
                "Woong UI capture must run only after foreground-after-return proof.");
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
    public void AndroidAppSwitchQaScript_WhenRoomAssertionsFail_WritesFailReportAndStopsBeforeScreenshotAcceptance()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-app-switch-qa.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-app-switch-{Guid.NewGuid():N}");
        string fakeAdb = Path.Combine(tempRoot, "fake-adb.cmd");
        string fakeGradle = Path.Combine(tempRoot, "gradlew.bat");
        string adbLog = Path.Combine(tempRoot, "adb.log");
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(fakeAdb, FakeAdbScriptWithFailingRoomAssertions(adbLog));
        File.WriteAllText(fakeGradle, "@echo off\r\nexit /b 0\r\n");

        try
        {
            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{tempRoot}\" -AdbPath \"{fakeAdb}\" -GradleWrapperPath \"{fakeGradle}\" -SkipBuild -ChromeForegroundSeconds 0")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = repoRoot
            });
            Assert.NotNull(process);
            Assert.True(process.WaitForExit(30_000), "The failing room-assertions scenario should finish quickly.");

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            Assert.NotEqual(0, process.ExitCode);
            Assert.Contains("Room assertions failed", stdout + stderr);
            Assert.Contains("Android app-switch QA artifacts", stdout + stderr);

            string latest = Path.Combine(tempRoot, "latest");
            string report = File.ReadAllText(Path.Combine(latest, "report.md"));
            string roomAssertions = File.ReadAllText(Path.Combine(latest, "room-assertions.json"));
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(latest, "manifest.json")));

            Assert.Equal("FAIL", manifest.RootElement.GetProperty("status").GetString());
            Assert.Equal("room-assertions-failed", manifest.RootElement.GetProperty("classification").GetString());
            Assert.Contains("syncOutboxChromeRows=0", manifest.RootElement.GetProperty("blockedReason").GetString());
            Assert.Contains("BLOCKED/FAIL: Room assertions failed", report);
            Assert.Contains("\"status\":\"FAIL\"", roomAssertions);

            string commands = File.ReadAllText(adbLog);
            Assert.Contains("pull /sdcard/Android/data/com.woong.monitorstack/files/app-switch-qa/room-assertions.json", commands);
            Assert.DoesNotContain("AppSwitchQaEvidenceTest#captureWoongDashboardAndSessionsOnlyAfterReturn", commands);
            Assert.DoesNotContain("dashboard-after-app-switch.png", commands);
            Assert.DoesNotContain("screencap", commands, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("uiautomator", commands, StringComparison.OrdinalIgnoreCase);
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
    public void AndroidAppSwitchQaScript_WhenWoongScreenshotPullIsBlank_RerunsSafeWoongCaptureAndRetriesPull()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-app-switch-qa.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-app-switch-{Guid.NewGuid():N}");
        string fakeAdb = Path.Combine(tempRoot, "fake-adb.cmd");
        string fakeGradle = Path.Combine(tempRoot, "gradlew.bat");
        string adbLog = Path.Combine(tempRoot, "adb.log");
        string blankPullMarker = Path.Combine(tempRoot, "blank-dashboard-pull-count.txt");
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(fakeAdb, FakeAdbScriptWithBlankDashboardScreenshotOnce(adbLog, blankPullMarker));
        File.WriteAllText(fakeGradle, "@echo off\r\nexit /b 0\r\n");

        try
        {
            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{tempRoot}\" -AdbPath \"{fakeAdb}\" -GradleWrapperPath \"{fakeGradle}\" -SkipBuild -ChromeForegroundSeconds 0")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = repoRoot
            });
            Assert.NotNull(process);
            Assert.True(process.WaitForExit(30_000), "The blank screenshot retry scenario should finish quickly.");

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            Assert.Equal(0, process.ExitCode);
            Assert.Contains("Android app-switch QA artifacts", stdout + stderr);

            string latest = Path.Combine(tempRoot, "latest");
            string dashboardScreenshot = Path.Combine(latest, "dashboard-after-app-switch.png");
            string report = File.ReadAllText(Path.Combine(latest, "report.md"));
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(latest, "manifest.json")));

            Assert.Equal("PASS", manifest.RootElement.GetProperty("status").GetString());
            Assert.True(new FileInfo(dashboardScreenshot).Length > 0, "Dashboard screenshot should be non-empty after retry.");
            Assert.Contains("Blank screenshot pull detected for dashboard-after-app-switch.png", report);

            string[] commandLines = File.ReadAllLines(adbLog);
            int firstDashboardPull = Array.FindIndex(
                commandLines,
                line => line.Contains("pull /sdcard/Android/data/com.woong.monitorstack/files/app-switch-qa/dashboard-after-app-switch.png", StringComparison.Ordinal));
            int retryCapture = Array.FindIndex(
                commandLines,
                firstDashboardPull + 1,
                line => line.Contains("AppSwitchQaEvidenceTest#captureWoongDashboardAndSessionsOnlyAfterReturn", StringComparison.Ordinal));
            int secondDashboardPull = Array.FindIndex(
                commandLines,
                retryCapture + 1,
                line => line.Contains("pull /sdcard/Android/data/com.woong.monitorstack/files/app-switch-qa/dashboard-after-app-switch.png", StringComparison.Ordinal));

            Assert.True(firstDashboardPull >= 0, "Initial Dashboard screenshot pull should run.");
            Assert.True(retryCapture > firstDashboardPull, "Blank screenshot retry should rerun only the Woong capture instrumentation.");
            Assert.True(secondDashboardPull > retryCapture, "Dashboard screenshot should be pulled again after retry capture.");
            Assert.DoesNotContain("screencap", File.ReadAllText(adbLog), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("uiautomator", File.ReadAllText(adbLog), StringComparison.OrdinalIgnoreCase);
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
    public void AndroidAppSwitchQaScript_WhenWoongScreenshotPullIsPerceptuallyBlank_RerunsSafeWoongCaptureAndRetriesPull()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-app-switch-qa.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-app-switch-{Guid.NewGuid():N}");
        string fakeAdb = Path.Combine(tempRoot, "fake-adb.cmd");
        string fakeGradle = Path.Combine(tempRoot, "gradlew.bat");
        string adbLog = Path.Combine(tempRoot, "adb.log");
        string blankPullMarker = Path.Combine(tempRoot, "perceptual-blank-dashboard-pull-count.txt");
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(fakeAdb, FakeAdbScriptWithPerceptuallyBlankDashboardScreenshotOnce(adbLog, blankPullMarker));
        File.WriteAllText(fakeGradle, "@echo off\r\nexit /b 0\r\n");

        try
        {
            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{tempRoot}\" -AdbPath \"{fakeAdb}\" -GradleWrapperPath \"{fakeGradle}\" -SkipBuild -ChromeForegroundSeconds 0")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = repoRoot
            });
            Assert.NotNull(process);
            Assert.True(process.WaitForExit(30_000), "The perceptual blank screenshot retry scenario should finish quickly.");

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            Assert.Equal(0, process.ExitCode);
            Assert.Contains("Android app-switch QA artifacts", stdout + stderr);

            string latest = Path.Combine(tempRoot, "latest");
            string dashboardScreenshot = Path.Combine(latest, "dashboard-after-app-switch.png");
            string report = File.ReadAllText(Path.Combine(latest, "report.md"));
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(latest, "manifest.json")));

            Assert.Equal("PASS", manifest.RootElement.GetProperty("status").GetString());
            Assert.True(new FileInfo(dashboardScreenshot).Length > 0, "Dashboard screenshot should exist after retry.");
            Assert.Contains("Perceptually blank screenshot detected for dashboard-after-app-switch.png", report);

            string[] commandLines = File.ReadAllLines(adbLog);
            int firstDashboardPull = Array.FindIndex(
                commandLines,
                line => line.Contains("pull /sdcard/Android/data/com.woong.monitorstack/files/app-switch-qa/dashboard-after-app-switch.png", StringComparison.Ordinal));
            int retryCapture = Array.FindIndex(
                commandLines,
                firstDashboardPull + 1,
                line => line.Contains("AppSwitchQaEvidenceTest#captureWoongDashboardAndSessionsOnlyAfterReturn", StringComparison.Ordinal));
            int secondDashboardPull = Array.FindIndex(
                commandLines,
                retryCapture + 1,
                line => line.Contains("pull /sdcard/Android/data/com.woong.monitorstack/files/app-switch-qa/dashboard-after-app-switch.png", StringComparison.Ordinal));

            Assert.True(firstDashboardPull >= 0, "Initial Dashboard screenshot pull should run.");
            Assert.True(retryCapture > firstDashboardPull, "Perceptual blank retry should rerun only the Woong capture instrumentation.");
            Assert.True(secondDashboardPull > retryCapture, "Dashboard screenshot should be pulled again after retry capture.");
            Assert.DoesNotContain("screencap", File.ReadAllText(adbLog), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("uiautomator", File.ReadAllText(adbLog), StringComparison.OrdinalIgnoreCase);
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
    public void AndroidAppSwitchQaScript_WhenWoongPidChangesAfterChromeReturn_ClassifiesEmulatorStabilityBlocked()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-app-switch-qa.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-app-switch-{Guid.NewGuid():N}");
        string fakeAdb = Path.Combine(tempRoot, "fake-adb.cmd");
        string fakeGradle = Path.Combine(tempRoot, "gradlew.bat");
        string adbLog = Path.Combine(tempRoot, "adb.log");
        string processMetadataCount = Path.Combine(tempRoot, "process-metadata-count.txt");
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(fakeAdb, FakeAdbScriptWithWoongPidChangeAfterChromeReturn(adbLog, processMetadataCount, androidRuntimeCrash: false));
        File.WriteAllText(fakeGradle, "@echo off\r\nexit /b 0\r\n");

        try
        {
            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{tempRoot}\" -AdbPath \"{fakeAdb}\" -GradleWrapperPath \"{fakeGradle}\" -SkipBuild -ChromeForegroundSeconds 0")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = repoRoot
            });
            Assert.NotNull(process);
            Assert.True(process.WaitForExit(30_000), "The emulator-kill classification scenario should finish quickly.");

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            Assert.Equal(0, process.ExitCode);
            Assert.Contains("Android app-switch QA artifacts", stdout + stderr);

            string latest = Path.Combine(tempRoot, "latest");
            string report = File.ReadAllText(Path.Combine(latest, "report.md"));
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(latest, "manifest.json")));

            Assert.Equal("BLOCKED", manifest.RootElement.GetProperty("status").GetString());
            Assert.Equal("emulator-stability-process-death", manifest.RootElement.GetProperty("classification").GetString());
            Assert.Contains("Woong process changed during Chrome app-switch", manifest.RootElement.GetProperty("blockedReason").GetString());
            Assert.Contains("emulator stability", report, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("restart the emulator", report, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(Path.Combine(latest, "logcat-crash.txt")));
            Assert.Contains("fake crash buffer without product crash", File.ReadAllText(Path.Combine(latest, "logcat-crash.txt")));

            string commands = File.ReadAllText(adbLog);
            Assert.Contains("process-before.txt", File.ReadAllText(Path.Combine(latest, "report.md")));
            Assert.Contains("echo Woong pid:", commands);
            Assert.Contains("logcat -d -b crash", commands);
            Assert.DoesNotContain("AppSwitchQaEvidenceTest#collectUsageStatsAfterChromeReturnPersistsFocusSessionAndOutbox", commands);
            Assert.DoesNotContain("AppSwitchQaEvidenceTest#captureWoongDashboardAndSessionsOnlyAfterReturn", commands);
            Assert.DoesNotContain("screencap", commands, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("uiautomator", commands, StringComparison.OrdinalIgnoreCase);
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
    public void AndroidAppSwitchQaScript_WhenWoongPidChangesAndAndroidRuntimeCrashExists_ClassifiesProductFailure()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-app-switch-qa.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-app-switch-{Guid.NewGuid():N}");
        string fakeAdb = Path.Combine(tempRoot, "fake-adb.cmd");
        string fakeGradle = Path.Combine(tempRoot, "gradlew.bat");
        string adbLog = Path.Combine(tempRoot, "adb.log");
        string processMetadataCount = Path.Combine(tempRoot, "process-metadata-count.txt");
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(fakeAdb, FakeAdbScriptWithWoongPidChangeAfterChromeReturn(adbLog, processMetadataCount, androidRuntimeCrash: true));
        File.WriteAllText(fakeGradle, "@echo off\r\nexit /b 0\r\n");

        try
        {
            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{tempRoot}\" -AdbPath \"{fakeAdb}\" -GradleWrapperPath \"{fakeGradle}\" -SkipBuild -ChromeForegroundSeconds 0")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = repoRoot
            });
            Assert.NotNull(process);
            Assert.True(process.WaitForExit(30_000), "The product-crash classification scenario should finish quickly.");

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            Assert.Equal(0, process.ExitCode);
            Assert.Contains("Android app-switch QA artifacts", stdout + stderr);

            string latest = Path.Combine(tempRoot, "latest");
            string report = File.ReadAllText(Path.Combine(latest, "report.md"));
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(latest, "manifest.json")));

            Assert.Equal("FAIL", manifest.RootElement.GetProperty("status").GetString());
            Assert.Equal("product-crash-process-death", manifest.RootElement.GetProperty("classification").GetString());
            Assert.Contains("AndroidRuntime crash evidence was found", manifest.RootElement.GetProperty("blockedReason").GetString());
            Assert.Contains("Inspect logcat-crash.txt", report);
            Assert.Contains("AndroidRuntime", File.ReadAllText(Path.Combine(latest, "logcat-crash.txt")));

            string commands = File.ReadAllText(adbLog);
            Assert.DoesNotContain("AppSwitchQaEvidenceTest#collectUsageStatsAfterChromeReturnPersistsFocusSessionAndOutbox", commands);
            Assert.DoesNotContain("AppSwitchQaEvidenceTest#captureWoongDashboardAndSessionsOnlyAfterReturn", commands);
            Assert.DoesNotContain("screencap", commands, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("uiautomator", commands, StringComparison.OrdinalIgnoreCase);
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
    public void AndroidAppSwitchQaScript_WhenCoreEvidencePassedButWoongPidUnavailable_WarnsAndCapturesFallbackLogcat()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-app-switch-qa.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-app-switch-{Guid.NewGuid():N}");
        string fakeAdb = Path.Combine(tempRoot, "fake-adb.cmd");
        string fakeGradle = Path.Combine(tempRoot, "gradlew.bat");
        string adbLog = Path.Combine(tempRoot, "adb.log");
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(fakeAdb, FakeAdbScriptWithMissingWoongPidAfterEvidence(adbLog));
        File.WriteAllText(fakeGradle, "@echo off\r\nexit /b 0\r\n");

        try
        {
            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{tempRoot}\" -AdbPath \"{fakeAdb}\" -GradleWrapperPath \"{fakeGradle}\" -SkipBuild -DeviceSerial emulator-5554 -ChromeForegroundSeconds 0")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = repoRoot
            });
            Assert.NotNull(process);
            Assert.True(process.WaitForExit(30_000), "The missing-pid diagnostic scenario should finish quickly.");

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            Assert.Equal(0, process.ExitCode);
            Assert.Contains("Android app-switch QA artifacts", stdout + stderr);

            string latest = Path.Combine(tempRoot, "latest");
            string report = File.ReadAllText(Path.Combine(latest, "report.md"));
            string fallbackLogcat = File.ReadAllText(Path.Combine(latest, "logcat-woong-app.txt"));
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(latest, "manifest.json")));

            Assert.Equal("PASS", manifest.RootElement.GetProperty("status").GetString());
            Assert.Contains("PASS: Android app-switch QA commands completed.", report);
            Assert.Contains("WARNING", report);
            Assert.Contains("Woong pid was unavailable", report);
            Assert.Contains("package-filtered logcat fallback", fallbackLogcat);
            Assert.Contains("fake fallback logcat for com.woong.monitorstack", fallbackLogcat);

            string commands = File.ReadAllText(adbLog);
            Assert.Contains("-s emulator-5554 shell pidof com.woong.monitorstack", commands);
            Assert.Contains("logcat -d -v time | grep -F 'com.woong.monitorstack' || true", commands);
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
    public void AndroidAppSwitchQaScript_WhenPackageManagerPreflightTimesOut_WritesBlockedAdvice()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-app-switch-qa.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-app-switch-{Guid.NewGuid():N}");
        string fakeAdb = Path.Combine(tempRoot, "fake-adb.cmd");
        string fakeGradle = Path.Combine(tempRoot, "gradlew.bat");
        string adbLog = Path.Combine(tempRoot, "adb.log");
        Directory.CreateDirectory(tempRoot);
        CreateFakeApks(tempRoot, sameContent: true);
        File.WriteAllText(fakeAdb, FakeAdbScriptWithPackageManagerTimeout(adbLog));
        File.WriteAllText(fakeGradle, "@echo off\r\nexit /b 0\r\n");

        try
        {
            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{tempRoot}\" -AdbPath \"{fakeAdb}\" -GradleWrapperPath \"{fakeGradle}\" -DeviceSerial emulator-5554 -ChromeForegroundSeconds 0 -PackageManagerPreflightTimeoutSeconds 1")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = repoRoot
            });
            Assert.NotNull(process);
            Assert.True(process.WaitForExit(30_000), "The package-manager timeout scenario should finish quickly.");

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            Assert.Equal(0, process.ExitCode);
            Assert.Contains("Android app-switch QA artifacts", stdout + stderr);

            string latest = Path.Combine(tempRoot, "latest");
            string report = File.ReadAllText(Path.Combine(latest, "report.md"));
            string preflight = File.ReadAllText(Path.Combine(latest, "package-manager-preflight.txt"));
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(latest, "manifest.json")));

            Assert.Equal("BLOCKED", manifest.RootElement.GetProperty("status").GetString());
            Assert.Equal(1, manifest.RootElement.GetProperty("packageManagerPreflightTimeoutSeconds").GetInt32());
            Assert.Contains("Package manager preflight timed out after 1 seconds", report);
            Assert.Contains("reboot the emulator", report, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("pm list packages com.woong.monitorstack", preflight);
            Assert.Contains("timedOut: True", preflight);

            string commands = File.ReadAllText(adbLog);
            Assert.Contains("-s emulator-5554 shell pm list packages com.woong.monitorstack", commands);
            Assert.DoesNotContain(" install -r ", commands, StringComparison.Ordinal);
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
    public void AndroidAppSwitchQaScript_WhenInstallTimesOut_WritesBlockedInstallDiagnostics()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-app-switch-qa.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-app-switch-{Guid.NewGuid():N}");
        string fakeAdb = Path.Combine(tempRoot, "fake-adb.cmd");
        string fakeGradle = Path.Combine(tempRoot, "gradlew.bat");
        string adbLog = Path.Combine(tempRoot, "adb.log");
        Directory.CreateDirectory(tempRoot);
        CreateFakeApks(tempRoot, sameContent: true);
        File.WriteAllText(fakeAdb, FakeAdbScriptWithInstallTimeout(adbLog));
        File.WriteAllText(fakeGradle, "@echo off\r\nexit /b 0\r\n");

        try
        {
            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{tempRoot}\" -AdbPath \"{fakeAdb}\" -GradleWrapperPath \"{fakeGradle}\" -DeviceSerial emulator-5554 -ChromeForegroundSeconds 0 -InstallTimeoutSeconds 1")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = repoRoot
            });
            Assert.NotNull(process);
            Assert.True(process.WaitForExit(30_000), "The install-timeout scenario should finish quickly.");

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            Assert.Equal(0, process.ExitCode);
            Assert.Contains("Android app-switch QA artifacts", stdout + stderr);

            string latest = Path.Combine(tempRoot, "latest");
            string report = File.ReadAllText(Path.Combine(latest, "report.md"));
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(latest, "manifest.json")));

            Assert.Equal("BLOCKED", manifest.RootElement.GetProperty("status").GetString());
            Assert.Equal(1, manifest.RootElement.GetProperty("installTimeoutSeconds").GetInt32());
            Assert.Contains("Install debug APK timed out after 1 seconds", report);
            Assert.Contains("install-debug-apk-stdout.txt", report);
            Assert.Contains("install-debug-apk-stderr.txt", report);
            Assert.Contains(
                "fake install stdout before timeout",
                File.ReadAllText(Path.Combine(latest, "install-debug-apk-stdout.txt")));
            Assert.Contains(
                "fake install stderr before timeout",
                File.ReadAllText(Path.Combine(latest, "install-debug-apk-stderr.txt")));

            string commands = File.ReadAllText(adbLog);
            Assert.Contains("-s emulator-5554 install -r", commands);
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
    public void AndroidAppSwitchQaScript_WhenInstalledApksAreCurrent_SkipsReinstallAndWritesDiagnostics()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-app-switch-qa.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-app-switch-{Guid.NewGuid():N}");
        string fakeAdb = Path.Combine(tempRoot, "fake-adb.cmd");
        string fakeGradle = Path.Combine(tempRoot, "gradlew.bat");
        string adbLog = Path.Combine(tempRoot, "adb.log");
        Directory.CreateDirectory(tempRoot);
        string apkPath = CreateFakeApks(tempRoot, sameContent: true).debugApk;
        string currentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(apkPath)))
            .ToLowerInvariant();
        File.WriteAllText(fakeAdb, FakeAdbScriptWithCurrentApks(adbLog, Path.Combine(tempRoot, "hash-check-count.txt"), currentHash));
        File.WriteAllText(fakeGradle, "@echo off\r\nexit /b 0\r\n");

        try
        {
            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{tempRoot}\" -AdbPath \"{fakeAdb}\" -GradleWrapperPath \"{fakeGradle}\" -DeviceSerial emulator-5554 -ChromeForegroundSeconds 0")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = repoRoot
            });
            Assert.NotNull(process);
            Assert.True(process.WaitForExit(30_000), "The current-APK scenario should finish quickly.");

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            Assert.Equal(0, process.ExitCode);
            Assert.Contains("Android app-switch QA artifacts", stdout + stderr);

            string latest = Path.Combine(tempRoot, "latest");
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(latest, "manifest.json")));
            Assert.Equal("PASS", manifest.RootElement.GetProperty("status").GetString());

            string commands = File.ReadAllText(adbLog);
            Assert.DoesNotContain(" install -r ", commands, StringComparison.Ordinal);
            Assert.Contains(
                "Skipped debug APK install because installed APK hash matches",
                File.ReadAllText(Path.Combine(latest, "install-debug-apk-stdout.txt")));
            Assert.Contains(
                "Skipped debug androidTest APK install because installed APK hash matches",
                File.ReadAllText(Path.Combine(latest, "install-android-test-apk-stdout.txt")));
            Assert.True(File.Exists(Path.Combine(latest, "install-debug-apk-stderr.txt")));
            Assert.True(File.Exists(Path.Combine(latest, "install-android-test-apk-stderr.txt")));
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
    public void AndroidAppSwitchQaScript_DeviceSerialPinsAllDeviceScopedCommands()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-android-app-switch-qa.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-android-app-switch-{Guid.NewGuid():N}");
        string fakeAdb = Path.Combine(tempRoot, "fake-adb.cmd");
        string fakeGradle = Path.Combine(tempRoot, "gradlew.bat");
        string adbLog = Path.Combine(tempRoot, "adb.log");
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(fakeAdb, FakeAdbScript(adbLog, serialAware: true));
        File.WriteAllText(fakeGradle, "@echo off\r\nexit /b 0\r\n");

        try
        {
            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{tempRoot}\" -AdbPath \"{fakeAdb}\" -GradleWrapperPath \"{fakeGradle}\" -SkipBuild -DeviceSerial emulator-5554 -ChromeForegroundSeconds 0")
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
            Assert.DoesNotContain("No connected Android device", stdout + stderr);

            string commands = File.ReadAllText(adbLog);
            Assert.Contains("-s emulator-5554 shell am start -n com.woong.monitorstack/.MainActivity", commands);
            Assert.Contains("-s emulator-5554 shell am start -W -n com.woong.monitorstack/.MainActivity", commands);
            Assert.Contains("-s emulator-5554 shell am start -a android.intent.action.VIEW -d about:blank -p com.android.chrome", commands);
            Assert.Contains("-s emulator-5554 shell am instrument -w -e class com.woong.monitorstack.usage.AppSwitchQaEvidenceTest#collectUsageStatsAfterChromeReturnPersistsFocusSessionAndOutbox", commands);
            Assert.Contains("-s emulator-5554 shell am instrument -w -e class com.woong.monitorstack.usage.AppSwitchQaEvidenceTest#dashboardAfterChromeReturnShowsWoongAsCurrentAndChromeAsLatestExternal", commands);
            Assert.Contains("-s emulator-5554 pull /sdcard/Android/data/com.woong.monitorstack/files/app-switch-qa/dashboard-current-focus-evidence.json", commands);
            Assert.Contains("-s emulator-5554 pull /sdcard/Android/data/com.woong.monitorstack/files/app-switch-qa/dashboard-after-app-switch.png", commands);
            Assert.Contains("-s emulator-5554 logcat -d -b crash", commands);
            Assert.DoesNotContain("-s emulator-5556", commands);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static string FakeAdbScript(string adbLog, bool serialAware)
    {
        string pullTarget = serialAware ? "%5" : "%3";
        string pullRemoteFileName = serialAware ? "%~nx4" : "%~nx2";
        string shellPrefix = serialAware ? "%3" : "%1";
        string logcatPrefix = serialAware ? "%3" : "%1";

        return $$"""
@echo off
echo %*>>"{{adbLog}}"
if "%1"=="devices" (
  echo List of devices attached
  echo emulator-5554 device product:test model:FakeDevice
  echo emulator-5556 device product:test model:OtherFakeDevice
  exit /b 0
)
if "{{shellPrefix}}"=="shell" (
  echo     topResumedActivity=ActivityRecord{stale u0 com.android.chrome/org.chromium.chrome.browser.ChromeTabbedActivity t1}
  echo     topResumedActivity=ActivityRecord{fake u0 com.woong.monitorstack/.MainActivity t2}
  echo   mCurrentFocus=Window{fake u0 com.woong.monitorstack/com.woong.monitorstack.MainActivity}
  exit /b 0
)
if "{{logcatPrefix}}"=="logcat" (
  echo fake logcat for %*
  exit /b 0
)
if "%1"=="pull" (
  if "{{pullRemoteFileName}}"=="room-assertions.json" (
    echo {{PassingRoomAssertionsJson(4)}}>"{{pullTarget}}"
    exit /b 0
  )
  if "{{pullRemoteFileName}}"=="dashboard-current-focus-evidence.json" (
    echo {{PassingDashboardCurrentFocusJson(4)}}>"{{pullTarget}}"
    exit /b 0
  )
  if "{{pullRemoteFileName}}"=="dashboard-location-map-evidence.json" (
    echo {{PassingLocationMapEvidenceJson()}}>"{{pullTarget}}"
    exit /b 0
  )
  if "{{pullRemoteFileName}}"=="dashboard-location-map.png" (
    echo fake location map screenshot>"{{pullTarget}}"
    exit /b 0
  )
  if /I "{{pullRemoteFileName}}"=="dashboard-current-focus-after-chrome-return.xml" (
    >"{{pullTarget}}" echo ^<hierarchy^>
    >>"{{pullTarget}}" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"{{pullTarget}}" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" text="Dashboard" bounds="[0,2210][200,2240]" /^>
    >>"{{pullTarget}}" echo ^</node^>
    >>"{{pullTarget}}" echo ^</hierarchy^>
    exit /b 0
  )
  if /I "{{pullRemoteFileName}}"=="dashboard-after-app-switch.xml" (
    >"{{pullTarget}}" echo ^<hierarchy^>
    >>"{{pullTarget}}" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"{{pullTarget}}" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" bounds="[0,2210][200,2240]" /^>
    >>"{{pullTarget}}" echo ^</node^>
    >>"{{pullTarget}}" echo ^</hierarchy^>
    exit /b 0
  )
  if /I "{{pullRemoteFileName}}"=="sessions-after-app-switch.xml" (
    >"{{pullTarget}}" echo ^<hierarchy^>
    >>"{{pullTarget}}" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"{{pullTarget}}" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" bounds="[0,2210][200,2240]" /^>
    >>"{{pullTarget}}" echo ^</node^>
    >>"{{pullTarget}}" echo ^</hierarchy^>
    exit /b 0
  )
  echo fake artifact>"{{pullTarget}}"
  exit /b 0
)
if "%3"=="pull" (
  if "{{pullRemoteFileName}}"=="room-assertions.json" (
    echo {{PassingRoomAssertionsJson(4)}}>"{{pullTarget}}"
    exit /b 0
  )
  if "{{pullRemoteFileName}}"=="dashboard-current-focus-evidence.json" (
    echo {{PassingDashboardCurrentFocusJson(4)}}>"{{pullTarget}}"
    exit /b 0
  )
  if "{{pullRemoteFileName}}"=="dashboard-location-map-evidence.json" (
    echo {{PassingLocationMapEvidenceJson()}}>"{{pullTarget}}"
    exit /b 0
  )
  if "{{pullRemoteFileName}}"=="dashboard-location-map.png" (
    echo fake location map screenshot>"{{pullTarget}}"
    exit /b 0
  )
  if /I "{{pullRemoteFileName}}"=="dashboard-current-focus-after-chrome-return.xml" (
    >"{{pullTarget}}" echo ^<hierarchy^>
    >>"{{pullTarget}}" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"{{pullTarget}}" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" text="Dashboard" bounds="[0,2210][200,2240]" /^>
    >>"{{pullTarget}}" echo ^</node^>
    >>"{{pullTarget}}" echo ^</hierarchy^>
    exit /b 0
  )
  if /I "{{pullRemoteFileName}}"=="dashboard-after-app-switch.xml" (
    >"{{pullTarget}}" echo ^<hierarchy^>
    >>"{{pullTarget}}" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"{{pullTarget}}" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" bounds="[0,2210][200,2240]" /^>
    >>"{{pullTarget}}" echo ^</node^>
    >>"{{pullTarget}}" echo ^</hierarchy^>
    exit /b 0
  )
  if /I "{{pullRemoteFileName}}"=="sessions-after-app-switch.xml" (
    >"{{pullTarget}}" echo ^<hierarchy^>
    >>"{{pullTarget}}" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"{{pullTarget}}" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" bounds="[0,2210][200,2240]" /^>
    >>"{{pullTarget}}" echo ^</node^>
    >>"{{pullTarget}}" echo ^</hierarchy^>
    exit /b 0
  )
  echo fake artifact>"{{pullTarget}}"
  exit /b 0
)
exit /b 0
""";
    }

    private static string FakeAdbScriptWithMissingWoongPidAfterEvidence(string adbLog)
    {
        return $$"""
@echo off
echo %*>>"{{adbLog}}"
echo %* | findstr /C:"shell pidof com.woong.monitorstack" >nul
if not errorlevel 1 (
  echo fake pidof failure 1>&2
  exit /b 1
)
echo %* | findstr /C:"logcat -d -v time | grep -F 'com.woong.monitorstack' || true" >nul
if not errorlevel 1 (
  echo fake fallback logcat for com.woong.monitorstack
  exit /b 0
)
if "%1"=="devices" (
  echo List of devices attached
  echo emulator-5554 device product:test model:FakeDevice
  exit /b 0
)
if "%3"=="shell" (
  if "%4"=="pm" (
    echo package:com.woong.monitorstack
    exit /b 0
  )
  echo     topResumedActivity=ActivityRecord{stale u0 com.android.chrome/org.chromium.chrome.browser.ChromeTabbedActivity t1}
  echo     topResumedActivity=ActivityRecord{fake u0 com.woong.monitorstack/.MainActivity t2}
  echo   mCurrentFocus=Window{fake u0 com.woong.monitorstack/com.woong.monitorstack.MainActivity}
  exit /b 0
)
if "%3"=="logcat" (
  echo fake crash logcat for %*
  exit /b 0
)
if "%3"=="pull" (
  if "%~nx5"=="room-assertions.json" (
    echo {{PassingRoomAssertionsJson(9)}}>"%5"
  ) else if "%~nx5"=="dashboard-current-focus-evidence.json" (
    echo {{PassingDashboardCurrentFocusJson(9)}}>"%5"
  ) else if "%~nx5"=="dashboard-location-map-evidence.json" (
    echo {{PassingLocationMapEvidenceJson()}}>"%5"
  ) else if "%~nx5"=="dashboard-location-map.png" (
    echo fake location map screenshot>"%5"
  ) else if /I "%~nx5"=="dashboard-current-focus-after-chrome-return.xml" (
    >"%5" echo ^<hierarchy^>
    >>"%5" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"%5" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" text="Dashboard" bounds="[0,2210][200,2240]" /^>
    >>"%5" echo ^</node^>
    >>"%5" echo ^</hierarchy^>
  ) else if /I "%~nx5"=="dashboard-after-app-switch.xml" (
    >"%5" echo ^<hierarchy^>
    >>"%5" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"%5" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" bounds="[0,2210][200,2240]" /^>
    >>"%5" echo ^</node^>
    >>"%5" echo ^</hierarchy^>
  ) else if /I "%~nx5"=="sessions-after-app-switch.xml" (
    >"%5" echo ^<hierarchy^>
    >>"%5" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"%5" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" bounds="[0,2210][200,2240]" /^>
    >>"%5" echo ^</node^>
    >>"%5" echo ^</hierarchy^>
  ) else (
    echo fake artifact>"%5"
  )
  exit /b 0
)
exit /b 0
""";
    }

    private static string FakeAdbScriptWithFailingRoomAssertions(string adbLog)
    {
        return $$"""
@echo off
echo %*>>"{{adbLog}}"
if "%1"=="devices" (
  echo List of devices attached
  echo emulator-5554 device product:test model:FakeDevice
  exit /b 0
)
if "%1"=="shell" (
  echo     topResumedActivity=ActivityRecord{stale u0 com.android.chrome/org.chromium.chrome.browser.ChromeTabbedActivity t1}
  echo     topResumedActivity=ActivityRecord{fake u0 com.woong.monitorstack/.MainActivity t2}
  echo   mCurrentFocus=Window{fake u0 com.woong.monitorstack/com.woong.monitorstack.MainActivity}
  exit /b 0
)
if "%1"=="pull" (
  if "%~nx3"=="room-assertions.json" (
    echo {"status":"FAIL","privacy":"No Chrome screenshots, no Chrome UI hierarchy, no typed text, no form contents, no browser/page contents.","chromePackageName":"com.android.chrome","focusSessionChromeRows":1,"syncOutboxChromeRows":0}>"%3"
  ) else (
    echo screenshot acceptance should not run after room assertions fail 1>&2
    exit /b 44
  )
  exit /b 0
)
if "%1"=="logcat" (
  echo fake logcat for %*
  exit /b 0
)
exit /b 0
""";
    }

    private static string FakeAdbScriptWithBlankDashboardScreenshotOnce(string adbLog, string blankPullMarker)
    {
        return $$"""
@echo off
echo %*>>"{{adbLog}}"
if "%1"=="devices" (
  echo List of devices attached
  echo emulator-5554 device product:test model:FakeDevice
  exit /b 0
)
if "%1"=="shell" (
  echo     topResumedActivity=ActivityRecord{stale u0 com.android.chrome/org.chromium.chrome.browser.ChromeTabbedActivity t1}
  echo     topResumedActivity=ActivityRecord{fake u0 com.woong.monitorstack/.MainActivity t2}
  echo   mCurrentFocus=Window{fake u0 com.woong.monitorstack/com.woong.monitorstack.MainActivity}
  exit /b 0
)
if "%1"=="logcat" (
  echo fake logcat for %*
  exit /b 0
)
if "%1"=="pull" (
  if "%~nx3"=="dashboard-after-app-switch.png" (
    if not exist "{{blankPullMarker}}" (
      >"{{blankPullMarker}}" echo 1
      break > "%3"
      exit /b 0
    )
  )
  if "%~nx3"=="room-assertions.json" (
    echo {{PassingRoomAssertionsJson(4)}}>"%3"
  ) else if "%~nx3"=="dashboard-current-focus-evidence.json" (
    echo {{PassingDashboardCurrentFocusJson(4)}}>"%3"
  ) else if "%~nx3"=="dashboard-location-map-evidence.json" (
    echo {{PassingLocationMapEvidenceJson()}}>"%3"
  ) else if "%~nx3"=="dashboard-location-map.png" (
    echo fake location map screenshot>"%3"
  ) else if /I "%~nx3"=="dashboard-current-focus-after-chrome-return.xml" (
    >"%3" echo ^<hierarchy^>
    >>"%3" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"%3" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" text="Dashboard" bounds="[0,2210][200,2240]" /^>
    >>"%3" echo ^</node^>
    >>"%3" echo ^</hierarchy^>
  ) else if /I "%~nx3"=="dashboard-after-app-switch.xml" (
    >"%3" echo ^<hierarchy^>
    >>"%3" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"%3" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" bounds="[0,2210][200,2240]" /^>
    >>"%3" echo ^</node^>
    >>"%3" echo ^</hierarchy^>
  ) else if /I "%~nx3"=="sessions-after-app-switch.xml" (
    >"%3" echo ^<hierarchy^>
    >>"%3" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"%3" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" bounds="[0,2210][200,2240]" /^>
    >>"%3" echo ^</node^>
    >>"%3" echo ^</hierarchy^>
  ) else (
    echo fake artifact>"%3"
  )
  exit /b 0
)
exit /b 0
""";
    }

    private static string FakeAdbScriptWithPerceptuallyBlankDashboardScreenshotOnce(string adbLog, string blankPullMarker)
    {
        return $$"""
@echo off
echo %*>>"{{adbLog}}"
if "%1"=="devices" (
  echo List of devices attached
  echo emulator-5554 device product:test model:FakeDevice
  exit /b 0
)
if "%1"=="shell" (
  echo     topResumedActivity=ActivityRecord{stale u0 com.android.chrome/org.chromium.chrome.browser.ChromeTabbedActivity t1}
  echo     topResumedActivity=ActivityRecord{fake u0 com.woong.monitorstack/.MainActivity t2}
  echo   mCurrentFocus=Window{fake u0 com.woong.monitorstack/com.woong.monitorstack.MainActivity}
  exit /b 0
)
if "%1"=="logcat" (
  echo fake logcat for %*
  exit /b 0
)
if "%1"=="pull" (
  if "%~nx3"=="dashboard-after-app-switch.png" (
    if not exist "{{blankPullMarker}}" (
      >"{{blankPullMarker}}" echo 1
      powershell.exe -NoProfile -Command "Add-Type -AssemblyName System.Drawing; $bmp = [System.Drawing.Bitmap]::new(12, 12); $g = [System.Drawing.Graphics]::FromImage($bmp); $g.Clear([System.Drawing.Color]::White); $g.Dispose(); $bmp.Save('%3', [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()"
      exit /b 0
    )
    powershell.exe -NoProfile -Command "Add-Type -AssemblyName System.Drawing; $bmp = [System.Drawing.Bitmap]::new(12, 12); for ($x = 0; $x -lt 12; $x++) { for ($y = 0; $y -lt 12; $y++) { $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb((20 * $x) %% 255, (30 * $y) %% 255, 120)) } }; $bmp.Save('%3', [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()"
    exit /b 0
  )
  if "%~nx3"=="room-assertions.json" (
    echo {{PassingRoomAssertionsJson(4)}}>"%3"
  ) else if "%~nx3"=="dashboard-current-focus-evidence.json" (
    echo {{PassingDashboardCurrentFocusJson(4)}}>"%3"
  ) else if "%~nx3"=="dashboard-location-map-evidence.json" (
    echo {{PassingLocationMapEvidenceJson()}}>"%3"
  ) else if "%~nx3"=="dashboard-location-map.png" (
    echo fake location map screenshot>"%3"
  ) else if /I "%~nx3"=="dashboard-current-focus-after-chrome-return.xml" (
    >"%3" echo ^<hierarchy^>
    >>"%3" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"%3" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" text="Dashboard" bounds="[0,2210][200,2240]" /^>
    >>"%3" echo ^</node^>
    >>"%3" echo ^</hierarchy^>
  ) else if /I "%~nx3"=="dashboard-after-app-switch.xml" (
    >"%3" echo ^<hierarchy^>
    >>"%3" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"%3" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" bounds="[0,2210][200,2240]" /^>
    >>"%3" echo ^</node^>
    >>"%3" echo ^</hierarchy^>
  ) else if /I "%~nx3"=="sessions-after-app-switch.xml" (
    >"%3" echo ^<hierarchy^>
    >>"%3" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"%3" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" bounds="[0,2210][200,2240]" /^>
    >>"%3" echo ^</node^>
    >>"%3" echo ^</hierarchy^>
  ) else (
    echo fake artifact>"%3"
  )
  exit /b 0
)
exit /b 0
""";
    }

    private static string FakeAdbScriptWithWoongPidChangeAfterChromeReturn(
        string adbLog,
        string processMetadataCount,
        bool androidRuntimeCrash)
    {
        string crashLine = androidRuntimeCrash
            ? "AndroidRuntime FATAL EXCEPTION Process: com.woong.monitorstack"
            : "fake crash buffer without product crash";

        return $$"""
@echo off
setlocal EnableDelayedExpansion
echo %*>>"{{adbLog}}"
if "%1"=="devices" (
  echo List of devices attached
  echo emulator-5554 device product:test model:FakeDevice
  exit /b 0
)
echo %* | findstr /C:"echo Woong pid:" >nul
if not errorlevel 1 (
  if not exist "{{processMetadataCount}}" >"{{processMetadataCount}}" echo 0
  set /p count=<"{{processMetadataCount}}"
  if "!count!"=="0" (
    >"{{processMetadataCount}}" echo 1
    echo Woong pid:
    echo 111
    echo Chrome pid:
    echo Matching processes:
    echo u0_a1 111 1 com.woong.monitorstack
    exit /b 0
  )
  if "!count!"=="1" (
    >"{{processMetadataCount}}" echo 2
    echo Woong pid:
    echo 111
    echo Chrome pid:
    echo 222
    echo Matching processes:
    echo u0_a1 111 1 com.woong.monitorstack
    echo u0_a2 222 1 com.android.chrome
    exit /b 0
  )
  echo Woong pid:
  echo 333
  echo Chrome pid:
  echo Matching processes:
  echo u0_a1 333 1 com.woong.monitorstack
  exit /b 0
)
if "%1"=="shell" (
  echo     topResumedActivity=ActivityRecord{fake u0 com.woong.monitorstack/.MainActivity t2}
  echo   mCurrentFocus=Window{fake u0 com.woong.monitorstack/com.woong.monitorstack.MainActivity}
  exit /b 0
)
if "%1"=="logcat" (
  echo {{crashLine}}
  exit /b 0
)
if "%1"=="pull" (
  echo pull should not run after process-death classification 1>&2
  exit /b 44
)
exit /b 0
""";
    }

    private static (string debugApk, string androidTestApk) CreateFakeApks(string tempRoot, bool sameContent)
    {
        string debugApk = Path.Combine(tempRoot, "app", "build", "outputs", "apk", "debug", "app-debug.apk");
        string androidTestApk = Path.Combine(tempRoot, "app", "build", "outputs", "apk", "androidTest", "debug", "app-debug-androidTest.apk");
        Directory.CreateDirectory(Path.GetDirectoryName(debugApk)!);
        Directory.CreateDirectory(Path.GetDirectoryName(androidTestApk)!);
        File.WriteAllText(debugApk, "fake-debug-apk");
        File.WriteAllText(androidTestApk, sameContent ? "fake-debug-apk" : "fake-android-test-apk");
        return (debugApk, androidTestApk);
    }

    private static string PassingRoomAssertionsJson(int rowCount) =>
        $$"""{"status":"PASS","focusSessionChromeRows":{{rowCount}},"syncOutboxChromeRows":{{rowCount}},"currentAppStateRows":1,"latestCurrentAppStatePackageName":"com.woong.monitorstack","hasLatestWoongCurrentAppState":true,"pendingCurrentAppStateOutboxRows":1,"hasPendingWoongCurrentAppStateOutbox":true,"priorExternalChromeMetadataOnly":true,"currentAppStateOutboxMetadataOnly":true}""";

    private static string PassingDashboardCurrentFocusJson(int chromeRows) =>
        $$"""{"status":"PASS","actualCurrentPackageText":"com.woong.monitorstack","actualLatestExternalPackageText":"com.android.chrome","roomFocusSessionMonitorRows":2,"roomFocusSessionChromeRows":{{chromeRows}},"latestCurrentAppStatePackageName":"com.woong.monitorstack","hasLatestWoongCurrentAppState":true,"pendingCurrentAppStateOutboxRows":1,"hasPendingWoongCurrentAppStateOutbox":true,"currentAppStateOutboxMetadataOnly":true}""";

    private static string PassingLocationMapEvidenceJson() =>
        """{"status":"PASS","visitCount":1,"topVisitLocationKey":"37.5665,126.9780","topVisitDurationMs":600000,"topVisitSampleCount":2,"metadataOnly":true,"mapScreenshot":"dashboard-location-map.png"}""";

    private static string FakeAdbScriptWithInstallTimeout(string adbLog)
    {
        return $$"""
@echo off
echo %*>>"{{adbLog}}"
if "%1"=="devices" (
  echo List of devices attached
  echo emulator-5554 device product:test model:FakeDevice
  exit /b 0
)
if "%3"=="shell" (
  exit /b 0
)
if "%3"=="install" (
  echo fake install stdout before timeout
  echo fake install stderr before timeout 1>&2
  ping -n 6 127.0.0.1 >nul
  exit /b 0
)
exit /b 0
""";
    }

    private static string FakeAdbScriptWithPackageManagerTimeout(string adbLog)
    {
        return $$"""
@echo off
echo %*>>"{{adbLog}}"
if "%1"=="devices" (
  echo List of devices attached
  echo emulator-5554 device product:test model:FakeDevice
  exit /b 0
)
if "%3"=="shell" (
  if "%4"=="pm" (
    echo package manager did not answer before timeout
    ping -n 6 127.0.0.1 >nul
    exit /b 0
  )
  exit /b 0
)
if "%3"=="install" (
  echo install should not run when package manager preflight is blocked 1>&2
  exit /b 44
)
exit /b 0
""";
    }

    private static string FakeAdbScriptWithCurrentApks(string adbLog, string hashCheckCountPath, string currentHash)
    {
        return $$"""
@echo off
setlocal EnableDelayedExpansion
echo %*>>"{{adbLog}}"
if "%1"=="devices" (
  echo List of devices attached
  echo emulator-5554 device product:test model:FakeDevice
  exit /b 0
)
if "%3"=="shell" (
  if "%4"=="pm" (
    echo package:com.woong.monitorstack
    exit /b 0
  )
  if not exist "{{hashCheckCountPath}}" >"{{hashCheckCountPath}}" echo 0
  set /p count=<"{{hashCheckCountPath}}"
  if "!count!"=="0" (
    >"{{hashCheckCountPath}}" echo 1
    echo {{currentHash}}
    exit /b 0
  )
  if "!count!"=="1" (
    >"{{hashCheckCountPath}}" echo 2
    echo {{currentHash}}
    exit /b 0
  )
  echo     topResumedActivity=ActivityRecord{stale u0 com.android.chrome/org.chromium.chrome.browser.ChromeTabbedActivity t1}
  echo     topResumedActivity=ActivityRecord{fake u0 com.woong.monitorstack/.MainActivity t2}
  echo   mCurrentFocus=Window{fake u0 com.woong.monitorstack/com.woong.monitorstack.MainActivity}
  exit /b 0
)
if "%3"=="pull" (
  if "%~nx4"=="room-assertions.json" (
    echo {"status":"PASS","focusSessionChromeRows":4,"syncOutboxChromeRows":4,"currentAppStateRows":1,"latestCurrentAppStatePackageName":"com.woong.monitorstack","hasLatestWoongCurrentAppState":true,"pendingCurrentAppStateOutboxRows":1,"hasPendingWoongCurrentAppStateOutbox":true,"priorExternalChromeMetadataOnly":true,"currentAppStateOutboxMetadataOnly":true}>"%5"
  ) else if "%~nx4"=="dashboard-current-focus-evidence.json" (
    echo {"status":"PASS","actualCurrentPackageText":"com.woong.monitorstack","actualLatestExternalPackageText":"com.android.chrome","roomFocusSessionMonitorRows":2,"roomFocusSessionChromeRows":4,"latestCurrentAppStatePackageName":"com.woong.monitorstack","hasLatestWoongCurrentAppState":true,"pendingCurrentAppStateOutboxRows":1,"hasPendingWoongCurrentAppStateOutbox":true,"currentAppStateOutboxMetadataOnly":true}>"%5"
  ) else if "%~nx4"=="dashboard-location-map-evidence.json" (
    echo {"status":"PASS","visitCount":1,"topVisitLocationKey":"37.5665,126.9780","topVisitDurationMs":600000,"topVisitSampleCount":2,"metadataOnly":true,"mapScreenshot":"dashboard-location-map.png"}>"%5"
  ) else if "%~nx4"=="dashboard-location-map.png" (
    echo fake location map screenshot>"%5"
  ) else if /I "%~nx4"=="dashboard-current-focus-after-chrome-return.xml" (
    >"%5" echo ^<hierarchy^>
    >>"%5" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"%5" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" text="Dashboard" bounds="[0,2210][200,2240]" /^>
    >>"%5" echo ^</node^>
    >>"%5" echo ^</hierarchy^>
  ) else if /I "%~nx4"=="dashboard-after-app-switch.xml" (
    >"%5" echo ^<hierarchy^>
    >>"%5" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"%5" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" bounds="[0,2210][200,2240]" /^>
    >>"%5" echo ^</node^>
    >>"%5" echo ^</hierarchy^>
  ) else if /I "%~nx4"=="sessions-after-app-switch.xml" (
    >"%5" echo ^<hierarchy^>
    >>"%5" echo ^<node resource-id="com.woong.monitorstack:id/bottomNavigation" bounds="[0,2200][1080,2280]"^>
    >>"%5" echo ^<node resource-id="com.woong.monitorstack:id/navigation_bar_item_large_label_view" bounds="[0,2210][200,2240]" /^>
    >>"%5" echo ^</node^>
    >>"%5" echo ^</hierarchy^>
  ) else (
    echo fake artifact>"%5"
  )
  exit /b 0
)
if "%3"=="logcat" (
  echo fake logcat for %*
  exit /b 0
)
if "%3"=="install" (
  echo install should have been skipped 1>&2
  exit /b 44
)
exit /b 0
""";
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
