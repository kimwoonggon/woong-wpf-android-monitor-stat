package com.woong.monitorstack.dashboard

import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.MonitorDatabase
import java.time.Instant
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.ZoneId
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class RoomDashboardRepositoryTest {
    private val timezoneId = ZoneId.of("Asia/Seoul")
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
    fun loadTodayAggregatesActiveIdleTopAppAndRecentSessions() {
        val dao = database.focusSessionDao()
        dao.insert(session("chrome-1", "com.android.chrome", "2026-04-28T09:00:00", 30, false))
        dao.insert(session("slack-idle", "com.slack", "2026-04-28T10:00:00", 10, true))
        dao.insert(session("chrome-2", "com.android.chrome", "2026-04-28T11:00:00", 15, false))
        dao.insert(session("yesterday", "com.video", "2026-04-27T09:00:00", 60, false))
        val repository = RoomDashboardRepository(
            dao = dao,
            timezoneId = timezoneId,
            todayProvider = { LocalDate.of(2026, 4, 28) }
        )

        val snapshot = repository.load(DashboardPeriod.Today)

        assertEquals(45 * 60_000L, snapshot.totalActiveMs)
        assertEquals(10 * 60_000L, snapshot.idleMs)
        assertEquals("com.android.chrome", snapshot.topAppPackageName)
        assertEquals("com.android.chrome", snapshot.recentSessions.first().packageName)
        assertEquals("11:00", snapshot.recentSessions.first().startedAtLocalText)
        assertEquals("15m", snapshot.recentSessions.first().durationText)
    }

    private fun session(
        id: String,
        packageName: String,
        localStart: String,
        durationMinutes: Long,
        isIdle: Boolean
    ): FocusSessionEntity {
        val startedAtUtcMillis = LocalDateTime.parse(localStart)
            .atZone(timezoneId)
            .toInstant()
            .toEpochMilli()
        val durationMs = durationMinutes * 60_000L
        val endedAtUtcMillis = startedAtUtcMillis + durationMs

        return FocusSessionEntity(
            clientSessionId = id,
            packageName = packageName,
            startedAtUtcMillis = startedAtUtcMillis,
            endedAtUtcMillis = endedAtUtcMillis,
            durationMs = durationMs,
            localDate = Instant.ofEpochMilli(startedAtUtcMillis)
                .atZone(timezoneId)
                .toLocalDate()
                .toString(),
            timezoneId = timezoneId.id,
            isIdle = isIdle,
            source = "usage_stats"
        )
    }
}
