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
import org.json.JSONArray
import org.json.JSONObject
import org.junit.Assert.assertEquals
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
        val evidenceOutput = File(outputDir, "dashboard-location-map-evidence.json")

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

        val evidence = buildLocationMapEvidence(database, mapScreenshotFileName = output.name)
        writeJson(evidenceOutput, evidence)

        assertEquals("PASS", evidence.getString("status"))
        assertEquals(2, evidence.getInt("visitCount"))
        assertEquals("37.5665,126.9780", evidence.getString("topVisitLocationKey"))
        assertTrue(
            "Location map evidence must stay metadata-only and exclude typed/page content fields.",
            evidence.getBoolean("metadataOnly")
        )
    }

    private fun buildLocationMapEvidence(
        database: MonitorDatabase,
        mapScreenshotFileName: String
    ): JSONObject {
        val visits = database.locationVisitDao().queryByRange(
            deviceId = RoomDashboardRepository.DefaultDeviceId,
            fromUtcMillis = 0L,
            toUtcMillis = Long.MAX_VALUE
        )
        val topVisit = visits.maxWithOrNull(
            compareBy<LocationVisitEntity> { it.durationMs }
                .thenBy { it.sampleCount }
                .thenByDescending { it.lastCapturedAtUtcMillis }
        )
        val visitsJson = JSONArray(visits.map { it.toMetadataJson() })
        val metadataOnlyJson = JSONObject()
            .put("visits", visitsJson)
            .put("topVisit", topVisit?.toMetadataJson() ?: JSONObject.NULL)
        val metadataOnly = metadataOnlyJson.toString().containsOnlyMetadataFields()
        val passed = visits.size == 2 &&
            topVisit?.locationKey == "37.5665,126.9780" &&
            metadataOnly

        return JSONObject()
            .put("status", if (passed) "PASS" else "FAIL")
            .put("privacy", PrivacyBoundary)
            .put("deviceId", RoomDashboardRepository.DefaultDeviceId)
            .put("visitCount", visits.size)
            .put("topVisitLocationKey", topVisit?.locationKey ?: "")
            .put("topVisitDurationMs", topVisit?.durationMs ?: 0L)
            .put("topVisitSampleCount", topVisit?.sampleCount ?: 0)
            .put("metadataOnly", metadataOnly)
            .put("mapScreenshot", mapScreenshotFileName)
            .put("visits", visitsJson)
    }

    private fun LocationVisitEntity.toMetadataJson(): JSONObject {
        return JSONObject()
            .put("id", id)
            .put("deviceId", deviceId)
            .put("locationKey", locationKey)
            .put("latitude", latitude)
            .put("longitude", longitude)
            .put("coordinatePrecisionDecimals", coordinatePrecisionDecimals)
            .put("firstCapturedAtUtcMillis", firstCapturedAtUtcMillis)
            .put("lastCapturedAtUtcMillis", lastCapturedAtUtcMillis)
            .put("durationMs", durationMs)
            .put("sampleCount", sampleCount)
            .put("accuracyMeters", accuracyMeters)
            .put("permissionState", permissionState.name)
            .put("captureMode", captureMode.name)
    }

    private fun writeJson(file: File, value: JSONObject) {
        file.parentFile?.mkdirs()
        file.writeText(value.toString(2))
    }

    private fun String.containsOnlyMetadataFields(): Boolean {
        val lower = lowercase()
        return ForbiddenEvidenceFragments.none { fragment -> fragment in lower }
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

    companion object {
        private const val PrivacyBoundary =
            "No typed text, page content, browser content, clipboard, passwords, messages, or touch coordinates are collected."
        private val ForbiddenEvidenceFragments = listOf(
            "windowtitle",
            "window_title",
            "pagetitle",
            "page_title",
            "url",
            "domain",
            "pagecontent",
            "page_content",
            "contenttext",
            "content_text",
            "typedtext",
            "typed_text",
            "clipboard",
            "password",
            "messagebody",
            "message_body",
            "forminput",
            "form_input",
            "touchcoordinate",
            "touch_coordinate"
        )
    }
}
