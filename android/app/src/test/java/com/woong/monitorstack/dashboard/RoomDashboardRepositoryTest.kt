package com.woong.monitorstack.dashboard

import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.LocationCaptureMode
import com.woong.monitorstack.data.local.LocationContextSnapshotEntity
import com.woong.monitorstack.data.local.LocationPermissionState
import com.woong.monitorstack.data.local.LocationVisitEntity
import com.woong.monitorstack.data.local.MonitorDatabase
import java.time.Instant
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.ZoneId
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
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
        assertEquals("Chrome", snapshot.topAppName)
        assertEquals("Chrome", snapshot.recentSessions.first().appName)
        assertEquals("com.android.chrome", snapshot.recentSessions.first().packageName)
        assertEquals("11:00", snapshot.recentSessions.first().startedAtLocalText)
        assertEquals("15m", snapshot.recentSessions.first().durationText)
        assertEquals(30 * 60_000L, snapshot.chartData.hourlyActivity.single { it.hourOfDay == 9 }.durationMs)
        assertEquals(15 * 60_000L, snapshot.chartData.hourlyActivity.single { it.hourOfDay == 11 }.durationMs)
        assertEquals("Chrome", snapshot.chartData.appUsage.single().label)
        assertEquals(45 * 60_000L, snapshot.chartData.appUsage.single().durationMs)
    }

    @Test
    fun loadLastHourUsesUtcWindowAndExcludesOlderSameDayRows() {
        val dao = database.focusSessionDao()
        dao.insert(session("chrome-inside", "com.android.chrome", "2026-04-28T11:20:00", 20, false))
        dao.insert(session("slack-older", "com.slack", "2026-04-28T10:00:00", 30, false))
        val now = LocalDateTime.parse("2026-04-28T12:00:00")
            .atZone(timezoneId)
            .toInstant()
        val repository = RoomDashboardRepository(
            dao = dao,
            timezoneId = timezoneId,
            todayProvider = { LocalDate.of(2026, 4, 28) },
            nowProvider = { now }
        )

        val snapshot = repository.load(DashboardPeriod.LastHour)

        assertEquals(20 * 60_000L, snapshot.totalActiveMs)
        assertEquals("Chrome", snapshot.topAppName)
        assertEquals(listOf("Chrome"), snapshot.recentSessions.map { it.appName })
        assertEquals(
            listOf(11),
            snapshot.chartData.hourlyActivity.map { it.hourOfDay }
        )
    }

    @Test
    fun loadLastHourIncludesAndClipsSessionThatStartedBeforeTheSelectedRange() {
        val dao = database.focusSessionDao()
        dao.insert(session("chrome-spanning", "com.android.chrome", "2026-04-28T10:30:00", 90, false))
        val now = LocalDateTime.parse("2026-04-28T12:00:00")
            .atZone(timezoneId)
            .toInstant()
        val repository = RoomDashboardRepository(
            dao = dao,
            timezoneId = timezoneId,
            todayProvider = { LocalDate.of(2026, 4, 28) },
            nowProvider = { now }
        )

        val snapshot = repository.load(DashboardPeriod.LastHour)

        assertEquals(60 * 60_000L, snapshot.totalActiveMs)
        assertEquals("Chrome", snapshot.topAppName)
        assertEquals("1h 0m", snapshot.recentSessions.single().durationText)
    }

    @Test
    fun loadTodayShowsLatestOptInLocationContextFromRoom() {
        val locationDao = database.locationContextSnapshotDao()
        locationDao.insert(
            locationSnapshot(
                id = "old-location",
                capturedLocal = "2026-04-28T08:15:00",
                latitude = 35.1796,
                longitude = 129.0756
            )
        )
        locationDao.insert(
            locationSnapshot(
                id = "latest-location",
                capturedLocal = "2026-04-28T09:30:00",
                latitude = 37.5665,
                longitude = 126.9780
            )
        )
        val repository = RoomDashboardRepository(
            dao = database.focusSessionDao(),
            locationDao = locationDao,
            deviceId = "android-device-1",
            timezoneId = timezoneId,
            todayProvider = { LocalDate.of(2026, 4, 28) },
            nowProvider = {
                LocalDateTime.parse("2026-04-28T09:45:00")
                    .atZone(timezoneId)
                    .toInstant()
            }
        )

        val snapshot = repository.load(DashboardPeriod.Today)

        assertEquals("Location context enabled", snapshot.locationContext.statusText)
        assertEquals("37.5665", snapshot.locationContext.latitudeText)
        assertEquals("126.9780", snapshot.locationContext.longitudeText)
        assertEquals("±36m", snapshot.locationContext.accuracyText)
        assertEquals("09:30", snapshot.locationContext.capturedAtLocalText)
        assertEquals(
            "A current coordinate should still draw one local map point even before visit aggregation has produced a location_visit row.",
            1,
            snapshot.locationContext.mapPoints.size
        )
        assertEquals(37.5665, snapshot.locationContext.mapPoints.single().latitude, 0.0001)
        assertEquals(126.9780, snapshot.locationContext.mapPoints.single().longitude, 0.0001)
        assertEquals("09:30", snapshot.locationContext.mapPoints.single().capturedAtLocalText)
    }

    @Test
    fun loadTodayMarksLocationContextStaleWhenLatestCaptureIsNotCurrent() {
        val locationDao = database.locationContextSnapshotDao()
        locationDao.insert(
            locationSnapshot(
                id = "stale-location",
                capturedLocal = "2026-04-28T09:30:00",
                latitude = 37.5665,
                longitude = 126.9780
            )
        )
        val repository = RoomDashboardRepository(
            dao = database.focusSessionDao(),
            locationDao = locationDao,
            deviceId = "android-device-1",
            timezoneId = timezoneId,
            todayProvider = { LocalDate.of(2026, 4, 28) },
            nowProvider = {
                LocalDateTime.parse("2026-04-28T11:30:00")
                    .atZone(timezoneId)
                    .toInstant()
            }
        )

        val snapshot = repository.load(DashboardPeriod.Today)

        assertEquals("Location context stale - last captured 2h 0m ago", snapshot.locationContext.statusText)
        assertEquals("09:30", snapshot.locationContext.capturedAtLocalText)
        assertEquals("37.5665", snapshot.locationContext.latitudeText)
        assertEquals("126.9780", snapshot.locationContext.longitudeText)
    }

    @Test
    fun loadTodayShowsLocationVisitStatisticsFromRoom() {
        val visitDao = database.locationVisitDao()
        visitDao.insert(
            locationVisit(
                id = "office",
                locationKey = "37.5665,126.9780",
                firstLocal = "2026-04-28T09:00:00",
                durationMinutes = 45,
                sampleCount = 3
            )
        )
        visitDao.insert(
            locationVisit(
                id = "cafe",
                locationKey = "37.5700,126.9820",
                firstLocal = "2026-04-28T14:00:00",
                durationMinutes = 15,
                sampleCount = 2
            )
        )
        val repository = RoomDashboardRepository(
            dao = database.focusSessionDao(),
            locationVisitDao = visitDao,
            deviceId = "android-device-1",
            timezoneId = timezoneId,
            todayProvider = { LocalDate.of(2026, 4, 28) }
        )

        val snapshot = repository.load(DashboardPeriod.Today)

        assertEquals("2 location visits", snapshot.locationContext.visitStatsText)
        assertEquals("37.5665, 126.9780 - 45m", snapshot.locationContext.topVisitText)
        assertEquals(2, snapshot.locationContext.mapPoints.size)
        assertEquals(37.5665, snapshot.locationContext.mapPoints[0].latitude, 0.0001)
        assertEquals(126.9780, snapshot.locationContext.mapPoints[0].longitude, 0.0001)
        assertEquals(45 * 60_000L, snapshot.locationContext.mapPoints[0].durationMs)
        assertEquals(3, snapshot.locationContext.mapPoints[0].sampleCount)
        assertEquals("09:45", snapshot.locationContext.mapPoints[0].capturedAtLocalText)
    }

    @Test
    fun loadTodayFormatsLocationPointTimesAsAsiaSeoulHourMinuteFromUtcInstants() {
        val visitDao = database.locationVisitDao()
        val firstCapturedAtUtcMillis = Instant.parse("2026-04-28T07:03:00Z").toEpochMilli()
        val lastCapturedAtUtcMillis = Instant.parse("2026-04-28T07:48:00Z").toEpochMilli()
        visitDao.insert(
            LocationVisitEntity(
                id = "seoul-time-visit",
                deviceId = "android-device-1",
                locationKey = "37.5665,126.9780",
                latitude = 37.5665,
                longitude = 126.9780,
                coordinatePrecisionDecimals = 4,
                firstCapturedAtUtcMillis = firstCapturedAtUtcMillis,
                lastCapturedAtUtcMillis = lastCapturedAtUtcMillis,
                durationMs = 45 * 60_000L,
                sampleCount = 3,
                accuracyMeters = 25.0f,
                permissionState = LocationPermissionState.GrantedPrecise,
                captureMode = LocationCaptureMode.AppUsageContext,
                createdAtUtcMillis = firstCapturedAtUtcMillis,
                updatedAtUtcMillis = lastCapturedAtUtcMillis
            )
        )
        val repository = RoomDashboardRepository(
            dao = database.focusSessionDao(),
            locationVisitDao = visitDao,
            deviceId = "android-device-1",
            timezoneId = timezoneId,
            todayProvider = { LocalDate.of(2026, 4, 28) }
        )

        val label = repository.load(DashboardPeriod.Today)
            .locationContext
            .mapPoints
            .single()
            .capturedAtLocalText

        assertEquals("16:48", label)
        assertFalse("Map labels must use HH:mm, not dotted time text.", label.contains("."))
    }

    @Test
    fun loadRecent7DaysBuildsDailyActivityBuckets() {
        val dao = database.focusSessionDao()
        dao.insert(session("chrome-day-1", "com.android.chrome", "2026-04-27T09:00:00", 30, false))
        dao.insert(session("slack-day-2", "com.slack", "2026-04-28T10:00:00", 15, false))
        dao.insert(session("idle-day-2", "com.slack", "2026-04-28T11:00:00", 99, true))
        dao.insert(session("too-old", "com.video", "2026-04-20T09:00:00", 60, false))
        val repository = RoomDashboardRepository(
            dao = dao,
            timezoneId = timezoneId,
            todayProvider = { LocalDate.of(2026, 4, 28) }
        )

        val snapshot = repository.load(DashboardPeriod.Recent7Days)

        assertEquals(
            listOf("2026-04-27", "2026-04-28"),
            snapshot.chartData.dailyActivity.map { it.localDate }
        )
        assertEquals(
            listOf(30 * 60_000L, 15 * 60_000L),
            snapshot.chartData.dailyActivity.map { it.durationMs }
        )
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

    private fun locationSnapshot(
        id: String,
        capturedLocal: String,
        latitude: Double?,
        longitude: Double?
    ): LocationContextSnapshotEntity {
        val capturedAtUtcMillis = LocalDateTime.parse(capturedLocal)
            .atZone(timezoneId)
            .toInstant()
            .toEpochMilli()

        return LocationContextSnapshotEntity(
            id = id,
            deviceId = "android-device-1",
            capturedAtUtcMillis = capturedAtUtcMillis,
            latitude = latitude,
            longitude = longitude,
            accuracyMeters = 35.5f,
            permissionState = LocationPermissionState.GrantedApproximate,
            captureMode = LocationCaptureMode.AppUsageContext,
            createdAtUtcMillis = capturedAtUtcMillis
        )
    }

    private fun locationVisit(
        id: String,
        locationKey: String,
        firstLocal: String,
        durationMinutes: Long,
        sampleCount: Int
    ): LocationVisitEntity {
        val firstCapturedAtUtcMillis = LocalDateTime.parse(firstLocal)
            .atZone(timezoneId)
            .toInstant()
            .toEpochMilli()
        val durationMs = durationMinutes * 60_000L
        val lastCapturedAtUtcMillis = firstCapturedAtUtcMillis + durationMs
        val latitude = locationKey.substringBefore(",").toDouble()
        val longitude = locationKey.substringAfter(",").toDouble()

        return LocationVisitEntity(
            id = id,
            deviceId = "android-device-1",
            locationKey = locationKey,
            latitude = latitude,
            longitude = longitude,
            coordinatePrecisionDecimals = 4,
            firstCapturedAtUtcMillis = firstCapturedAtUtcMillis,
            lastCapturedAtUtcMillis = lastCapturedAtUtcMillis,
            durationMs = durationMs,
            sampleCount = sampleCount,
            accuracyMeters = 25.0f,
            permissionState = LocationPermissionState.GrantedPrecise,
            captureMode = LocationCaptureMode.AppUsageContext,
            createdAtUtcMillis = firstCapturedAtUtcMillis,
            updatedAtUtcMillis = lastCapturedAtUtcMillis
        )
    }
}
