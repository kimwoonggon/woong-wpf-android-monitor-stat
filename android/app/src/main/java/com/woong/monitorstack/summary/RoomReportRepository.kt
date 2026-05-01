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
        val from = today.minusDays((period.dayCount - 1).toLong())
        val sessions = dao.queryByLocalDateRange(from.toString(), today.toString())
            .filterNot { it.isIdle }

        return ReportSnapshot(
            totalActiveMs = sessions.sumOf { it.durationMs },
            dayCount = period.dayCount,
            dateRangeText = "${from} - ${today}",
            topAppName = sessions.topAppName(),
            dailyActivity = sessions.dailyActivity(),
            topApps = sessions.topApps()
        )
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
