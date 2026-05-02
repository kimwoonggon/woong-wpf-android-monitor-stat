package com.woong.monitorstack.dashboard

import android.content.Context
import android.os.Looper
import android.view.View
import android.widget.TextView
import androidx.test.core.app.ActivityScenario
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.R
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.MonitorDatabase
import java.time.Instant
import java.time.ZoneId
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.Shadows.shadowOf
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class DashboardActivityRobolectricTest {
    @Test
    fun dashboardActivityHostsCanonicalDashboardFragmentSurface() {
        ActivityScenario.launch(DashboardActivity::class.java).use { scenario ->
            scenario.onActivity { activity ->
                activity.supportFragmentManager.executePendingTransactions()

                val fragment = activity.supportFragmentManager.findFragmentById(
                    R.id.mainFragmentContainer
                )
                assertTrue(fragment is DashboardFragment)
                assertNotNull(activity.findViewById<View>(R.id.dashboardScrollRoot))
                assertEquals(
                    "Obsolete legacy Activity id must not remain in packaged resources: dashboardRoot",
                    0,
                    activity.resources.getIdentifier("dashboardRoot", "id", activity.packageName)
                )
                assertEquals(
                    "Current Focus",
                    activity.findViewById<TextView>(R.id.currentFocusTitle).text.toString()
                )
                assertEquals(
                    "Usage OK",
                    activity.findViewById<TextView>(R.id.usageAccessStatusChip).text.toString()
                )
                assertEquals(
                    "Sync Off",
                    activity.findViewById<TextView>(R.id.syncStatusChip).text.toString()
                )
                assertEquals(
                    "Privacy Safe",
                    activity.findViewById<TextView>(R.id.privacyStatusChip).text.toString()
                )
                assertNotNull(activity.findViewById<View>(R.id.hourlyFocusChartCard))
                assertNotNull(activity.findViewById<View>(R.id.topAppsCard))
            }
        }
    }

    @Test
    fun dashboardActivityCanonicalFragmentShowsLocationStatusCardWithSafeDefaults() {
        ActivityScenario.launch(DashboardActivity::class.java).use { scenario ->
            scenario.onActivity { activity ->
                activity.supportFragmentManager.executePendingTransactions()

                assertNotNull(activity.findViewById<View>(R.id.locationContextCard))
                assertEquals(
                    "Location context",
                    activity.findViewById<TextView>(R.id.locationContextLabel).text.toString()
                )
                assertEquals(
                    "Location capture off",
                    activity.findViewById<TextView>(R.id.locationStatusText).text.toString()
                )
                assertEquals(
                    activity.getString(R.string.location_latitude_value, "Latitude not stored"),
                    activity.findViewById<TextView>(R.id.locationLatitudeText).text.toString()
                )
                assertEquals(
                    activity.getString(R.string.location_longitude_value, "Longitude not stored"),
                    activity.findViewById<TextView>(R.id.locationLongitudeText).text.toString()
                )
                assertEquals(
                    activity.getString(R.string.location_accuracy_value, "Accuracy unavailable"),
                    activity.findViewById<TextView>(R.id.locationAccuracyText).text.toString()
                )
                assertEquals(
                    activity.getString(R.string.location_captured_at_value, "No location captured"),
                    activity.findViewById<TextView>(R.id.locationCapturedAtText).text.toString()
                )
                val miniMap = activity.findViewById<LocationMiniMapView>(R.id.locationMiniMapView)
                assertNotNull(miniMap)
                assertEquals(0, miniMap.pointCount)
            }
        }
    }

    @Test
    fun dashboardSeparatesCurrentForegroundLatestExternalAndCollectionTime() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val timezoneId = ZoneId.systemDefault()
        val now = System.currentTimeMillis()
        Thread {
            val database = MonitorDatabase.getInstance(context)
            database.clearAllTables()
            database.focusSessionDao().insert(
                focusSession(
                    clientSessionId = "dashboard-runtime-chrome",
                    packageName = "com.android.chrome",
                    startedAtUtcMillis = now - 900_000L,
                    endedAtUtcMillis = now - 420_000L,
                    timezoneId = timezoneId
                )
            )
            database.focusSessionDao().insert(
                focusSession(
                    clientSessionId = "dashboard-runtime-monitor",
                    packageName = "com.woong.monitorstack",
                    startedAtUtcMillis = now - 120_000L,
                    endedAtUtcMillis = now,
                    timezoneId = timezoneId
                )
            )
        }.also { it.start(); it.join() }

        ActivityScenario.launch(DashboardActivity::class.java).use { scenario ->
            waitForDashboardRender()
            scenario.onActivity { activity ->
                assertEquals(
                    "Current foreground app",
                    activity.findViewById<TextView>(R.id.currentForegroundLabel).text.toString()
                )
                assertEquals(
                    "Woong Monitor",
                    activity.findViewById<TextView>(R.id.currentAppText).text.toString()
                )
                assertEquals(
                    "com.woong.monitorstack",
                    activity.findViewById<TextView>(R.id.currentPackageText).text.toString()
                )
                assertEquals(
                    "Latest collected external app",
                    activity.findViewById<TextView>(R.id.latestCollectedExternalLabel).text.toString()
                )
                assertEquals(
                    "Chrome",
                    activity.findViewById<TextView>(R.id.latestCollectedExternalAppText).text.toString()
                )
                assertEquals(
                    "com.android.chrome",
                    activity.findViewById<TextView>(R.id.latestCollectedExternalPackageText).text.toString()
                )
                assertTrue(
                    activity.findViewById<TextView>(R.id.lastCollectedText)
                        .text
                        .toString()
                        .startsWith("Last collection time: ")
                )
                assertTrue(
                    activity.findViewById<TextView>(R.id.currentForegroundEvidenceText)
                        .text
                        .toString()
                        .contains("cannot prove a live external foreground")
                )
            }
        }
    }

    private fun waitForDashboardRender() {
        Thread.sleep(500)
        shadowOf(Looper.getMainLooper()).idle()
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
            localDate = Instant.ofEpochMilli(startedAtUtcMillis)
                .atZone(timezoneId)
                .toLocalDate()
                .toString(),
            timezoneId = timezoneId.id,
            isIdle = false,
            source = "test"
        )
    }
}
