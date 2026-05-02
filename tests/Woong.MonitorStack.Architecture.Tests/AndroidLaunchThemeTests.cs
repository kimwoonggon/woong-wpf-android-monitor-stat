namespace Woong.MonitorStack.Architecture.Tests;

public sealed class AndroidLaunchThemeTests
{
    [Fact]
    public void AndroidMainActivity_UsesDedicatedStartingThemeForBrandedFirstLaunch()
    {
        string repoRoot = FindRepositoryRoot();
        string manifestPath = Path.Combine(
            repoRoot,
            "android",
            "app",
            "src",
            "main",
            "AndroidManifest.xml");
        string valuesStylesPath = Path.Combine(
            repoRoot,
            "android",
            "app",
            "src",
            "main",
            "res",
            "values",
            "styles.xml");
        string values31StylesPath = Path.Combine(
            repoRoot,
            "android",
            "app",
            "src",
            "main",
            "res",
            "values-v31",
            "styles.xml");
        string mainActivityPath = Path.Combine(
            repoRoot,
            "android",
            "app",
            "src",
            "main",
            "java",
            "com",
            "woong",
            "monitorstack",
            "MainActivity.kt");

        string manifest = File.ReadAllText(manifestPath);
        string valuesStyles = File.ReadAllText(valuesStylesPath);
        string values31Styles = File.ReadAllText(values31StylesPath);
        string mainActivity = File.ReadAllText(mainActivityPath);

        Assert.Contains("android:name=\".MainActivity\"", manifest, StringComparison.Ordinal);
        Assert.Contains(
            "android:theme=\"@style/Theme.WoongMonitor.Starting\"",
            manifest,
            StringComparison.Ordinal);
        Assert.Contains("name=\"Theme.WoongMonitor.Starting\"", valuesStyles);
        Assert.Contains("name=\"Theme.WoongMonitor.Starting\"", values31Styles);
        Assert.Contains("android:windowBackground", valuesStyles);
        Assert.Contains("android:windowSplashScreenBackground", values31Styles);
        Assert.Contains("setTheme(R.style.Theme_WoongMonitor)", mainActivity);
        Assert.DoesNotContain("android:postSplashScreenTheme", values31Styles);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Woong.MonitorStack.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
