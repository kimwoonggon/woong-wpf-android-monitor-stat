package com.woong.monitorstack.dashboard

import com.woong.monitorstack.data.local.FocusSessionDao
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.LocationCaptureMode
import com.woong.monitorstack.data.local.LocationContextSnapshotDao
import com.woong.monitorstack.data.local.LocationContextSnapshotEntity
import com.woong.monitorstack.data.local.LocationPermissionState
import com.woong.monitorstack.data.local.LocationVisitDao
import com.woong.monitorstack.data.local.LocationVisitEntity
import com.woong.monitorstack.display.AppDisplayNameFormatter
import java.time.Instant
import java.time.LocalDate
import java.time.LocalTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.time.temporal.ChronoUnit
import java.util.Locale
import kotlin.math.roundToInt

class RoomDashboardRepository(
    private val dao: FocusSessionDao,
    private val locationDao: LocationContextSnapshotDao? = null,
    private val locationVisitDao: LocationVisitDao? = null,
    private val deviceId: String = DefaultDeviceId,
    private val timezoneId: ZoneId = ZoneId.systemDefault(),
    private val todayProvider: () -> LocalDate = { LocalDate.now(timezoneId) },
    private val nowProvider: () -> Instant = { Instant.now() }
) : DashboardRepository {
    override fun load(period: DashboardPeriod): DashboardSnapshot {
        val today = todayProvider()
        val range = period.toUtcRange(today, nowProvider(), timezoneId)
        val dateRange = range.toLocalDateRange(timezoneId)
        val sessions = dao.queryByLocalDateRange(
            dateRange.first.toString(),
            dateRange.second.toString()
        )
            .filter { it.overlaps(range) }
            .map { FilteredDashboardSession(it, it.durationWithin(range)) }
        val activeSessions = sessions.filterNot { it.entity.isIdle }

        return DashboardSnapshot(
            totalActiveMs = activeSessions.sumOf { it.durationMs },
            topAppName = activeSessions.topAppName(),
            idleMs = sessions.filter { it.entity.isIdle }.sumOf { it.durationMs },
            recentSessions = sessions.toRecentRows(),
            chartData = activeSessions.toChartData(),
            locationContext = loadLocationContext(dateRange)
        )
    }

    private fun DashboardPeriod.toUtcRange(
        today: LocalDate,
        now: Instant,
        zoneId: ZoneId
    ): DashboardUtcRange {
        fun startOfDay(date: LocalDate): Instant = date.atStartOfDay(zoneId).toInstant()

        return when (this) {
            DashboardPeriod.Today -> DashboardUtcRange(
                from = startOfDay(today),
                to = startOfDay(today.plusDays(1))
            )
            DashboardPeriod.Yesterday -> DashboardUtcRange(
                from = startOfDay(today.minusDays(1)),
                to = startOfDay(today)
            )
            DashboardPeriod.LastHour -> DashboardUtcRange(
                from = now.minusSeconds(60 * 60),
                to = now
            )
            DashboardPeriod.LastSixHours -> DashboardUtcRange(
                from = now.minusSeconds(6 * 60 * 60),
                to = now
            )
            DashboardPeriod.LastTwentyFourHours -> DashboardUtcRange(
                from = now.minusSeconds(24 * 60 * 60),
                to = now
            )
            DashboardPeriod.Recent7Days -> DashboardUtcRange(
                from = startOfDay(today.minusDays(6)),
                to = startOfDay(today.plusDays(1))
            )
        }
    }

    private fun DashboardUtcRange.toLocalDateRange(zoneId: ZoneId): Pair<LocalDate, LocalDate> {
        return from.atZone(zoneId).toLocalDate() to
            to.minusMillis(1).atZone(zoneId).toLocalDate()
    }

    private fun FocusSessionEntity.overlaps(range: DashboardUtcRange): Boolean {
        return endedAtUtcMillis > range.from.toEpochMilli() &&
            startedAtUtcMillis < range.to.toEpochMilli()
    }

    private fun FocusSessionEntity.durationWithin(range: DashboardUtcRange): Long {
        val from = maxOf(startedAtUtcMillis, range.from.toEpochMilli())
        val to = minOf(endedAtUtcMillis, range.to.toEpochMilli())
        return (to - from).coerceAtLeast(0L)
    }

    private fun List<FilteredDashboardSession>.topAppName(): String? {
        return groupBy { AppDisplayNameFormatter.format(it.entity.packageName) }
            .mapValues { entry -> entry.value.sumOf { it.durationMs } }
            .entries
            .sortedWith(
                compareByDescending<Map.Entry<String, Long>> { it.value }
                    .thenBy { it.key }
            )
            .firstOrNull()
            ?.key
    }

    private fun List<FilteredDashboardSession>.toRecentRows(): List<DashboardSessionRow> {
        return sortedByDescending { it.entity.startedAtUtcMillis }
            .take(10)
            .map { filteredSession ->
                val session = filteredSession.entity
                DashboardSessionRow(
                    appName = AppDisplayNameFormatter.format(session.packageName),
                    packageName = session.packageName,
                    startedAtLocalText = Instant.ofEpochMilli(session.startedAtUtcMillis)
                        .atZone(timezoneId)
                        .format(TimeFormatter),
                    durationText = formatDuration(filteredSession.durationMs),
                    durationMs = filteredSession.durationMs
                )
            }
    }

    private fun List<FilteredDashboardSession>.toChartData(): DashboardChartData {
        return DashboardChartData(
            hourlyActivity = groupBy { session ->
                Instant.ofEpochMilli(session.entity.startedAtUtcMillis)
                    .atZone(timezoneId)
                    .hour
            }
                .map { entry ->
                    DashboardActivityBucket(
                        hourOfDay = entry.key,
                        durationMs = entry.value.sumOf { it.durationMs }
                    )
                }
                .sortedBy { it.hourOfDay },
            appUsage = groupBy { AppDisplayNameFormatter.format(it.entity.packageName) }
                .map { entry ->
                    DashboardUsageSlice(
                        label = entry.key,
                        durationMs = entry.value.sumOf { it.durationMs }
                    )
                }
                .sortedWith(
                    compareByDescending<DashboardUsageSlice> { it.durationMs }
                        .thenBy { it.label }
                ),
            dailyActivity = groupBy { it.entity.localDate }
                .map { entry ->
                    DashboardDailyActivityBucket(
                        localDate = entry.key,
                        durationMs = entry.value.sumOf { it.durationMs }
                    )
                }
                .sortedBy { it.localDate }
        )
    }

    private fun loadLocationContext(dateRange: Pair<LocalDate, LocalDate>): DashboardLocationContext {
        val fromUtcMillis = dateRange.first
            .atStartOfDay(timezoneId)
            .toInstant()
            .toEpochMilli()
        val toUtcMillis = dateRange.second
            .atTime(LocalTime.MAX)
            .atZone(timezoneId)
            .toInstant()
            .toEpochMilli()
        val visitSummary = loadLocationVisitSummary(fromUtcMillis, toUtcMillis)
        val latest = locationDao?.queryByCapturedRange(
            deviceId = deviceId,
            fromUtcMillis = fromUtcMillis,
            toUtcMillis = toUtcMillis
        )
            ?.lastOrNull()

        return latest?.toLocationContext(visitSummary)
            ?: DashboardLocationContext(
                visitStatsText = visitSummary.visitStatsText,
                topVisitText = visitSummary.topVisitText,
                mapPoints = visitSummary.mapPoints
            )
    }

    private fun loadLocationVisitSummary(
        fromUtcMillis: Long,
        toUtcMillis: Long
    ): LocationVisitSummary {
        val locationVisitDao = locationVisitDao ?: return LocationVisitSummary()
        val visits = locationVisitDao.queryByRange(
            deviceId = deviceId,
            fromUtcMillis = fromUtcMillis,
            toUtcMillis = toUtcMillis
        )

        if (visits.isEmpty()) {
            return LocationVisitSummary()
        }

        val topVisit = visits.maxWith(
            compareBy<LocationVisitEntity> { it.durationMs }
                .thenBy { it.sampleCount }
                .thenByDescending { it.lastCapturedAtUtcMillis }
        )
        val mapPoints = visits
            .sortedWith(
                compareByDescending<LocationVisitEntity> { it.durationMs }
                    .thenByDescending { it.sampleCount }
                    .thenByDescending { it.lastCapturedAtUtcMillis }
            )
            .take(10)
            .map { visit ->
                LocationMapPoint(
                    latitude = visit.latitude,
                    longitude = visit.longitude,
                    durationMs = visit.durationMs,
                    sampleCount = visit.sampleCount,
                    capturedAtLocalText = Instant.ofEpochMilli(visit.lastCapturedAtUtcMillis)
                        .atZone(timezoneId)
                        .format(TimeFormatter)
                )
            }

        return LocationVisitSummary(
            visitStatsText = "${visits.size} location visits",
            topVisitText = String.format(
                Locale.US,
                "%.4f, %.4f - %s",
                topVisit.latitude,
                topVisit.longitude,
                formatDuration(topVisit.durationMs)
            ),
            mapPoints = mapPoints
        )
    }

    private fun LocationContextSnapshotEntity.toLocationContext(
        visitSummary: LocationVisitSummary
    ): DashboardLocationContext {
        val hasCoordinate = latitude != null && longitude != null
        val canDisplayCoordinate = captureMode == LocationCaptureMode.AppUsageContext
            && permissionState != LocationPermissionState.NotGranted
            && hasCoordinate

        if (!canDisplayCoordinate) {
            return DashboardLocationContext(
                visitStatsText = visitSummary.visitStatsText,
                topVisitText = visitSummary.topVisitText,
                mapPoints = visitSummary.mapPoints
            )
        }

        val capturedAt = Instant.ofEpochMilli(capturedAtUtcMillis)
        val now = nowProvider()
        val statusText = if (isStale(capturedAt, now)) {
            "Location context stale - last captured ${formatElapsed(capturedAt, now)} ago"
        } else {
            "Location context enabled"
        }

        return DashboardLocationContext(
            statusText = statusText,
            latitudeText = String.format(Locale.US, "%.4f", latitude),
            longitudeText = String.format(Locale.US, "%.4f", longitude),
            accuracyText = accuracyMeters?.let { "±${it.roundToInt()}m" } ?: "Accuracy unavailable",
            capturedAtLocalText = capturedAt.atZone(timezoneId).format(TimeFormatter),
            visitStatsText = visitSummary.visitStatsText,
            topVisitText = visitSummary.topVisitText,
            mapPoints = visitSummary.mapPoints.ifEmpty {
                listOf(
                    LocationMapPoint(
                        latitude = latitude!!,
                        longitude = longitude!!,
                        durationMs = 0L,
                        sampleCount = 1,
                        capturedAtLocalText = capturedAt.atZone(timezoneId).format(TimeFormatter)
                    )
                )
            }
        )
    }

    private fun isStale(capturedAt: Instant, now: Instant): Boolean {
        return capturedAt.plusMillis(LocationStaleAfterMs).isBefore(now)
    }

    private fun formatElapsed(from: Instant, to: Instant): String {
        val elapsedMinutes = ChronoUnit.MINUTES.between(from, to).coerceAtLeast(0)
        val hours = elapsedMinutes / 60
        val minutes = elapsedMinutes % 60

        return if (hours > 0) {
            "${hours}h ${minutes}m"
        } else {
            "${minutes}m"
        }
    }

    private fun formatDuration(durationMs: Long): String {
        val totalMinutes = durationMs / 60_000
        val hours = totalMinutes / 60
        val minutes = totalMinutes % 60

        return if (hours > 0) {
            "${hours}h ${minutes}m"
        } else {
            "${minutes}m"
        }
    }

    companion object {
        const val DefaultDeviceId = "local-android-device"
        private const val LocationStaleAfterMs = 60 * 60 * 1_000L
        private val TimeFormatter: DateTimeFormatter = DateTimeFormatter.ofPattern("HH:mm")
    }
}

private data class DashboardUtcRange(
    val from: Instant,
    val to: Instant
)

private data class FilteredDashboardSession(
    val entity: FocusSessionEntity,
    val durationMs: Long
)

private data class LocationVisitSummary(
    val visitStatsText: String = "No location visits",
    val topVisitText: String = "No location statistics",
    val mapPoints: List<LocationMapPoint> = emptyList()
)
