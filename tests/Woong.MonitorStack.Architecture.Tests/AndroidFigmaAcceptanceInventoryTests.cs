namespace Woong.MonitorStack.Architecture.Tests;

public sealed class AndroidFigmaAcceptanceInventoryTests
{
    [Fact]
    public void AndroidFigmaAcceptanceInventory_MapsAllSevenScreensToCodeTestsAndScreenshots()
    {
        string repoRoot = FindRepositoryRoot();
        string inventoryPath = Path.Combine(repoRoot, "docs", "android-figma-7-screen-acceptance.md");

        Assert.True(File.Exists(inventoryPath), "Android Figma acceptance inventory must exist.");
        string inventory = File.ReadAllText(inventoryPath);

        string[] expectedRows =
        [
            "| Splash | `fragment_splash.xml` | `SplashFragment` | `MainActivityTest.launcherShowsSplashBeforeRoutingToDashboard` | `figma-01-splash.png` |",
            "| Permission | `fragment_permission_onboarding.xml` | `PermissionOnboardingFragment` | `MainActivityTest.whenUsageAccessMissingShowsPermissionOnboarding` | `figma-02-permission.png` |",
            "| Dashboard | `fragment_dashboard.xml` | `DashboardFragment` | `MainActivityTest.dashboardRollingPeriodButtonsReloadRoomBackedSummary` | `figma-03-dashboard.png` |",
            "| Sessions | `fragment_sessions.xml` | `SessionsFragment` | `MainActivityTest.sessionsPeriodButtonsReflectSelectedRange` | `figma-04-sessions.png` |",
            "| App Detail | `fragment_app_detail.xml` | `AppDetailFragment` | `RoomSessionsRepositoryTest` | `figma-05-app-detail.png` |",
            "| Report | `fragment_report.xml` | `ReportFragment` | `MainActivityTest.reportTabLoadsRoomBackedSevenDaySummary` | `figma-06-report.png` |",
            "| Settings | `fragment_settings.xml` | `SettingsFragment` | `MainActivityTest.settingsTabShowsRuntimePrivacySyncAndLocationControls` | `figma-07-settings.png` |"
        ];

        foreach (string expectedRow in expectedRows)
        {
            Assert.Contains(expectedRow, inventory, StringComparison.Ordinal);
        }

        Assert.Contains("UsageStatsManager metadata only", inventory, StringComparison.Ordinal);
        Assert.Contains("no typed text, passwords, form contents, clipboard contents, browser/page contents, other-app screenshots, or global touch coordinates", inventory, StringComparison.Ordinal);
        Assert.Contains("Legacy Activity cleanup", inventory, StringComparison.Ordinal);
        Assert.Contains("Chrome/app-switch QA", inventory, StringComparison.Ordinal);
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
