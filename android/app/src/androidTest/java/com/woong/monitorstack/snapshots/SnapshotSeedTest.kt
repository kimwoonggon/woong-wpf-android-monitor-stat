package com.woong.monitorstack.snapshots

import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.MonitorDatabase
import java.time.ZoneId
import java.time.ZonedDateTime
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class SnapshotSeedTest {
    @Test
    fun seedDeterministicUsageSessionsForLocalScreenshots() {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val database = MonitorDatabase.getInstance(context)
        val zone = ZoneId.systemDefault()
        val today = ZonedDateTime.now(zone).toLocalDate()
        val base = today.atTime(9, 0).atZone(zone)

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
                    clientSessionId = "snapshot-idle",
                    packageName = "com.android.chrome",
                    startedAt = base.plusHours(3),
                    durationMinutes = 10,
                    isIdle = true
                )
            )
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
