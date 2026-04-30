package com.woong.monitorstack

import android.content.Context
import android.os.Looper
import android.view.View
import android.widget.Button
import android.widget.CheckBox
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.dashboard.DashboardFragment
import com.woong.monitorstack.settings.SharedPreferencesAndroidLocationSettings
import com.woong.monitorstack.usage.PermissionOnboardingFragment
import java.time.LocalDate
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.After
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.Robolectric
import org.robolectric.RobolectricTestRunner
import org.robolectric.Shadows.shadowOf
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class MainActivityTest {
    @After
    fun tearDown() {
        MainActivity.usageAccessGateFactory = MainActivity.defaultUsageAccessGateFactory()
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

        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()

        activity.supportFragmentManager.executePendingTransactions()

        assertEquals(
            PermissionOnboardingFragment::class.java,
            activity.supportFragmentManager.findFragmentById(R.id.mainFragmentContainer)?.javaClass
        )
        assertNotNull(activity.findViewById(R.id.openUsageAccessSettingsButton))
    }

    @Test
    fun whenUsageAccessGrantedShowsDashboard() {
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = true) }

        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()

        activity.supportFragmentManager.executePendingTransactions()

        assertEquals(
            DashboardFragment::class.java,
            activity.supportFragmentManager.findFragmentById(R.id.mainFragmentContainer)?.javaClass
        )
        assertEquals(
            R.id.navDashboard,
            activity.findViewById<com.google.android.material.bottomnavigation.BottomNavigationView>(
                R.id.bottomNavigation
            ).selectedItemId
        )
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
    fun reportTabLoadsRoomBackedSevenDaySummary() {
        MainActivity.usageAccessGateFactory = { FakeUsageAccessGate(hasAccess = true) }
        val context = ApplicationProvider.getApplicationContext<Context>()
        val database = MonitorDatabase.getInstance(context)
        val today = LocalDate.now().toString()
        Thread {
            database.focusSessionDao().insert(
                FocusSessionEntity(
                    clientSessionId = "report-fragment-chrome",
                    packageName = "com.android.chrome",
                    startedAtUtcMillis = 1_800_000_000_000L,
                    endedAtUtcMillis = 1_800_018_000_000L,
                    durationMs = 18_000_000L,
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

        activity.findViewById<com.google.android.material.bottomnavigation.BottomNavigationView>(
            R.id.bottomNavigation
        ).selectedItemId = R.id.navReport
        activity.supportFragmentManager.executePendingTransactions()
        waitForMainThreadWork()

        val totalCard = activity.findViewById<View>(R.id.reportTotalFocusCard)
        val topAppCard = activity.findViewById<View>(R.id.reportTopAppCard)
        val topAppsList = activity.findViewById<RecyclerView>(R.id.reportTopAppsRecyclerView)

        assertEquals(
            "5h 0m",
            totalCard.findViewById<TextView>(R.id.summaryValueText).text.toString()
        )
        assertEquals(
            "Chrome",
            topAppCard.findViewById<TextView>(R.id.summaryValueText).text.toString()
        )
        assertTrue(topAppsList.adapter?.itemCount ?: 0 > 0)
    }

    private fun waitForMainThreadWork() {
        Thread.sleep(250)
        shadowOf(Looper.getMainLooper()).idle()
    }

    private class FakeUsageAccessGate(
        private val hasAccess: Boolean
    ) : MainActivity.UsageAccessGate {
        override fun hasUsageAccess(packageName: String): Boolean = hasAccess
    }
}
