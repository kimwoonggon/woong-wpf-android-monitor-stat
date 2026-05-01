namespace Woong.MonitorStack.Architecture.Tests;

public sealed class AndroidReadmeTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Readme_DocumentsAndroidEmulatorBuildInstallLaunchAndScreenshotFlow()
    {
        string readme = File.ReadAllText(Path.Combine(RepositoryRoot, "README.md"));

        Assert.Contains("& \"$env:ANDROID_HOME\\emulator\\emulator.exe\" -list-avds", readme, StringComparison.Ordinal);
        Assert.Contains("& \"$env:ANDROID_HOME\\cmdline-tools\\latest\\bin\\sdkmanager.bat\"", readme, StringComparison.Ordinal);
        Assert.Contains("\"system-images;android-36;google_apis;x86_64\"", readme, StringComparison.Ordinal);
        Assert.Contains("& \"$env:ANDROID_HOME\\cmdline-tools\\latest\\bin\\avdmanager.bat\" create avd -n Medium_Phone", readme, StringComparison.Ordinal);
        Assert.Contains("& \"$env:ANDROID_HOME\\emulator\\emulator.exe\" -avd Medium_Phone", readme, StringComparison.Ordinal);
        Assert.Contains("& \"$env:ANDROID_HOME\\platform-tools\\adb.exe\" wait-for-device", readme, StringComparison.Ordinal);
        Assert.Contains("& \"$env:ANDROID_HOME\\platform-tools\\adb.exe\" shell getprop sys.boot_completed", readme, StringComparison.Ordinal);
        Assert.Contains(".\\gradlew.bat testDebugUnitTest --no-daemon --stacktrace", readme, StringComparison.Ordinal);
        Assert.Contains(".\\gradlew.bat assembleDebug --no-daemon --stacktrace", readme, StringComparison.Ordinal);
        Assert.Contains("& \"$env:ANDROID_HOME\\platform-tools\\adb.exe\" install -r app\\build\\outputs\\apk\\debug\\app-debug.apk", readme, StringComparison.Ordinal);
        Assert.Contains("& \"$env:ANDROID_HOME\\platform-tools\\adb.exe\" shell monkey -p com.woong.monitorstack -c android.intent.category.LAUNCHER 1", readme, StringComparison.Ordinal);
        Assert.Contains("& \"$env:ANDROID_HOME\\platform-tools\\adb.exe\" shell screencap -p /sdcard/woong-dashboard.png", readme, StringComparison.Ordinal);
        Assert.Contains("& \"$env:ANDROID_HOME\\platform-tools\\adb.exe\" pull /sdcard/woong-dashboard.png artifacts\\android-check\\manual\\dashboard.png", readme, StringComparison.Ordinal);
        Assert.Contains("Raw `exec-out screencap -p >", readme, StringComparison.Ordinal);
        Assert.Contains("scripts\\run-android-ui-snapshots.ps1", readme, StringComparison.Ordinal);
        Assert.Contains("scripts\\run-android-ui-snapshots.ps1 -DeviceSerial emulator-5554", readme, StringComparison.Ordinal);
        Assert.Contains("Dashboard, Sessions, App Detail, Report, Report custom range, Settings", readme, StringComparison.Ordinal);
        Assert.Contains("scripts\\run-android-usage-current-focus-validation.ps1 -DeviceSerial emulator-5554", readme, StringComparison.Ordinal);
        Assert.Contains("It does not screenshot Chrome or inspect Chrome page contents.", readme, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Woong.MonitorStack.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not find repository root.");
    }
}
