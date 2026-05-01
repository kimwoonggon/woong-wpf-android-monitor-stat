package com.woong.monitorstack.summary

import com.woong.monitorstack.data.local.FocusSessionDao
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.display.AppDisplayNameFormatter
import java.time.LocalDate

class RoomReportRepository(
    private val dao: FocusSessionDao,
    private val todayProvider: () -> LocalDate = { LocalDate.now() }
) {
    fun load(period: ReportPeriod): ReportSnapshot {
        val today = todayProvider()
        val (from, to) = period.dateRange(today)
        val sessions = dao.queryByLocalDateRange(from.toString(), to.toString())
            .filterNot { it.isIdle }

        return ReportSnapshot(
            totalActiveMs = sessions.sumOf { it.durationMs },
            dayCount = period.dayCount,
            dateRangeText = "${from} - ${to}",
            topAppName = sessions.topAppName(),
            dailyActivity = sessions.dailyActivity(),
            topApps = sessions.topApps()
        )
    }

    private fun ReportPeriod.dateRange(today: LocalDate): Pair<LocalDate, LocalDate> {
        return when (this) {
            ReportPeriod.Last7Days -> today.minusDays(6) to today
            ReportPeriod.Last30Days -> today.minusDays(29) to today
            ReportPeriod.Last90Days -> today.minusDays(89) to today
            is ReportPeriod.Custom -> from to to
        }
    }

    private fun List<FocusSessionEntity>.topAppName(): String? {
        return topApps().firstOrNull()?.appName
    }

    private fun List<FocusSessionEntity>.dailyActivity(): List<ReportDailyActivity> {
        return groupBy { it.localDate }
            .map { entry ->
                ReportDailyActivity(
                    localDate = entry.key,
                    durationMs = entry.value.sumOf { it.durationMs }
                )
            }
            .sortedBy { it.localDate }
    }

    private fun List<FocusSessionEntity>.topApps(): List<ReportTopApp> {
        return groupBy { AppDisplayNameFormatter.format(it.packageName) }
            .map { entry ->
                ReportTopApp(
                    appName = entry.key,
                    durationMs = entry.value.sumOf { it.durationMs }
                )
            }
            .sortedWith(
                compareByDescending<ReportTopApp> { it.durationMs }
                    .thenBy { it.appName }
            )
    }
}

data class ReportSnapshot(
    val totalActiveMs: Long,
    val dayCount: Int,
    val dateRangeText: String,
    val topAppName: String?,
    val dailyActivity: List<ReportDailyActivity>,
    val topApps: List<ReportTopApp>
)

data class ReportDailyActivity(
    val localDate: String,
    val durationMs: Long
)

data class ReportTopApp(
    val appName: String,
    val durationMs: Long
)
