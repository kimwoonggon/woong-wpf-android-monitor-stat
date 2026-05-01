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
        Assert.Contains("@+id/sessionAppNameText", row);
        Assert.Contains("@+id/sessionPackageText", row);
        Assert.Contains("@+id/sessionTimeRangeText", row);
        Assert.Contains("@+id/sessionDurationText", row);
        Assert.Contains("@+id/sessionStateText", row);
    }

    [Fact]
    public void AndroidFocusSessionRowLayout_UsesReadableHeightForAppPackageTimeAndState()
    {
        string repoRoot = FindRepositoryRoot();
        string row = ReadAndroidLayout(repoRoot, "item_focus_session.xml");

        Assert.Contains("android:layout_height=\"wrap_content\"", row);
        Assert.Contains("android:minHeight=\"116dp\"", row);
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
        Assert.Contains("@+id/permissionShieldIcon", permission);
        Assert.Contains("@+id/permissionPrinciplesCard", permission);
        Assert.Contains("@+id/permissionRuntimeStatusText", permission);

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
    public void AndroidSplashFragment_UsesGoalScreenBranding()
    {
        string repoRoot = FindRepositoryRoot();
        string splash = ReadAndroidLayout(repoRoot, "fragment_splash.xml");

        Assert.Contains("@+id/splashLogoContainer", splash);
        Assert.Contains("@drawable/bg_android_logo_tile", splash);
        Assert.Contains("@drawable/ic_android_logo_bars", splash);
        Assert.Contains("@string/android_focus_tracker", splash);
        Assert.Contains("@string/loading_korean", splash);
        Assert.DoesNotContain("@drawable/bg_circle_logo_placeholder", splash);
    }

    [Fact]
    public void AndroidPermissionFragment_UsesGoalScreenPermissionCards()
    {
        string repoRoot = FindRepositoryRoot();
        string permission = ReadAndroidLayout(repoRoot, "fragment_permission_onboarding.xml");

        Assert.Contains("@+id/permissionBackButton", permission);
        Assert.Contains("@+id/permissionShieldIcon", permission);
        Assert.Contains("@+id/permissionPrinciplesCard", permission);
        Assert.Contains("@string/permission_headline", permission);
        Assert.Contains("@string/permission_body", permission);
        Assert.Contains("@string/permission_setting_open_korean", permission);
        Assert.Contains("@string/permission_principle_local_only", permission);
        Assert.Contains("@string/permission_principle_no_external_sync", permission);
        Assert.Contains("@string/permission_principle_no_hidden_sync", permission);
        Assert.Contains("@string/permission_principle_can_revoke", permission);
    }

    [Fact]
    public void AndroidLaunchTheme_UsesWoongSplashBrandingOnAndroidTwelveAndLater()
    {
        string repoRoot = FindRepositoryRoot();
        string stylesV31 = File.ReadAllText(Path.Combine(
            repoRoot,
            "android",
            "app",
            "src",
            "main",
            "res",
            "values-v31",
            "styles.xml"));

        Assert.Contains("name=\"Theme.WoongMonitor\"", stylesV31);
        Assert.Contains("android:windowSplashScreenBackground", stylesV31);
        Assert.Contains("@color/wms_surface", stylesV31);
        Assert.Contains("android:windowSplashScreenAnimatedIcon", stylesV31);
        Assert.Contains("@drawable/ic_android_logo_bars", stylesV31);
        Assert.Contains("android:windowSplashScreenIconBackgroundColor", stylesV31);
        Assert.Contains("@color/wms_primary", stylesV31);
    }

    [Fact]
    public void AndroidMainShell_UsesCompactReadableToolbarTitle()
    {
        string repoRoot = FindRepositoryRoot();
        string main = ReadAndroidLayout(repoRoot, "activity_main.xml");
        string styles = File.ReadAllText(Path.Combine(repoRoot, "android", "app", "src", "main", "res", "values", "styles.xml"));

        Assert.Contains("app:titleTextAppearance=\"@style/WmsToolbarTitle\"", main);
        Assert.Contains("name=\"WmsToolbarTitle\"", styles);
        Assert.Contains("<item name=\"actionBarSize\">56dp</item>", styles);
        Assert.Contains("android:minHeight=\"?attr/actionBarSize\"", main);
        Assert.Contains("app:contentInsetStart=\"16dp\"", main);
        Assert.Contains("app:contentInsetStartWithNavigation=\"16dp\"", main);
        Assert.Contains("<item name=\"android:textSize\">16sp</item>", styles);
    }

    [Fact]
    public void AndroidMainShell_UsesCompactWireframeBottomNavigation()
    {
        string repoRoot = FindRepositoryRoot();
        string main = ReadAndroidLayout(repoRoot, "activity_main.xml");
        string styles = File.ReadAllText(Path.Combine(repoRoot, "android", "app", "src", "main", "res", "values", "styles.xml"));

        Assert.Contains("android:layout_marginBottom=\"@dimen/bottom_navigation_base_height\"", main);
        Assert.Contains("android:layout_height=\"@dimen/bottom_navigation_base_height\"", main);
        Assert.DoesNotContain("android:layout_marginBottom=\"144dp\"", main);
        Assert.DoesNotContain("android:layout_marginBottom=\"48dp\"", main);
        Assert.Contains("app:itemIconSize=\"18dp\"", main);
        Assert.Contains("app:itemPaddingTop=\"0dp\"", main);
        Assert.Contains("app:itemPaddingBottom=\"0dp\"", main);
        Assert.Contains("app:itemTextAppearanceActive=\"@style/WmsBottomNavLabel\"", main);
        Assert.Contains("app:itemTextAppearanceInactive=\"@style/WmsBottomNavLabel\"", main);
        Assert.DoesNotContain("@+id/bottomNavigationLabelRow", main);
        Assert.DoesNotContain("@+id/mainNavDashboardLabel", main);
        Assert.DoesNotContain("@+id/mainNavSessionsLabel", main);
        Assert.DoesNotContain("@+id/mainNavReportLabel", main);
        Assert.DoesNotContain("@+id/mainNavSettingsLabel", main);
        Assert.Contains("name=\"WmsBottomNavLabel\"", styles);
        Assert.Contains("<item name=\"android:textSize\">10sp</item>", styles);
    }

    [Fact]
    public void AndroidMainShell_BottomNavigationKeepsCompactLabelsAboveSystemNavigation()
    {
        string repoRoot = FindRepositoryRoot();
        string main = ReadAndroidLayout(repoRoot, "activity_main.xml");

        Assert.Contains("android:layout_marginBottom=\"@dimen/bottom_navigation_base_height\"", main);
        Assert.Contains("android:layout_height=\"@dimen/bottom_navigation_base_height\"", main);
        Assert.DoesNotContain("android:layout_marginBottom=\"144dp\"", main);
        Assert.DoesNotContain("android:layout_marginBottom=\"48dp\"", main);
        Assert.DoesNotContain("android:paddingBottom=\"8dp\"", main);
        Assert.Contains("app:itemIconSize=\"18dp\"", main);
        Assert.Contains("app:itemPaddingTop=\"0dp\"", main);
        Assert.Contains("app:itemPaddingBottom=\"0dp\"", main);
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
    public void AndroidFragmentDashboard_CurrentFocusUsesCompactHorizontalRuntimeCard()
    {
        string repoRoot = FindRepositoryRoot();
        string dashboard = ReadAndroidLayout(repoRoot, "fragment_dashboard.xml");

        Assert.Contains("@+id/currentFocusRuntimeRow", dashboard);
        Assert.Contains("@+id/currentFocusAppIconPlaceholder", dashboard);
        Assert.Contains("@+id/currentFocusIdentityColumn", dashboard);
        Assert.Contains("@+id/currentFocusTimingColumn", dashboard);
        Assert.Contains("android:orientation=\"horizontal\"", dashboard);

        int titleIndex = dashboard.IndexOf("@+id/currentFocusTitle", StringComparison.Ordinal);
        int rowIndex = dashboard.IndexOf("@+id/currentFocusRuntimeRow", StringComparison.Ordinal);
        int iconIndex = dashboard.IndexOf("@+id/currentFocusAppIconPlaceholder", StringComparison.Ordinal);
        int identityIndex = dashboard.IndexOf("@+id/currentFocusIdentityColumn", StringComparison.Ordinal);
        int timingIndex = dashboard.IndexOf("@+id/currentFocusTimingColumn", StringComparison.Ordinal);

        Assert.True(titleIndex >= 0, "Expected Current Focus title.");
        Assert.True(rowIndex > titleIndex, "Expected the runtime row directly after the Current Focus title.");
        Assert.True(iconIndex > rowIndex, "Expected the app icon placeholder inside the runtime row.");
        Assert.True(identityIndex > iconIndex, "Expected app identity text after the icon.");
        Assert.True(timingIndex > identityIndex, "Expected session and collection timing after app identity.");
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
    public void AndroidFragmentDashboard_ShowsOptionalLocationContextFromRoomState()
    {
        string repoRoot = FindRepositoryRoot();
        string dashboard = ReadAndroidLayout(repoRoot, "fragment_dashboard.xml");
        string strings = File.ReadAllText(Path.Combine(repoRoot, "android", "app", "src", "main", "res", "values", "strings.xml"));
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

        Assert.Contains("@+id/locationContextCard", dashboard);
        Assert.Contains("@+id/locationStatusText", dashboard);
        Assert.Contains("@+id/locationLatitudeText", dashboard);
        Assert.Contains("@+id/locationLongitudeText", dashboard);
        Assert.Contains("@+id/locationAccuracyText", dashboard);
        Assert.Contains("@+id/locationCapturedAtText", dashboard);
        Assert.Contains("location_latitude_value", strings);
        Assert.Contains("location_longitude_value", strings);
        Assert.Contains("location_accuracy_value", strings);
        Assert.Contains("location_captured_at_value", strings);
        Assert.Contains("state.locationContext", fragment);
        Assert.Contains("R.string.location_latitude_value", fragment);
        Assert.Contains("R.string.location_longitude_value", fragment);
        Assert.Contains("binding.locationLatitudeText.text", fragment);
        Assert.Contains("binding.locationLongitudeText.text", fragment);
    }

    [Fact]
    public void AndroidFragmentDashboard_KeepsPeriodFiltersBeforeOptionalLocationContext()
    {
        string repoRoot = FindRepositoryRoot();
        string dashboard = ReadAndroidLayout(repoRoot, "fragment_dashboard.xml");

        int summaryIndex = dashboard.IndexOf("@+id/summaryCardsGrid", StringComparison.Ordinal);
        int periodIndex = dashboard.IndexOf("@+id/periodFilterScroll", StringComparison.Ordinal);
        int locationIndex = dashboard.IndexOf("@+id/locationContextCard", StringComparison.Ordinal);
        int chartIndex = dashboard.IndexOf("@+id/hourlyFocusChartCard", StringComparison.Ordinal);

        Assert.True(summaryIndex >= 0, "Expected summary cards in the dashboard.");
        Assert.True(periodIndex > summaryIndex, "Expected period filters after summary cards.");
        Assert.True(locationIndex > periodIndex, "Optional location context must not push period filters below the first dashboard flow.");
        Assert.True(chartIndex > locationIndex, "Expected charts after period filters and optional location context.");
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

    [Fact]
    public void AndroidSessionsFragment_UsesReferenceListAndFilterStructure()
    {
        string repoRoot = FindRepositoryRoot();
        string layout = ReadAndroidLayout(repoRoot, "fragment_sessions.xml");

        Assert.Contains("@+id/sessionsSubtitle", layout);
        Assert.Contains("@+id/sessionsFilterRow", layout);
        Assert.Contains("@+id/sessionsFilterButton", layout);
        Assert.Contains("@+id/sessionsTotalCountText", layout);
        Assert.Contains("@+id/sessionsListCard", layout);
        Assert.Contains("@+id/sessionsRecyclerView", layout);
    }

    [Fact]
    public void AndroidAppDetailFragment_UsesReferenceAnalysisStructure()
    {
        string repoRoot = FindRepositoryRoot();
        string layout = ReadAndroidLayout(repoRoot, "fragment_app_detail.xml");

        Assert.Contains("@+id/appDetailBackButton", layout);
        Assert.Contains("@+id/appDetailIdentityRow", layout);
        Assert.Contains("@+id/appDetailIconPlaceholder", layout);
        Assert.Contains("@+id/appTotalDurationCard", layout);
        Assert.Contains("@+id/appSessionCountCard", layout);
        Assert.Contains("@+id/appHourlyChartCard", layout);
        Assert.Contains("@+id/appHourlyChart", layout);
        Assert.Contains("@+id/appDetailSessionsCard", layout);
        Assert.Contains("@+id/appDetailSessionsRecyclerView", layout);
    }

    [Fact]
    public void AndroidReportFragment_UsesReferenceTrendReportStructure()
    {
        string repoRoot = FindRepositoryRoot();
        string layout = ReadAndroidLayout(repoRoot, "fragment_report.xml");

        Assert.Contains("@+id/reportSubtitle", layout);
        Assert.Contains("@+id/reportPeriodRow", layout);
        Assert.Contains("@+id/reportCustomButton", layout);
        Assert.Contains("@+id/reportDateRangeText", layout);
        Assert.Contains("@+id/reportTrendChartCard", layout);
        Assert.Contains("@+id/sevenDayTrendChart", layout);
        Assert.Contains("@+id/reportTopAppsCard", layout);
    }

    [Fact]
    public void AndroidSettingsFragment_UsesReferenceGroupedRuntimeSettings()
    {
        string repoRoot = FindRepositoryRoot();
        string layout = ReadAndroidLayout(repoRoot, "fragment_settings.xml");

        Assert.Contains("@+id/usageAccessStatusRow", layout);
        Assert.Contains("@+id/collectionSettingsCard", layout);
        Assert.Contains("@+id/backgroundCollectionSwitch", layout);
        Assert.Contains("@+id/collectionIntervalText", layout);
        Assert.Contains("@+id/autoSyncSwitch", layout);
        Assert.Contains("@+id/manualSyncButton", layout);
        Assert.Contains("@+id/privacyStorageSettingsCard", layout);
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
