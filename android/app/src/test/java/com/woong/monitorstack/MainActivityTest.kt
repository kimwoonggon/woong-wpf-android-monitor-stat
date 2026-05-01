package com.woong.monitorstack

import android.content.Context
import android.os.Looper
import android.view.View
import android.widget.Button
import android.widget.CheckBox
import android.widget.TextView
import com.github.mikephil.charting.charts.BarChart
import com.github.mikephil.charting.charts.LineChart
import androidx.recyclerview.widget.RecyclerView
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.dashboard.DashboardFragment
import com.woong.monitorstack.sessions.AppDetailFragment
import com.woong.monitorstack.settings.SharedPreferencesAndroidLocationSettings
import com.woong.monitorstack.settings.SharedPreferencesAndroidSyncSettings
import com.woong.monitorstack.usage.AndroidRecentUsageCollector
import com.woong.monitorstack.usage.PermissionOnboardingFragment
import com.woong.monitorstack.usage.UsageCollectionScheduleResult
import java.time.LocalDate
import java.time.ZoneId
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertFalse
import org.junit.After
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.Robolectric
import org.robolectric.RobolectricTestRunner
import org.robolectric.Shadows.shadowOf
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class MainActivityTest {
    @Before
    fun setUp() {
        MainActivity.splashDelayMillis = 0L
        MainActivity.usageCollectionReconcilerFactory = {
            FakeUsageCollectionReconciler(result = UsageCollectionScheduleResult.Scheduled)
        }
        MainActivity.usageImmediateCollectorFactory = {
            NoopImmediateCollector
        }
    }

    @After
    fun tearDown() {
        MainActivity.usageAccessGateFactory = MainActivity.defaultUsageAccessGateFactory()
        MainActivity.usageCollectionReconcilerFactory =
            MainActivity.defaultUsageCollectionReconcilerFactory()
        MainActivity.usageImmediateCollectorFactory =
            MainActivity.defaultUsageImmediateCollectorFactory()
        MainActivity.splashDelayMillis = MainActivity.DefaultSplashDelayMillis
    }

    @Test
    fun launcherShowsSplashBeforeRoutingToDashboard() {
        MainActivity.splashDelayMillis = 500L
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = true) }
        val controller = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
        val activity = controller.get()
        activity.supportFragmentManager.executePendingTransactions()

        assertEquals(
            SplashFragment::class.java,
            activity.supportFragmentManager.findFragmentById(R.id.mainFragmentContainer)?.javaClass
        )
        assertEquals(View.GONE, activity.findViewById<View>(R.id.topAppBar).visibility)
        assertEquals(View.GONE, activity.findViewById<View>(R.id.bottomNavigation).visibility)

        shadowOf(Looper.getMainLooper()).idleFor(500, java.util.concurrent.TimeUnit.MILLISECONDS)
        activity.supportFragmentManager.executePendingTransactions()

        assertEquals(
            DashboardFragment::class.java,
            activity.supportFragmentManager.findFragmentById(R.id.mainFragmentContainer)?.javaClass
        )
        assertEquals(View.VISIBLE, activity.findViewById<View>(R.id.topAppBar).visibility)
        assertEquals(View.VISIBLE, activity.findViewById<View>(R.id.bottomNavigation).visibility)
    }

    @Test
    fun launcherShowsMainShellWithoutRedirectingToAnotherActivity() {
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = true) }
        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()

        assertNull(shadowOf(activity).nextStartedActivity)
        assertNotNull(activity.findViewById(R.id.topAppBar))
        assertNotNull(activity.findViewById(R.id.mainFragmentContainer))
        assertNotNull(activity.findViewById(R.id.bottomNavigation))
        assertEquals(
            R.id.navDashboard,
            activity.findViewById<com.google.android.material.bottomnavigation.BottomNavigationView>(
                R.id.bottomNavigation
            ).selectedItemId
        )
    }

    @Test
    fun whenUsageAccessMissingShowsPermissionOnboarding() {
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = false) }
        val reconciler = FakeUsageCollectionReconciler(
            result = UsageCollectionScheduleResult.UsageAccessMissing
        )
        MainActivity.usageCollectionReconcilerFactory = { reconciler }

        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()

        activity.supportFragmentManager.executePendingTransactions()

        assertEquals(
            PermissionOnboardingFragment::class.java,
            activity.supportFragmentManager.findFragmentById(R.id.mainFragmentContainer)?.javaClass
        )
        assertEquals(View.GONE, activity.findViewById<View>(R.id.topAppBar).visibility)
        assertEquals(View.GONE, activity.findViewById<View>(R.id.bottomNavigation).visibility)
        assertNotNull(activity.findViewById(R.id.openUsageAccessSettingsButton))
        assertEquals(
            "Collection paused until Usage Access is granted.",
            activity.findViewById<TextView>(R.id.permissionRuntimeStatusText).text.toString()
        )
        assertEquals(listOf("com.woong.monitorstack"), reconciler.packageNames)
    }

    @Test
    fun whenUsageAccessGrantedShowsDashboard() {
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = true) }
        val reconciler = FakeUsageCollectionReconciler(
            result = UsageCollectionScheduleResult.Scheduled
        )
        MainActivity.usageCollectionReconcilerFactory = { reconciler }

        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()

        activity.supportFragmentManager.executePendingTransactions()

        assertEquals(
            DashboardFragment::class.java,
            activity.supportFragmentManager.findFragmentById(R.id.mainFragmentContainer)?.javaClass
        )
        assertEquals(View.VISIBLE, activity.findViewById<View>(R.id.topAppBar).visibility)
        assertEquals(View.VISIBLE, activity.findViewById<View>(R.id.bottomNavigation).visibility)
        assertEquals(
            R.id.navDashboard,
            activity.findViewById<com.google.android.material.bottomnavigation.BottomNavigationView>(
                R.id.bottomNavigation
            ).selectedItemId
        )
        assertEquals(listOf("com.woong.monitorstack"), reconciler.packageNames)
    }

    @Test
    fun whenUsageAccessGrantedAfterSettingsReturnRechecksAndShowsDashboard() {
        val gate = MutableUsageAccessGate(hasAccess = false)
        val reconciler = FakeUsageCollectionReconciler(
            result = UsageCollectionScheduleResult.UsageAccessMissing
        )
        MainActivity.usageAccessGateFactory = { gate }
        MainActivity.usageCollectionReconcilerFactory = { reconciler }
        val controller = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
        val activity = controller.get()
        activity.supportFragmentManager.executePendingTransactions()

        assertEquals(
            PermissionOnboardingFragment::class.java,
            activity.supportFragmentManager.findFragmentById(R.id.mainFragmentContainer)?.javaClass
        )

        gate.hasAccess = true
        reconciler.result = UsageCollectionScheduleResult.Scheduled
        controller.pause().resume()
        activity.supportFragmentManager.executePendingTransactions()

        assertEquals(
            DashboardFragment::class.java,
            activity.supportFragmentManager.findFragmentById(R.id.mainFragmentContainer)?.javaClass
        )
        assertEquals(
            listOf("com.woong.monitorstack", "com.woong.monitorstack"),
            reconciler.packageNames
        )
    }

    @Test
    fun whenReturningToAppFromAnyTabReconcilesUsageCollection() {
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = true) }
        val reconciler = FakeUsageCollectionReconciler(
            result = UsageCollectionScheduleResult.Scheduled
        )
        MainActivity.usageCollectionReconcilerFactory = { reconciler }
        val controller = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
        val activity = controller.get()
        activity.supportFragmentManager.executePendingTransactions()

        activity.findViewById<com.google.android.material.bottomnavigation.BottomNavigationView>(
            R.id.bottomNavigation
        ).selectedItemId = R.id.navSessions
        activity.supportFragmentManager.executePendingTransactions()

        controller.pause().resume()

        assertEquals(
            listOf("com.woong.monitorstack", "com.woong.monitorstack"),
            reconciler.packageNames
        )
    }

    @Test
    fun dashboardCollectsRecentUsageAndReloadsRoomBackedRows() {
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = true) }
        val context = ApplicationProvider.getApplicationContext<Context>()
        clearMonitorDatabase(context)
        MainActivity.usageImmediateCollectorFactory = {
            FakeImmediateCollector(context)
        }

        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()
        activity.supportFragmentManager.executePendingTransactions()
        waitForMainThreadWork()

        assertEquals("Chrome", activity.findViewById<TextView>(R.id.currentAppText).text.toString())
        assertEquals("com.android.chrome", activity.findViewById<TextView>(R.id.currentPackageText).text.toString())
        assertEquals("2m", activity.findViewById<TextView>(R.id.activeFocusValueText).text.toString())
    }

    @Test
    fun dashboardCurrentFocusShowsLatestTrackedExternalAppAfterReturningFromChrome() {
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = true) }
        val context = ApplicationProvider.getApplicationContext<Context>()
        clearMonitorDatabase(context)
        MainActivity.usageImmediateCollectorFactory = {
            FakeMixedImmediateCollector(context)
        }

        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()
        activity.supportFragmentManager.executePendingTransactions()
        waitForMainThreadWork()

        assertEquals(
            "Chrome",
            activity.findViewById<TextView>(R.id.currentAppText).text.toString()
        )
        assertEquals(
            "com.android.chrome",
            activity.findViewById<TextView>(R.id.currentPackageText).text.toString()
        )
        assertEquals(
            "13m",
            activity.findViewById<TextView>(R.id.activeFocusValueText).text.toString()
        )
    }

    @Test
    fun dashboardLatestPersistedSessionsStillRenderInRecentSessions() {
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = true) }
        val context = ApplicationProvider.getApplicationContext<Context>()
        clearMonitorDatabase(context)
        MainActivity.usageImmediateCollectorFactory = {
            FakeMixedImmediateCollector(context)
        }

        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()
        activity.supportFragmentManager.executePendingTransactions()
        waitForMainThreadWork()

        val recentSessions = activity.findViewById<RecyclerView>(R.id.recentSessionsRecyclerView)
        assertTrue(recentSessions.adapter?.itemCount ?: 0 > 0)
    }

    @Test
    fun dashboardRollingPeriodButtonsReloadRoomBackedSummary() {
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = true) }
        val context = ApplicationProvider.getApplicationContext<Context>()
        clearMonitorDatabase(context)
        val database = MonitorDatabase.getInstance(context)
        val timezoneId = ZoneId.systemDefault()
        val now = System.currentTimeMillis()
        Thread {
            database.focusSessionDao().insert(
                focusSession(
                    clientSessionId = "dashboard-recent-chrome",
                    packageName = "com.android.chrome",
                    startedAtUtcMillis = now - 30 * 60_000L,
                    endedAtUtcMillis = now - 10 * 60_000L,
                    timezoneId = timezoneId
                )
            )
            database.focusSessionDao().insert(
                focusSession(
                    clientSessionId = "dashboard-older-slack",
                    packageName = "com.slack",
                    startedAtUtcMillis = now - 3 * 60 * 60_000L,
                    endedAtUtcMillis = now - 2 * 60 * 60_000L,
                    timezoneId = timezoneId
                )
            )
        }.also { it.start(); it.join() }

        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()
        activity.supportFragmentManager.executePendingTransactions()
        waitForMainThreadWork()

        activity.findViewById<Button>(R.id.oneHourFilterButton).performClick()
        waitForMainThreadWork()

        assertEquals("20m", activity.findViewById<TextView>(R.id.activeFocusValueText).text.toString())
        assertEquals("Chrome", activity.findViewById<TextView>(R.id.currentAppText).text.toString())

        activity.findViewById<Button>(R.id.sixHourFilterButton).performClick()
        waitForMainThreadWork()

        assertEquals("1h 20m", activity.findViewById<TextView>(R.id.activeFocusValueText).text.toString())
        assertTrue(
            activity.findViewById<RecyclerView>(R.id.topAppsRecyclerView).adapter?.itemCount ?: 0 > 0
        )
        assertNotNull(activity.findViewById<BarChart>(R.id.hourlyFocusChart).data)
    }

    @Test
    fun permissionOnboardingOpenSettingsButtonLaunchesUsageAccessSettings() {
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = false) }
        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()
        activity.supportFragmentManager.executePendingTransactions()

        activity.findViewById<Button>(R.id.openUsageAccessSettingsButton).performClick()

        assertEquals(
            android.provider.Settings.ACTION_USAGE_ACCESS_SETTINGS,
            shadowOf(activity).nextStartedActivity.action
        )
    }

    @Test
    fun settingsTabShowsRuntimePrivacySyncAndLocationControls() {
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = true) }
        val context = ApplicationProvider.getApplicationContext<Context>()
        context.getSharedPreferences(
            SharedPreferencesAndroidLocationSettings.PreferenceName,
            Context.MODE_PRIVATE
        ).edit().clear().commit()
        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()

        activity.findViewById<com.google.android.material.bottomnavigation.BottomNavigationView>(
            R.id.bottomNavigation
        ).selectedItemId = R.id.navSettings
        activity.supportFragmentManager.executePendingTransactions()

        assertNotNull(activity.findViewById(R.id.openUsageAccessSettingsButton))
        assertEquals(
            "This app does not collect messages, passwords, form input, or global touch coordinates.",
            activity.findViewById<TextView>(R.id.sensitiveDataBoundaryText).text.toString()
        )
        assertEquals(
            "Sync is off. Data stays on this Android device.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )

        val locationContext = activity.findViewById<CheckBox>(R.id.locationContextCheckBox)
        val preciseLatitudeLongitude = activity.findViewById<CheckBox>(
            R.id.preciseLatitudeLongitudeCheckBox
        )

        assertEquals(false, locationContext.isChecked)
        assertEquals(false, preciseLatitudeLongitude.isChecked)
        assertEquals(false, preciseLatitudeLongitude.isEnabled)
    }

    @Test
    fun settingsTabPersistsSyncOptInAndShowsManualSyncSkippedWhenOff() {
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = true) }
        val context = ApplicationProvider.getApplicationContext<Context>()
        context.getSharedPreferences(
            SharedPreferencesAndroidSyncSettings.PreferenceName,
            Context.MODE_PRIVATE
        ).edit().clear().commit()
        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()

        activity.findViewById<com.google.android.material.bottomnavigation.BottomNavigationView>(
            R.id.bottomNavigation
        ).selectedItemId = R.id.navSettings
        activity.supportFragmentManager.executePendingTransactions()

        val autoSyncSwitch =
            activity.findViewById<com.google.android.material.switchmaterial.SwitchMaterial>(
                R.id.autoSyncSwitch
            )
        val manualSyncButton = activity.findViewById<Button>(R.id.manualSyncButton)
        val syncStatusText = activity.findViewById<TextView>(R.id.syncStatusText)

        assertFalse(autoSyncSwitch.isChecked)
        assertTrue(manualSyncButton.isEnabled)

        manualSyncButton.performClick()

        assertEquals(
            "Manual sync skipped because sync is off. Local only.",
            syncStatusText.text.toString()
        )

        autoSyncSwitch.performClick()

        assertTrue(SharedPreferencesAndroidSyncSettings(context).isSyncEnabled())
        assertEquals(
            "Sync is on. Manual sync will use configured server settings.",
            syncStatusText.text.toString()
        )
    }

    @Test
    fun reportTabLoadsRoomBackedSevenDaySummary() {
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = true) }
        val context = ApplicationProvider.getApplicationContext<Context>()
        clearMonitorDatabase(context)
        val database = MonitorDatabase.getInstance(context)
        val timezoneId = ZoneId.systemDefault()
        val endedAtUtcMillis = System.currentTimeMillis()
        val startedAtUtcMillis = endedAtUtcMillis - 18_000_000L
        val today = LocalDate.now(timezoneId).toString()
        Thread {
            database.focusSessionDao().insert(
                FocusSessionEntity(
                    clientSessionId = "report-fragment-chrome",
                    packageName = "com.android.chrome",
                    startedAtUtcMillis = startedAtUtcMillis,
                    endedAtUtcMillis = endedAtUtcMillis,
                    durationMs = 18_000_000L,
                    localDate = today,
                    timezoneId = timezoneId.id,
                    isIdle = false,
                    source = "test"
                )
            )
        }.also { it.start(); it.join() }

        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()

        activity.findViewById<com.google.android.material.bottomnavigation.BottomNavigationView>(
            R.id.bottomNavigation
        ).selectedItemId = R.id.navReport
        activity.supportFragmentManager.executePendingTransactions()
        waitForMainThreadWork()

        val totalCard = activity.findViewById<View>(R.id.reportTotalFocusCard)
        val topAppCard = activity.findViewById<View>(R.id.reportTopAppCard)
        val topAppsList = activity.findViewById<RecyclerView>(R.id.reportTopAppsRecyclerView)
        val trendChart = activity.findViewById<LineChart>(R.id.sevenDayTrendChart)

        assertEquals(
            "5h 0m",
            totalCard.findViewById<TextView>(R.id.summaryValueText).text.toString()
        )
        assertEquals(
            "Chrome",
            topAppCard.findViewById<TextView>(R.id.summaryValueText).text.toString()
        )
        assertTrue(topAppsList.adapter?.itemCount ?: 0 > 0)
        assertNotNull(trendChart.data)
        assertEquals(1, trendChart.data.getDataSetByIndex(0).entryCount)
    }

    @Test
    fun reportTabThirtyAndNinetyDayButtonsReloadRoomBackedSummary() {
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = true) }
        val context = ApplicationProvider.getApplicationContext<Context>()
        clearMonitorDatabase(context)
        val database = MonitorDatabase.getInstance(context)
        val timezoneId = ZoneId.systemDefault()
        val today = LocalDate.now(timezoneId)
        Thread {
            database.focusSessionDao().insert(
                reportSession(
                    clientSessionId = "report-today-chrome",
                    packageName = "com.android.chrome",
                    localDate = today,
                    durationMs = 10 * 60_000L,
                    timezoneId = timezoneId
                )
            )
            database.focusSessionDao().insert(
                reportSession(
                    clientSessionId = "report-day-20-youtube",
                    packageName = "com.google.android.youtube",
                    localDate = today.minusDays(20),
                    durationMs = 20 * 60_000L,
                    timezoneId = timezoneId
                )
            )
            database.focusSessionDao().insert(
                reportSession(
                    clientSessionId = "report-day-80-slack",
                    packageName = "com.slack",
                    localDate = today.minusDays(80),
                    durationMs = 40 * 60_000L,
                    timezoneId = timezoneId
                )
            )
        }.also { it.start(); it.join() }

        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()

        activity.findViewById<com.google.android.material.bottomnavigation.BottomNavigationView>(
            R.id.bottomNavigation
        ).selectedItemId = R.id.navReport
        activity.supportFragmentManager.executePendingTransactions()
        waitForMainThreadWork()

        val totalCard = activity.findViewById<View>(R.id.reportTotalFocusCard)
        val averageCard = activity.findViewById<View>(R.id.reportAverageCard)
        val topAppCard = activity.findViewById<View>(R.id.reportTopAppCard)
        val trendChart = activity.findViewById<LineChart>(R.id.sevenDayTrendChart)
        val dateRangeText = activity.findViewById<TextView>(R.id.reportDateRangeText)

        assertEquals(
            "10m",
            totalCard.findViewById<TextView>(R.id.summaryValueText).text.toString()
        )

        activity.findViewById<Button>(R.id.reportThirtyDayButton).performClick()
        waitForMainThreadWork()

        assertEquals(
            "30m",
            totalCard.findViewById<TextView>(R.id.summaryValueText).text.toString()
        )
        assertEquals(
            "1m",
            averageCard.findViewById<TextView>(R.id.summaryValueText).text.toString()
        )
        assertEquals(
            "YouTube",
            topAppCard.findViewById<TextView>(R.id.summaryValueText).text.toString()
        )
        assertTrue(dateRangeText.text.toString().contains(today.minusDays(29).toString()))
        assertEquals(2, trendChart.data.getDataSetByIndex(0).entryCount)

        activity.findViewById<Button>(R.id.reportNinetyDayButton).performClick()
        waitForMainThreadWork()

        assertEquals(
            "1h 10m",
            totalCard.findViewById<TextView>(R.id.summaryValueText).text.toString()
        )
        assertEquals(
            "Slack",
            topAppCard.findViewById<TextView>(R.id.summaryValueText).text.toString()
        )
        assertTrue(dateRangeText.text.toString().contains(today.minusDays(89).toString()))
        assertEquals(3, trendChart.data.getDataSetByIndex(0).entryCount)
    }

    @Test
    fun appDetailLoadsRoomBackedHourlyChartForSelectedApp() {
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = true) }
        val context = ApplicationProvider.getApplicationContext<Context>()
        clearMonitorDatabase(context)
        val database = MonitorDatabase.getInstance(context)
        val today = LocalDate.now().toString()
        Thread {
            database.focusSessionDao().insert(
                FocusSessionEntity(
                    clientSessionId = "app-detail-chrome-1",
                    packageName = "com.android.chrome",
                    startedAtUtcMillis = 1_800_000_000_000L,
                    endedAtUtcMillis = 1_800_006_000_000L,
                    durationMs = 6_000_000L,
                    localDate = today,
                    timezoneId = "Asia/Seoul",
                    isIdle = false,
                    source = "test"
                )
            )
            database.focusSessionDao().insert(
                FocusSessionEntity(
                    clientSessionId = "app-detail-chrome-2",
                    packageName = "com.android.chrome",
                    startedAtUtcMillis = 1_800_007_200_000L,
                    endedAtUtcMillis = 1_800_010_200_000L,
                    durationMs = 3_000_000L,
                    localDate = today,
                    timezoneId = "Asia/Seoul",
                    isIdle = false,
                    source = "test"
                )
            )
        }.also { it.start(); it.join() }

        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()
        activity.supportFragmentManager
            .beginTransaction()
            .replace(
                R.id.mainFragmentContainer,
                AppDetailFragment.newInstance("com.android.chrome")
            )
            .commitNow()
        waitForMainThreadWork()

        val chart = activity.findViewById<BarChart>(R.id.appHourlyChart)

        assertNotNull(chart.data)
        assertEquals(2, chart.data.getDataSetByIndex(0).entryCount)
    }

    private fun waitForMainThreadWork() {
        Thread.sleep(500)
        shadowOf(Looper.getMainLooper()).idle()
    }

    private fun clearMonitorDatabase(context: Context) {
        Thread {
            MonitorDatabase.getInstance(context).clearAllTables()
        }.also { it.start(); it.join() }
    }

    private fun focusSession(
        clientSessionId: String,
        packageName: String,
        startedAtUtcMillis: Long,
        endedAtUtcMillis: Long,
        timezoneId: ZoneId
    ): FocusSessionEntity {
        return FocusSessionEntity(
            clientSessionId = clientSessionId,
            packageName = packageName,
            startedAtUtcMillis = startedAtUtcMillis,
            endedAtUtcMillis = endedAtUtcMillis,
            durationMs = endedAtUtcMillis - startedAtUtcMillis,
            localDate = java.time.Instant.ofEpochMilli(startedAtUtcMillis)
                .atZone(timezoneId)
                .toLocalDate()
                .toString(),
            timezoneId = timezoneId.id,
            isIdle = false,
            source = "test"
        )
    }

    private val packageNameForTest = "com.woong.monitorstack"

    private class FakeUsageAccessGate(
        private val hasAccess: Boolean
    ) : MainActivity.UsageAccessGate {
        override fun hasUsageAccess(packageName: String): Boolean = hasAccess
    }

    private class MutableUsageAccessGate(
        var hasAccess: Boolean
    ) : MainActivity.UsageAccessGate {
        override fun hasUsageAccess(packageName: String): Boolean = hasAccess
    }

    private class FakeUsageCollectionReconciler(
        var result: UsageCollectionScheduleResult
    ) : MainActivity.UsageCollectionReconciler {
        val packageNames = mutableListOf<String>()

        override fun reconcile(packageName: String): UsageCollectionScheduleResult {
            packageNames += packageName
            return result
        }
    }

    private object NoopImmediateCollector : AndroidRecentUsageCollector {
        override fun collectRecentUsage(): Int = 0
    }

    private class FakeImmediateCollector(
        private val context: Context
    ) : AndroidRecentUsageCollector {
        override fun collectRecentUsage(): Int {
            val timezoneId = ZoneId.systemDefault()
            val startedAtUtcMillis = System.currentTimeMillis() - 120_000L
            val endedAtUtcMillis = System.currentTimeMillis()
            MonitorDatabase.getInstance(context).focusSessionDao().insert(
                FocusSessionEntity(
                    clientSessionId = "fake-immediate-chrome",
                    packageName = "com.android.chrome",
                    startedAtUtcMillis = startedAtUtcMillis,
                    endedAtUtcMillis = endedAtUtcMillis,
                    durationMs = endedAtUtcMillis - startedAtUtcMillis,
                    localDate = LocalDate.now(timezoneId).toString(),
                    timezoneId = timezoneId.id,
                    isIdle = false,
                    source = "fake_immediate_usage"
                )
            )
            return 1
        }
    }

    private class FakeMixedImmediateCollector(
        private val context: Context
    ) : AndroidRecentUsageCollector {
        override fun collectRecentUsage(): Int {
            val timezoneId = ZoneId.systemDefault()
            val now = System.currentTimeMillis()
            val dao = MonitorDatabase.getInstance(context).focusSessionDao()
            dao.insert(
                mixedFocusSession(
                    clientSessionId = "fake-immediate-chrome",
                    packageName = "com.android.chrome",
                    startedAtUtcMillis = now - 900_000L,
                    endedAtUtcMillis = now - 420_000L,
                    timezoneId = timezoneId
                )
            )
            dao.insert(
                mixedFocusSession(
                    clientSessionId = "fake-immediate-launcher",
                    packageName = "com.google.android.apps.nexuslauncher",
                    startedAtUtcMillis = now - 360_000L,
                    endedAtUtcMillis = now - 180_000L,
                    timezoneId = timezoneId
                )
            )
            dao.insert(
                mixedFocusSession(
                    clientSessionId = "fake-immediate-monitor-latest",
                    packageName = "com.woong.monitorstack",
                    startedAtUtcMillis = now - 120_000L,
                    endedAtUtcMillis = now,
                    timezoneId = timezoneId
                )
            )
            return 3
        }

        private fun mixedFocusSession(
            clientSessionId: String,
            packageName: String,
            startedAtUtcMillis: Long,
            endedAtUtcMillis: Long,
            timezoneId: ZoneId
        ): FocusSessionEntity {
            return FocusSessionEntity(
                clientSessionId = clientSessionId,
                packageName = packageName,
                startedAtUtcMillis = startedAtUtcMillis,
                endedAtUtcMillis = endedAtUtcMillis,
                durationMs = endedAtUtcMillis - startedAtUtcMillis,
                localDate = LocalDate.now(timezoneId).toString(),
                timezoneId = timezoneId.id,
                isIdle = false,
                source = "fake_immediate_usage"
            )
        }
    }

    private fun reportSession(
        clientSessionId: String,
        packageName: String,
        localDate: LocalDate,
        durationMs: Long,
        timezoneId: ZoneId
    ): FocusSessionEntity {
        val startedAtUtcMillis = localDate.atTime(9, 0)
            .atZone(timezoneId)
            .toInstant()
            .toEpochMilli()

        return FocusSessionEntity(
            clientSessionId = clientSessionId,
            packageName = packageName,
            startedAtUtcMillis = startedAtUtcMillis,
            endedAtUtcMillis = startedAtUtcMillis + durationMs,
            durationMs = durationMs,
            localDate = localDate.toString(),
            timezoneId = timezoneId.id,
            isIdle = false,
            source = "report_test"
        )
    }
}
