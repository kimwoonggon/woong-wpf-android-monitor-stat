package com.woong.monitorstack.sessions

import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.MonitorDatabase
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class RoomSessionsRepositoryTest {
    private lateinit var database: MonitorDatabase

    @Before
    fun setUp() {
        database = Room.inMemoryDatabaseBuilder(
            ApplicationProvider.getApplicationContext(),
            MonitorDatabase::class.java
        )
            .allowMainThreadQueries()
            .build()
    }

    @After
    fun tearDown() {
        database.close()
    }

    @Test
    fun loadRecentSessionsReturnsNewestRowsWithReadableDuration() {
        database.focusSessionDao().insert(
            focusSession(
                clientSessionId = "session-1",
                packageName = "com.android.chrome",
                startedAtUtcMillis = 1_000,
                durationMs = 60_000
            )
        )
        database.focusSessionDao().insert(
            focusSession(
                clientSessionId = "session-2",
                packageName = "com.slack",
                startedAtUtcMillis = 5_000,
                durationMs = 125_000
            )
        )
        val repository = RoomSessionsRepository(database.focusSessionDao())

        val rows = repository.loadRecentSessions(limit = 10)

        assertEquals(listOf("Slack", "Chrome"), rows.map { it.appName })
        assertEquals(listOf("com.slack", "com.android.chrome"), rows.map { it.packageName })
        assertEquals("2m 5s", rows.first().durationText)
        assertEquals("09:00 - 09:02", rows.first().timeRangeText)
        assertEquals("Active", rows.first().stateText)
    }

    @Test
    fun loadAppDetailAggregatesSelectedPackageSessions() {
        database.focusSessionDao().insert(
            focusSession(
                clientSessionId = "chrome-1",
                packageName = "com.android.chrome",
                startedAtUtcMillis = 1_000,
                durationMs = 60_000
            )
        )
        database.focusSessionDao().insert(
            focusSession(
                clientSessionId = "chrome-2",
                packageName = "com.android.chrome",
                startedAtUtcMillis = 120_000,
                durationMs = 120_000
            )
        )
        database.focusSessionDao().insert(
            focusSession(
                clientSessionId = "slack-1",
                packageName = "com.slack",
                startedAtUtcMillis = 300_000,
                durationMs = 300_000
            )
        )
        val repository = RoomSessionsRepository(database.focusSessionDao())

        val detail = repository.loadAppDetail("com.android.chrome")

        assertEquals("Chrome", detail.appName)
        assertEquals("com.android.chrome", detail.packageName)
        assertEquals("3m", detail.totalDurationText)
        assertEquals("2 sessions", detail.sessionCountText)
        assertEquals(listOf("2m", "1m"), detail.sessions.map { it.durationText })
        assertEquals(listOf("com.android.chrome", "com.android.chrome"), detail.sessions.map { it.packageName })
    }

    @Test
    fun loadAppDetailBuildsHourlyUsageBucketsForSelectedPackageOnly() {
        database.focusSessionDao().insert(
            focusSession(
                clientSessionId = "chrome-9",
                packageName = "com.android.chrome",
                startedAtUtcMillis = 1_000,
                durationMs = 60_000
            )
        )
        database.focusSessionDao().insert(
            focusSession(
                clientSessionId = "chrome-10",
                packageName = "com.android.chrome",
                startedAtUtcMillis = 3_601_000,
                durationMs = 120_000
            )
        )
        database.focusSessionDao().insert(
            focusSession(
                clientSessionId = "slack-10",
                packageName = "com.slack",
                startedAtUtcMillis = 3_601_000,
                durationMs = 300_000
            )
        )
        val repository = RoomSessionsRepository(database.focusSessionDao())

        val detail = repository.loadAppDetail("com.android.chrome")

        assertEquals(listOf(9, 10), detail.hourlyUsage.map { it.hourOfDay })
        assertEquals(listOf(60_000L, 120_000L), detail.hourlyUsage.map { it.durationMs })
    }

    private fun focusSession(
        clientSessionId: String,
        packageName: String,
        startedAtUtcMillis: Long,
        durationMs: Long
    ): FocusSessionEntity {
        return FocusSessionEntity(
            clientSessionId = clientSessionId,
            packageName = packageName,
            startedAtUtcMillis = startedAtUtcMillis,
            endedAtUtcMillis = startedAtUtcMillis + durationMs,
            durationMs = durationMs,
            localDate = "2026-04-29",
            timezoneId = "Asia/Seoul",
            isIdle = false,
            source = "android_usage_stats"
        )
    }
}
