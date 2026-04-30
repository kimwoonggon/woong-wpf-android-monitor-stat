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

    [Fact]
    public void AndroidFocusSessionRowLayout_SeparatesPackageTimeDurationAndState()
    {
        string repoRoot = FindRepositoryRoot();
        string row = ReadAndroidLayout(repoRoot, "item_focus_session.xml");

        Assert.Contains("@+id/sessionAppIconPlaceholder", row);
        Assert.Contains("@+id/sessionPackageText", row);
        Assert.Contains("@+id/sessionTimeRangeText", row);
        Assert.Contains("@+id/sessionDurationText", row);
        Assert.Contains("@+id/sessionStateText", row);
    }

    [Fact]
    public void AndroidPrimaryActivityLayouts_AvoidSystemBarOverlap()
    {
        string repoRoot = FindRepositoryRoot();
        string[] layouts =
        [
            "activity_dashboard.xml",
            "activity_sessions.xml",
            "activity_settings.xml",
            "activity_daily_summary.xml"
        ];

        foreach (string layout in layouts)
        {
            string xml = ReadAndroidLayout(repoRoot, layout);
            Assert.Contains("android:fitsSystemWindows=\"true\"", xml);
        }
    }

    [Fact]
    public void AndroidSummaryMetricCards_UseMaterialCardContainers()
    {
        string repoRoot = FindRepositoryRoot();
        string dashboard = ReadAndroidLayout(repoRoot, "activity_dashboard.xml");
        string dailySummary = ReadAndroidLayout(repoRoot, "activity_daily_summary.xml");

        string[] dashboardCards =
        [
            "totalActiveCard",
            "screenOnCard",
            "idleCard",
            "webFocusCard"
        ];

        foreach (string card in dashboardCards)
        {
            AssertMaterialCardContainer(dashboard, card);
        }

        string[] dailySummaryCards =
        [
            "dailySummaryActiveCard",
            "dailySummaryIdleCard",
            "dailySummaryWebCard"
        ];

        foreach (string card in dailySummaryCards)
        {
            AssertMaterialCardContainer(dailySummary, card);
        }
    }

    [Fact]
    public void AndroidMainShell_UsesFragmentContainerAndMaterialBottomNavigation()
    {
        string repoRoot = FindRepositoryRoot();
        string main = ReadAndroidLayout(repoRoot, "activity_main.xml");
        string menu = File.ReadAllText(Path.Combine(repoRoot, "android", "app", "src", "main", "res", "menu", "menu_bottom_navigation.xml"));

        Assert.Contains("androidx.coordinatorlayout.widget.CoordinatorLayout", main);
        Assert.Contains("com.google.android.material.appbar.MaterialToolbar", main);
        Assert.Contains("@+id/topAppBar", main);
        Assert.Contains("androidx.fragment.app.FragmentContainerView", main);
        Assert.Contains("@+id/mainFragmentContainer", main);
        Assert.Contains("com.google.android.material.bottomnavigation.BottomNavigationView", main);
        Assert.Contains("@+id/bottomNavigation", main);
        Assert.Contains("@menu/menu_bottom_navigation", main);

        Assert.Contains("@+id/navDashboard", menu);
        Assert.Contains("@+id/navSessions", menu);
        Assert.Contains("@+id/navReport", menu);
        Assert.Contains("@+id/navSettings", menu);
    }

    [Fact]
    public void AndroidFragmentWireframeLayouts_ExistForProductFlow()
    {
        string repoRoot = FindRepositoryRoot();
        string[] requiredLayouts =
        [
            "fragment_splash.xml",
            "fragment_permission_onboarding.xml",
            "fragment_dashboard.xml",
            "fragment_sessions.xml",
            "fragment_app_detail.xml",
            "fragment_report.xml",
            "fragment_settings.xml",
            "item_summary_card.xml",
            "item_app_usage.xml",
            "item_focus_session.xml",
            "item_settings_group.xml"
        ];

        foreach (string layout in requiredLayouts)
        {
            string path = Path.Combine(repoRoot, "android", "app", "src", "main", "res", "layout", layout);
            Assert.True(File.Exists(path), $"Expected Android XML wireframe layout to exist: {layout}");
        }

        string permission = ReadAndroidLayout(repoRoot, "fragment_permission_onboarding.xml");
        Assert.Contains("@+id/openUsageAccessSettingsButton", permission);
        Assert.Contains("@+id/collectsCard", permission);
        Assert.Contains("@+id/doesNotCollectCard", permission);

        string dashboard = ReadAndroidLayout(repoRoot, "fragment_dashboard.xml");
        Assert.Contains("@+id/currentFocusCard", dashboard);
        Assert.Contains("@+id/summaryCardsGrid", dashboard);
        Assert.Contains("@+id/topAppsRecyclerView", dashboard);
        Assert.Contains("@+id/recentSessionsRecyclerView", dashboard);

        string settings = ReadAndroidLayout(repoRoot, "fragment_settings.xml");
        Assert.Contains("@+id/permissionsSettingsCard", settings);
        Assert.Contains("@+id/locationSettingsCard", settings);
        Assert.Contains("@+id/privacySettingsCard", settings);
    }

    [Fact]
    public void AndroidMainShell_UsesCompactReadableToolbarTitle()
    {
        string repoRoot = FindRepositoryRoot();
        string main = ReadAndroidLayout(repoRoot, "activity_main.xml");
        string styles = File.ReadAllText(Path.Combine(repoRoot, "android", "app", "src", "main", "res", "values", "styles.xml"));

        Assert.Contains("app:titleTextAppearance=\"@style/WmsToolbarTitle\"", main);
        Assert.Contains("name=\"WmsToolbarTitle\"", styles);
        Assert.Contains("<item name=\"android:textSize\">18sp</item>", styles);
    }

    [Fact]
    public void AndroidFragmentDashboard_SummaryCardsUseDistinctMetrics()
    {
        string repoRoot = FindRepositoryRoot();
        string dashboard = ReadAndroidLayout(repoRoot, "fragment_dashboard.xml");

        Assert.Contains("@+id/activeFocusCard", dashboard);
        Assert.Contains("@+id/screenOnCard", dashboard);
        Assert.Contains("@+id/idleGapCard", dashboard);
        Assert.Contains("@+id/syncedCard", dashboard);
        Assert.Contains("@string/active_focus", dashboard);
        Assert.Contains("@string/screen_on", dashboard);
        Assert.Contains("@string/idle_time", dashboard);
        Assert.Contains("@string/sync_local_only_status", dashboard);
    }

    [Fact]
    public void AndroidFragmentDashboard_DoesNotHardcodeFakeRuntimeData()
    {
        string repoRoot = FindRepositoryRoot();
        string dashboard = ReadAndroidLayout(repoRoot, "fragment_dashboard.xml");

        Assert.DoesNotContain("com.android.chrome", dashboard, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Session duration   00:12:31", dashboard, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Last collected   09:25", dashboard, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Last DB write   09:25", dashboard, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AndroidFragmentDashboard_ExposesRoomBackedValueIds()
    {
        string repoRoot = FindRepositoryRoot();
        string dashboard = ReadAndroidLayout(repoRoot, "fragment_dashboard.xml");
        string fragment = File.ReadAllText(Path.Combine(
            repoRoot,
            "android",
            "app",
            "src",
            "main",
            "java",
            "com",
            "woong",
            "monitorstack",
            "dashboard",
            "DashboardFragment.kt"));

        string[] expectedIds =
        [
            "@+id/activeFocusValueText",
            "@+id/screenOnValueText",
            "@+id/idleValueText",
            "@+id/syncStateValueText"
        ];

        foreach (string expectedId in expectedIds)
        {
            Assert.Contains(expectedId, dashboard);
        }

        Assert.Contains("RoomDashboardRepository", fragment);
        Assert.Contains("DashboardViewModel", fragment);
        Assert.Contains("MonitorDatabase.getInstance", fragment);
    }

    [Fact]
    public void AndroidSessionsFragment_UsesRoomRepositoryAndEmptyState()
    {
        string repoRoot = FindRepositoryRoot();
        string layout = ReadAndroidLayout(repoRoot, "fragment_sessions.xml");
        string fragment = File.ReadAllText(Path.Combine(
            repoRoot,
            "android",
            "app",
            "src",
            "main",
            "java",
            "com",
            "woong",
            "monitorstack",
            "sessions",
            "SessionsFragment.kt"));

        Assert.Contains("@+id/sessionsRecyclerView", layout);
        Assert.Contains("@+id/emptySessionsText", layout);
        Assert.Contains("RoomSessionsRepository", fragment);
        Assert.Contains("MonitorDatabase.getInstance", fragment);
        Assert.Contains("submitRows", fragment);
    }

    private static string ReadAndroidLayout(string repoRoot, string fileName)
    {
        return File.ReadAllText(Path.Combine(repoRoot, "android", "app", "src", "main", "res", "layout", fileName));
    }

    private static void AssertMaterialCardContainer(string xml, string cardId)
    {
        string marker = $"android:id=\"@+id/{cardId}\"";
        int markerIndex = xml.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, $"Expected layout to contain {marker}.");
        int start = Math.Max(0, markerIndex - 160);
        string prefix = xml[start..markerIndex];

        Assert.Contains("com.google.android.material.card.MaterialCardView", prefix);
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
