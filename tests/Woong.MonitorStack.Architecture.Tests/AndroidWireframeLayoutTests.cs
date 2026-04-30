namespace Woong.MonitorStack.Architecture.Tests;

public sealed class AndroidWireframeLayoutTests
{
    [Fact]
    public void AndroidDashboardLayout_UsesWireframeCardAndNavigationStructure()
    {
        string repoRoot = FindRepositoryRoot();
        string dashboard = ReadAndroidLayout(repoRoot, "activity_dashboard.xml");

        Assert.Contains("androidx.core.widget.NestedScrollView", dashboard);
        Assert.Contains("com.google.android.material.card.MaterialCardView", dashboard);
        Assert.Contains("@+id/statusChipRow", dashboard);
        Assert.Contains("@+id/currentFocusCard", dashboard);
        Assert.Contains("@+id/summaryCardsGrid", dashboard);
        Assert.Contains("@+id/periodFilterRow", dashboard);
        Assert.Contains("@+id/hourlyFocusChartCard", dashboard);
        Assert.Contains("@+id/topAppsCard", dashboard);
        Assert.Contains("@+id/recentSessionsCard", dashboard);
        Assert.Contains("@+id/bottomNavigationRow", dashboard);
        Assert.Contains("@+id/navDashboardText", dashboard);
        Assert.Contains("@+id/navSessionsText", dashboard);
        Assert.Contains("@+id/navReportText", dashboard);
        Assert.Contains("@+id/navSettingsText", dashboard);
    }

    [Fact]
    public void AndroidSettingsLayout_UsesScrollableGroupedSettingsCards()
    {
        string repoRoot = FindRepositoryRoot();
        string settings = ReadAndroidLayout(repoRoot, "activity_settings.xml");

        Assert.Contains("androidx.core.widget.NestedScrollView", settings);
        Assert.Contains("com.google.android.material.card.MaterialCardView", settings);
        Assert.Contains("@+id/permissionsSettingsCard", settings);
        Assert.Contains("@+id/syncSettingsCard", settings);
        Assert.Contains("@+id/privacySettingsCard", settings);
        Assert.Contains("@+id/locationSettingsCard", settings);
        Assert.Contains("@+id/storageSettingsCard", settings);
        Assert.Contains("@+id/locationContextCheckBox", settings);
        Assert.Contains("@+id/preciseLatitudeLongitudeCheckBox", settings);
        Assert.Contains("@+id/requestLocationPermissionButton", settings);
    }

    [Fact]
    public void AndroidSharedResources_DefineWireframeTokens()
    {
        string repoRoot = FindRepositoryRoot();
        string valuesRoot = Path.Combine(repoRoot, "android", "app", "src", "main", "res", "values");
        string drawableRoot = Path.Combine(repoRoot, "android", "app", "src", "main", "res", "drawable");
        string colors = File.ReadAllText(Path.Combine(valuesRoot, "colors.xml"));
        string styles = File.ReadAllText(Path.Combine(valuesRoot, "styles.xml"));

        string[] expectedColors =
        [
            "wms_primary",
            "wms_background",
            "wms_surface",
            "wms_border",
            "wms_text_primary",
            "wms_text_secondary",
            "wms_text_muted",
            "wms_success",
            "wms_warning",
            "wms_info"
        ];

        foreach (string color in expectedColors)
        {
            Assert.Contains($"name=\"{color}\"", colors);
        }

        string[] expectedStyles =
        [
            "WmsCard",
            "WmsScreenTitle",
            "WmsSectionTitle",
            "WmsKeyValueText",
            "WmsStatusChip",
            "WmsStatusChip.Success",
            "WmsStatusChip.Warning",
            "WmsStatusChip.Info",
            "WmsPeriodButton",
            "WmsPeriodButton.Selected"
        ];

        foreach (string style in expectedStyles)
        {
            Assert.Contains($"name=\"{style}\"", styles);
        }

        Assert.True(
            File.Exists(Path.Combine(drawableRoot, "bg_status_chip_neutral.xml")),
            "Android wireframe status chips need a shared rounded background.");
    }

    [Fact]
    public void AndroidPeriodButtonStyle_UsesReadableMixedCaseContentWidth()
    {
        string repoRoot = FindRepositoryRoot();
        string styles = File.ReadAllText(Path.Combine(repoRoot, "android", "app", "src", "main", "res", "values", "styles.xml"));

        Assert.Contains("<item name=\"android:layout_width\">wrap_content</item>", styles);
        Assert.Contains("<item name=\"android:minWidth\">80dp</item>", styles);
        Assert.Contains("<item name=\"android:textAllCaps\">false</item>", styles);
    }

    [Fact]
    public void AndroidSessionsAndDailySummaryLayouts_UseProductCardScreens()
    {
        string repoRoot = FindRepositoryRoot();
        string sessions = ReadAndroidLayout(repoRoot, "activity_sessions.xml");
        string dailySummary = ReadAndroidLayout(repoRoot, "activity_daily_summary.xml");

        Assert.Contains("androidx.core.widget.NestedScrollView", sessions);
        Assert.Contains("com.google.android.material.card.MaterialCardView", sessions);
        Assert.Contains("@+id/sessionsFilterRow", sessions);
        Assert.Contains("@+id/sessionsListCard", sessions);
        Assert.Contains("androidx.core.widget.NestedScrollView", dailySummary);
        Assert.Contains("com.google.android.material.card.MaterialCardView", dailySummary);
        Assert.Contains("@+id/dailySummaryMetricGrid", dailySummary);
        Assert.Contains("@+id/dailySummaryTopCard", dailySummary);
    }

    private static string ReadAndroidLayout(string repoRoot, string fileName)
    {
        return File.ReadAllText(Path.Combine(repoRoot, "android", "app", "src", "main", "res", "layout", fileName));
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
