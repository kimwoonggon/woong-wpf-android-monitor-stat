package com.woong.monitorstack.dashboard

import com.woong.monitorstack.data.local.FocusSessionDao
import com.woong.monitorstack.data.local.FocusSessionEntity
import java.time.Instant
import java.time.LocalDate
import java.time.ZoneId
import java.time.format.DateTimeFormatter

class RoomDashboardRepository(
    private val dao: FocusSessionDao,
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
            topAppPackageName = activeSessions.topPackageName(),
            idleMs = sessions.filter { it.isIdle }.sumOf { it.durationMs },
            recentSessions = sessions.toRecentRows(),
            chartData = activeSessions.toChartData()
        )
    }

    private fun DashboardPeriod.toLocalDateRange(today: LocalDate): Pair<LocalDate, LocalDate> {
        return when (this) {
            DashboardPeriod.Today -> today to today
            DashboardPeriod.Yesterday -> today.minusDays(1) to today.minusDays(1)
            DashboardPeriod.Recent7Days -> today.minusDays(6) to today
        }
    }

    private fun List<FocusSessionEntity>.topPackageName(): String? {
        return groupBy { it.packageName }
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
            appUsage = groupBy { it.packageName }
                .map { entry ->
                    DashboardUsageSlice(
                        label = entry.key,
                        durationMs = entry.value.sumOf { it.durationMs }
                    )
                }
                .sortedWith(
                    compareByDescending<DashboardUsageSlice> { it.durationMs }
                        .thenBy { it.label }
                )
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
        private val TimeFormatter: DateTimeFormatter = DateTimeFormatter.ofPattern("HH:mm")
    }
}
