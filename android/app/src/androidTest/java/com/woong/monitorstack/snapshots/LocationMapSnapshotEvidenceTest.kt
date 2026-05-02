package com.woong.monitorstack.snapshots

import android.content.Context
import androidx.core.widget.NestedScrollView
import androidx.test.core.app.ActivityScenario
import androidx.test.core.app.ApplicationProvider
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.uiautomator.UiDevice
import com.woong.monitorstack.R
import com.woong.monitorstack.dashboard.DashboardActivity
import com.woong.monitorstack.dashboard.RoomDashboardRepository
import com.woong.monitorstack.data.local.LocationCaptureMode
import com.woong.monitorstack.data.local.LocationContextSnapshotEntity
import com.woong.monitorstack.data.local.LocationPermissionState
import com.woong.monitorstack.data.local.LocationVisitEntity
import com.woong.monitorstack.data.local.MonitorDatabase
import java.io.File
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.ZoneId
import java.util.TimeZone
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class LocationMapSnapshotEvidenceTest {
    @Test
    fun captureDashboardLocationMapWithSeoulTimeLabels() {
        TimeZone.setDefault(TimeZone.getTimeZone("Asia/Seoul"))
        val context = ApplicationProvider.getApplicationContext<Context>()
        val database = MonitorDatabase.getInstance(context)
        seedLocationRows(database)

        val device = UiDevice.getInstance(InstrumentationRegistry.getInstrumentation())
        val outputDir = File(requireNotNull(context.getExternalFilesDir(null)), "ui-snapshots-location")
        outputDir.mkdirs()
        val output = File(outputDir, "dashboard-location-map.png")

        ActivityScenario.launch(DashboardActivity::class.java).use { scenario ->
            InstrumentationRegistry.getInstrumentation().waitForIdleSync()
            device.waitForIdle()
            Thread.sleep(1_000)
            scenario.onActivity { activity ->
                val scrollView = activity.findViewById<NestedScrollView>(R.id.dashboardScrollRoot)
                val target = activity.findViewById<android.view.View>(R.id.locationContextCard)
                scrollView.scrollTo(0, target.top.coerceAtLeast(0))
            }
            InstrumentationRegistry.getInstrumentation().waitForIdleSync()
            device.waitForIdle()
            Thread.sleep(500)
            assertTrue("Expected dashboard location screenshot capture to succeed.", device.takeScreenshot(output))
        }

        assertTrue("Expected dashboard location screenshot file to exist: $output", output.isFile)
        assertTrue("Expected dashboard location screenshot to be non-empty: $output", output.length() > 0)
    }

    private fun seedLocationRows(database: MonitorDatabase) {
        val zone = ZoneId.of("Asia/Seoul")
        val today = LocalDate.now(zone)
        val firstCaptured = LocalDateTime.of(today.year, today.month, today.dayOfMonth, 16, 3)
            .atZone(zone)
            .toInstant()
            .toEpochMilli()
        val lastCaptured = LocalDateTime.of(today.year, today.month, today.dayOfMonth, 16, 48)
            .atZone(zone)
            .toInstant()
            .toEpochMilli()

        database.clearAllTables()
        database.locationContextSnapshotDao().insert(
            LocationContextSnapshotEntity(
                id = "location-map-evidence-snapshot",
                deviceId = RoomDashboardRepository.DefaultDeviceId,
                capturedAtUtcMillis = lastCaptured,
                latitude = 37.5665,
                longitude = 126.9780,
                accuracyMeters = 35.5f,
                permissionState = LocationPermissionState.GrantedApproximate,
                captureMode = LocationCaptureMode.AppUsageContext,
                createdAtUtcMillis = lastCaptured
            )
        )
        database.locationVisitDao().insert(
            LocationVisitEntity(
                id = "location-map-evidence-office",
                deviceId = RoomDashboardRepository.DefaultDeviceId,
                locationKey = "37.5665,126.9780",
                latitude = 37.5665,
                longitude = 126.9780,
                coordinatePrecisionDecimals = 4,
                firstCapturedAtUtcMillis = firstCaptured,
                lastCapturedAtUtcMillis = lastCaptured,
                durationMs = 45 * 60_000L,
                sampleCount = 3,
                accuracyMeters = 35.5f,
                permissionState = LocationPermissionState.GrantedApproximate,
                captureMode = LocationCaptureMode.AppUsageContext,
                createdAtUtcMillis = firstCaptured,
                updatedAtUtcMillis = lastCaptured
            )
        )
        database.locationVisitDao().insert(
            LocationVisitEntity(
                id = "location-map-evidence-cafe",
                deviceId = RoomDashboardRepository.DefaultDeviceId,
                locationKey = "37.5700,126.9820",
                latitude = 37.5700,
                longitude = 126.9820,
                coordinatePrecisionDecimals = 4,
                firstCapturedAtUtcMillis = firstCaptured + 60 * 60_000L,
                lastCapturedAtUtcMillis = firstCaptured + 75 * 60_000L,
                durationMs = 15 * 60_000L,
                sampleCount = 2,
                accuracyMeters = 35.5f,
                permissionState = LocationPermissionState.GrantedApproximate,
                captureMode = LocationCaptureMode.AppUsageContext,
                createdAtUtcMillis = firstCaptured + 60 * 60_000L,
                updatedAtUtcMillis = firstCaptured + 75 * 60_000L
            )
        )
    }
}
