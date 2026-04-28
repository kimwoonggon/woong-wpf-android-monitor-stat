package com.woong.monitorstack.summary

import java.time.LocalDate

class DailySummaryViewModel(
    private val repository: DailySummaryRepository,
    private val today: () -> LocalDate = { LocalDate.now() }
) {
    var state: DailySummaryUiState = DailySummaryUiState()
        private set

    fun loadPreviousDay(
        userId: String,
        timezoneId: String
    ) {
        val summaryDate = today().minusDays(1).toString()
        val summary = repository.getDailySummary(
            userId = userId,
            summaryDate = summaryDate,
            timezoneId = timezoneId
        )

        state = DailySummaryUiState(
            summaryDateText = summary.summaryDate,
            activeTimeText = formatDuration(summary.totalActiveMs),
            idleTimeText = formatDuration(summary.totalIdleMs),
            webTimeText = formatDuration(summary.totalWebMs),
            topAppText = summary.topApps.firstOrNull()?.key ?: NoTopValue,
            topDomainText = summary.topDomains.firstOrNull()?.key ?: NoTopValue
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
        private const val NoTopValue = "None"
    }
}

data class DailySummaryUiState(
    val summaryDateText: String = "",
    val activeTimeText: String = "0m",
    val idleTimeText: String = "0m",
    val webTimeText: String = "0m",
    val topAppText: String = "None",
    val topDomainText: String = "None"
)
