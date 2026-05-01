package com.woong.monitorstack.dashboard

import com.woong.monitorstack.data.local.FocusSessionDao
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.LocationCaptureMode
import com.woong.monitorstack.data.local.LocationContextSnapshotDao
import com.woong.monitorstack.data.local.LocationContextSnapshotEntity
import com.woong.monitorstack.data.local.LocationPermissionState
import com.woong.monitorstack.display.AppDisplayNameFormatter
import java.time.Instant
import java.time.LocalDate
import java.time.LocalTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale
import kotlin.math.roundToInt

class RoomDashboardRepository(
    private val dao: FocusSessionDao,
    private val locationDao: LocationContextSnapshotDao? = null,
    private val deviceId: String = DefaultDeviceId,
    private val timezoneId: ZoneId = ZoneId.systemDefault(),
    private val todayProvider: () -> LocalDate = { LocalDate.now(timezoneId) }
) : DashboardRepository {
    override fun load(period: DashboardPeriod): DashboardSnapshot {
        val today = todayProvider()
        val dateRange = period.toLocalDateRange(today)
        val sessions = dao.queryByLocalDateRange(
            dateRange.first.toString(),
            dateRange.second.toString()
        )
        val activeSessions = sessions.filterNot { it.isIdle }

        return DashboardSnapshot(
            totalActiveMs = activeSessions.sumOf { it.durationMs },
            topAppName = activeSessions.topAppName(),
            idleMs = sessions.filter { it.isIdle }.sumOf { it.durationMs },
            recentSessions = sessions.toRecentRows(),
            chartData = activeSessions.toChartData(),
            locationContext = loadLocationContext(dateRange)
        )
    }

    private fun DashboardPeriod.toLocalDateRange(today: LocalDate): Pair<LocalDate, LocalDate> {
        return when (this) {
            DashboardPeriod.Today -> today to today
            DashboardPeriod.Yesterday -> today.minusDays(1) to today.minusDays(1)
            DashboardPeriod.Recent7Days -> today.minusDays(6) to today
        }
    }

    private fun List<FocusSessionEntity>.topAppName(): String? {
        return groupBy { AppDisplayNameFormatter.format(it.packageName) }
            .mapValues { entry -> entry.value.sumOf { it.durationMs } }
            .entries
            .sortedWith(
                compareByDescending<Map.Entry<String, Long>> { it.value }
                    .thenBy { it.key }
            )
            .firstOrNull()
            ?.key
    }

    private fun List<FocusSessionEntity>.toRecentRows(): List<DashboardSessionRow> {
        return sortedByDescending { it.startedAtUtcMillis }
            .take(10)
            .map { session ->
                DashboardSessionRow(
                    appName = AppDisplayNameFormatter.format(session.packageName),
                    packageName = session.packageName,
                    startedAtLocalText = Instant.ofEpochMilli(session.startedAtUtcMillis)
                        .atZone(timezoneId)
                        .format(TimeFormatter),
                    durationText = formatDuration(session.durationMs)
                )
            }
    }

    private fun List<FocusSessionEntity>.toChartData(): DashboardChartData {
        return DashboardChartData(
            hourlyActivity = groupBy { session ->
                Instant.ofEpochMilli(session.startedAtUtcMillis)
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
            appUsage = groupBy { AppDisplayNameFormatter.format(it.packageName) }
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
            dailyActivity = groupBy { it.localDate }
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
        val locationDao = locationDao ?: return DashboardLocationContext()
        val fromUtcMillis = dateRange.first
            .atStartOfDay(timezoneId)
            .toInstant()
            .toEpochMilli()
        val toUtcMillis = dateRange.second
            .atTime(LocalTime.MAX)
            .atZone(timezoneId)
            .toInstant()
            .toEpochMilli()
        val latest = locationDao.queryByCapturedRange(
            deviceId = deviceId,
            fromUtcMillis = fromUtcMillis,
            toUtcMillis = toUtcMillis
        )
            .lastOrNull()

        return latest?.toLocationContext() ?: DashboardLocationContext()
    }

    private fun LocationContextSnapshotEntity.toLocationContext(): DashboardLocationContext {
        val hasCoordinate = latitude != null && longitude != null
        val canDisplayCoordinate = captureMode == LocationCaptureMode.AppUsageContext
            && permissionState != LocationPermissionState.NotGranted
            && hasCoordinate

        if (!canDisplayCoordinate) {
            return DashboardLocationContext()
        }

        return DashboardLocationContext(
            statusText = "Location context enabled",
            latitudeText = String.format(Locale.US, "%.4f", latitude),
            longitudeText = String.format(Locale.US, "%.4f", longitude),
            accuracyText = accuracyMeters?.let { "±${it.roundToInt()}m" } ?: "Accuracy unavailable",
            capturedAtLocalText = Instant.ofEpochMilli(capturedAtUtcMillis)
                .atZone(timezoneId)
                .format(TimeFormatter)
        )
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
        private val TimeFormatter: DateTimeFormatter = DateTimeFormatter.ofPattern("HH:mm")
    }
}
