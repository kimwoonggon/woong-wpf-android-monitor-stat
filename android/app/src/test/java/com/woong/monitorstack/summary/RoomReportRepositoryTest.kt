package com.woong.monitorstack.summary

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
class RoomReportRepositoryTest {
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
    fun loadThirtyDaysExcludesOlderRowsAndBuildsDailyBuckets() {
        val dao = database.focusSessionDao()
        dao.insert(session("today-chrome", "com.android.chrome", "2026-04-30T09:00:00", 10, false))
        dao.insert(session("day-20-youtube", "com.google.android.youtube", "2026-04-10T09:00:00", 20, false))
        dao.insert(session("day-20-idle", "com.slack", "2026-04-10T10:00:00", 99, true))
        dao.insert(session("too-old", "com.slack", "2026-03-30T09:00:00", 60, false))
        val repository = RoomReportRepository(
            dao = dao,
            timezoneId = timezoneId,
            todayProvider = { LocalDate.of(2026, 4, 30) }
        )

        val snapshot = repository.load(ReportPeriod.Last30Days)

        assertEquals(30 * 60_000L, snapshot.totalActiveMs)
        assertEquals(30, snapshot.dayCount)
        assertEquals("2026-04-01 - 2026-04-30", snapshot.dateRangeText)
        assertEquals("YouTube", snapshot.topAppName)
        assertEquals(
            listOf("2026-04-10", "2026-04-30"),
            snapshot.dailyActivity.map { it.localDate }
        )
        assertEquals(
            listOf(20 * 60_000L, 10 * 60_000L),
            snapshot.dailyActivity.map { it.durationMs }
        )
        assertEquals(
            listOf("YouTube", "Chrome"),
            snapshot.topApps.map { it.appName }
        )
    }

    @Test
    fun loadNinetyDaysAggregatesActiveSessionsOnly() {
        val dao = database.focusSessionDao()
        dao.insert(session("today-chrome", "com.android.chrome", "2026-04-30T09:00:00", 10, false))
        dao.insert(session("day-80-youtube", "com.google.android.youtube", "2026-02-09T09:00:00", 40, false))
        dao.insert(session("day-80-idle", "com.google.android.youtube", "2026-02-09T10:00:00", 120, true))
        dao.insert(session("too-old", "com.slack", "2026-01-29T09:00:00", 90, false))
        val repository = RoomReportRepository(
            dao = dao,
            timezoneId = timezoneId,
            todayProvider = { LocalDate.of(2026, 4, 30) }
        )

        val snapshot = repository.load(ReportPeriod.Last90Days)

        assertEquals(50 * 60_000L, snapshot.totalActiveMs)
        assertEquals(90, snapshot.dayCount)
        assertEquals("2026-01-31 - 2026-04-30", snapshot.dateRangeText)
        assertEquals("YouTube", snapshot.topAppName)
        assertEquals(
            listOf("2026-02-09", "2026-04-30"),
            snapshot.dailyActivity.map { it.localDate }
        )
        assertEquals(
            listOf(40 * 60_000L, 10 * 60_000L),
            snapshot.dailyActivity.map { it.durationMs }
        )
    }

    @Test
    fun loadCustomRangeAggregatesOnlySessionsInsideRequestedDates() {
        val dao = database.focusSessionDao()
        dao.insert(session("before-range", "com.android.chrome", "2026-04-10T09:00:00", 60, false))
        dao.insert(session("inside-youtube", "com.google.android.youtube", "2026-04-20T09:00:00", 40, false))
        dao.insert(session("inside-chrome", "com.android.chrome", "2026-04-25T09:00:00", 15, false))
        dao.insert(session("after-range", "com.slack", "2026-04-26T09:00:00", 90, false))
        val repository = RoomReportRepository(
            dao = dao,
            timezoneId = timezoneId,
            todayProvider = { LocalDate.of(2026, 4, 30) }
        )

        val snapshot = repository.load(
            ReportPeriod.Custom(
                from = LocalDate.of(2026, 4, 15),
                to = LocalDate.of(2026, 4, 25)
            )
        )

        assertEquals(55 * 60_000L, snapshot.totalActiveMs)
        assertEquals(11, snapshot.dayCount)
        assertEquals("2026-04-15 - 2026-04-25", snapshot.dateRangeText)
        assertEquals("YouTube", snapshot.topAppName)
        assertEquals(
            listOf("2026-04-20", "2026-04-25"),
            snapshot.dailyActivity.map { it.localDate }
        )
    }

    @Test
    fun loadCustomRangeIncludesAndClipsSessionsThatOverlapTheRequestedDates() {
        val dao = database.focusSessionDao()
        dao.insert(session("spanning-chrome", "com.android.chrome", "2026-04-14T23:30:00", 90, false))
        val repository = RoomReportRepository(
            dao = dao,
            timezoneId = timezoneId,
            todayProvider = { LocalDate.of(2026, 4, 30) }
        )

        val snapshot = repository.load(
            ReportPeriod.Custom(
                from = LocalDate.of(2026, 4, 15),
                to = LocalDate.of(2026, 4, 15)
            )
        )

        assertEquals(60 * 60_000L, snapshot.totalActiveMs)
        assertEquals(listOf("2026-04-15"), snapshot.dailyActivity.map { it.localDate })
        assertEquals(listOf(60 * 60_000L), snapshot.dailyActivity.map { it.durationMs })
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

        return FocusSessionEntity(
            clientSessionId = id,
            packageName = packageName,
            startedAtUtcMillis = startedAtUtcMillis,
            endedAtUtcMillis = startedAtUtcMillis + durationMs,
            durationMs = durationMs,
            localDate = Instant.ofEpochMilli(startedAtUtcMillis)
                .atZone(timezoneId)
                .toLocalDate()
                .toString(),
            timezoneId = timezoneId.id,
            isIdle = isIdle,
            source = "report_test"
        )
    }
}
