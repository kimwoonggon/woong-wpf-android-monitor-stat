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
            Assert.Contains("shell am instrument -w -e class com.woong.monitorstack.usage.AppSwitchQaEvidenceTest#captureWoongDashboardAndSessionsOnlyAfterReturn", commands);
            Assert.Contains("pull /sdcard/Android/data/com.woong.monitorstack/files/app-switch-qa/room-assertions.json", commands);
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
  echo fake artifact>"{{pullTarget}}"
  exit /b 0
)
if "%3"=="pull" (
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
    echo {"status":"PASS","focusSessionChromeRows":9,"syncOutboxChromeRows":9}>"%5"
  ) else (
    echo fake artifact>"%5"
  )
  exit /b 0
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
  echo fake artifact>"%5"
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
