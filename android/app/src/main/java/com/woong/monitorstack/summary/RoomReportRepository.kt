package com.woong.monitorstack.summary

import com.woong.monitorstack.data.local.FocusSessionDao
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.display.AppDisplayNameFormatter
import java.time.Instant
import java.time.LocalDate
import java.time.ZoneId

class RoomReportRepository(
    private val dao: FocusSessionDao,
    private val timezoneId: ZoneId = ZoneId.systemDefault(),
    private val todayProvider: () -> LocalDate = { LocalDate.now() }
) {
    fun load(period: ReportPeriod): ReportSnapshot {
        val today = todayProvider()
        val (from, to) = period.dateRange(today)
        val range = ReportUtcRange(
            from = from.atStartOfDay(timezoneId).toInstant(),
            to = to.plusDays(1).atStartOfDay(timezoneId).toInstant()
        )
        val sessions = dao.queryByUtcOverlap(
            fromUtcMillis = range.from.toEpochMilli(),
            toUtcMillis = range.to.toEpochMilli()
        )
            .filterNot { it.isIdle }
            .map {
                val startedAtUtcMillis = maxOf(it.startedAtUtcMillis, range.from.toEpochMilli())
                val endedAtUtcMillis = minOf(it.endedAtUtcMillis, range.to.toEpochMilli())
                FilteredReportSession(
                    entity = it,
                    startedAtUtcMillis = startedAtUtcMillis,
                    endedAtUtcMillis = endedAtUtcMillis,
                    durationMs = (endedAtUtcMillis - startedAtUtcMillis).coerceAtLeast(0L)
                )
            }
            .filter { it.durationMs > 0 }

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

    private fun FocusSessionEntity.durationWithin(range: ReportUtcRange): Long {
        val from = maxOf(startedAtUtcMillis, range.from.toEpochMilli())
        val to = minOf(endedAtUtcMillis, range.to.toEpochMilli())
        return (to - from).coerceAtLeast(0L)
    }

    private fun List<FilteredReportSession>.topAppName(): String? {
        return topApps().firstOrNull()?.appName
    }

    private fun List<FilteredReportSession>.dailyActivity(): List<ReportDailyActivity> {
        return flatMap { it.splitIntoLocalDaySlices(timezoneId) }
            .groupBy { it.localDate }
            .map { entry ->
                ReportDailyActivity(
                    localDate = entry.key,
                    durationMs = entry.value.sumOf { it.durationMs }
                )
            }
            .sortedBy { it.localDate }
    }

    private fun FilteredReportSession.splitIntoLocalDaySlices(
        zoneId: ZoneId
    ): List<ReportDailySlice> {
        val slices = mutableListOf<ReportDailySlice>()
        var cursor = Instant.ofEpochMilli(startedAtUtcMillis)
        val end = Instant.ofEpochMilli(endedAtUtcMillis)

        while (cursor.isBefore(end)) {
            val localDate = cursor.atZone(zoneId).toLocalDate()
            val nextDayStart = localDate.plusDays(1).atStartOfDay(zoneId).toInstant()
            val sliceEnd = minOf(nextDayStart, end)
            val durationMs = sliceEnd.toEpochMilli() - cursor.toEpochMilli()
            if (durationMs > 0L) {
                slices += ReportDailySlice(
                    localDate = localDate.toString(),
                    durationMs = durationMs
                )
            }
            cursor = sliceEnd
        }

        return slices
    }

    private fun List<FilteredReportSession>.topApps(): List<ReportTopApp> {
        return groupBy { AppDisplayNameFormatter.format(it.entity.packageName) }
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

private data class ReportUtcRange(
    val from: Instant,
    val to: Instant
)

private data class FilteredReportSession(
    val entity: FocusSessionEntity,
    val startedAtUtcMillis: Long,
    val endedAtUtcMillis: Long,
    val durationMs: Long
)

private data class ReportDailySlice(
    val localDate: String,
    val durationMs: Long
)
