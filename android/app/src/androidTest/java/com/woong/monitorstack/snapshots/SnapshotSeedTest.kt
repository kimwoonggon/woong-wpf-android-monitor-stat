package com.woong.monitorstack.snapshots

import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.LocationCaptureMode
import com.woong.monitorstack.data.local.LocationContextSnapshotEntity
import com.woong.monitorstack.data.local.LocationPermissionState
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.dashboard.RoomDashboardRepository
import java.time.ZoneId
import java.time.ZonedDateTime
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class SnapshotSeedTest {
    @Test
    fun seedDeterministicUsageSessionsForLocalScreenshots() {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val database = MonitorDatabase.getInstance(context)
        val zone = ZoneId.systemDefault()
        val now = ZonedDateTime.now(zone)
        val today = now.toLocalDate()
        val earliestToday = today.atStartOfDay(zone).plusMinutes(5)
        val candidateBase = now.minusHours(5)
        val base = if (candidateBase.isBefore(earliestToday)) {
            earliestToday
        } else {
            candidateBase
        }

        database.clearAllTables()
        database.focusSessionDao().insertAll(
            listOf(
                focusSession(
                    clientSessionId = "snapshot-chrome",
                    packageName = "com.android.chrome",
                    startedAt = base,
                    durationMinutes = 60,
                    isIdle = false
                ),
                focusSession(
                    clientSessionId = "snapshot-youtube",
                    packageName = "com.google.android.youtube",
                    startedAt = base.plusHours(1),
                    durationMinutes = 45,
                    isIdle = false
                ),
                focusSession(
                    clientSessionId = "snapshot-slack",
                    packageName = "com.slack",
                    startedAt = base.plusHours(2),
                    durationMinutes = 15,
                    isIdle = false
                ),
                focusSession(
                    clientSessionId = "snapshot-report-day-minus-1-chrome",
                    packageName = "com.android.chrome",
                    startedAt = base.minusDays(1).plusHours(1),
                    durationMinutes = 35,
                    isIdle = false
                ),
                focusSession(
                    clientSessionId = "snapshot-report-day-minus-2-youtube",
                    packageName = "com.google.android.youtube",
                    startedAt = base.minusDays(2).plusHours(2),
                    durationMinutes = 50,
                    isIdle = false
                ),
                focusSession(
                    clientSessionId = "snapshot-report-day-minus-3-slack",
                    packageName = "com.slack",
                    startedAt = base.minusDays(3).plusHours(3),
                    durationMinutes = 25,
                    isIdle = false
                ),
                focusSession(
                    clientSessionId = "snapshot-report-day-minus-4-chrome",
                    packageName = "com.android.chrome",
                    startedAt = base.minusDays(4).plusHours(4),
                    durationMinutes = 40,
                    isIdle = false
                ),
                focusSession(
                    clientSessionId = "snapshot-idle",
                    packageName = "com.android.chrome",
                    startedAt = base.plusHours(3),
                    durationMinutes = 10,
                    isIdle = true
                )
            )
        )
        database.locationContextSnapshotDao().insert(
            LocationContextSnapshotEntity(
                id = "snapshot-location-context",
                deviceId = RoomDashboardRepository.DefaultDeviceId,
                capturedAtUtcMillis = base.plusMinutes(30).toInstant().toEpochMilli(),
                latitude = 37.5665,
                longitude = 126.9780,
                accuracyMeters = 35.5f,
                permissionState = LocationPermissionState.GrantedApproximate,
                captureMode = LocationCaptureMode.AppUsageContext,
                createdAtUtcMillis = base.plusMinutes(30).toInstant().toEpochMilli()
            )
        )

        val reportRows = database.focusSessionDao()
            .queryByLocalDateRange(today.minusDays(6).toString(), today.toString())
            .filterNot { it.isIdle }
        val reportBucketCount = reportRows
            .map { it.localDate }
            .distinct()
            .size

        assertTrue(
            "Expected snapshot seed to create at least five report localDate buckets.",
            reportBucketCount >= 5
        )
    }

    private fun focusSession(
        clientSessionId: String,
        packageName: String,
        startedAt: ZonedDateTime,
        durationMinutes: Long,
        isIdle: Boolean
    ): FocusSessionEntity {
        val startedAtUtcMillis = startedAt.toInstant().toEpochMilli()
        val durationMs = durationMinutes * 60_000

        return FocusSessionEntity(
            clientSessionId = clientSessionId,
            packageName = packageName,
            startedAtUtcMillis = startedAtUtcMillis,
            endedAtUtcMillis = startedAtUtcMillis + durationMs,
            durationMs = durationMs,
            localDate = startedAt.toLocalDate().toString(),
            timezoneId = startedAt.zone.id,
            isIdle = isIdle,
            source = "snapshot_seed"
        )
    }
}
