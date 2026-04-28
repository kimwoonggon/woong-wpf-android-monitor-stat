package com.woong.monitorstack.summary

import org.junit.Assert.assertEquals
import org.junit.Test
import java.time.LocalDate

class DailySummaryViewModelTest {
    @Test
    fun loadPreviousDayFetchesYesterdayAndFormatsSummaryState() {
        val repository = FakeDailySummaryRepository(
            DailySummaryResponse(
                summaryDate = "2026-04-27",
                totalActiveMs = 900_000,
                totalIdleMs = 120_000,
                totalWebMs = 240_000,
                topApps = listOf(UsageTotalResponse("com.android.chrome", 600_000)),
                topDomains = listOf(UsageTotalResponse("example.com", 240_000))
            )
        )
        val viewModel = DailySummaryViewModel(
            repository = repository,
            today = { LocalDate.of(2026, 4, 28) }
        )

        viewModel.loadPreviousDay(
            userId = "user-1",
            timezoneId = "Asia/Seoul"
        )

        assertEquals("user-1", repository.userId)
        assertEquals("2026-04-27", repository.summaryDate)
        assertEquals("Asia/Seoul", repository.timezoneId)
        assertEquals("2026-04-27", viewModel.state.summaryDateText)
        assertEquals("15m", viewModel.state.activeTimeText)
        assertEquals("2m", viewModel.state.idleTimeText)
        assertEquals("4m", viewModel.state.webTimeText)
        assertEquals("com.android.chrome", viewModel.state.topAppText)
        assertEquals("example.com", viewModel.state.topDomainText)
    }

    private class FakeDailySummaryRepository(
        private val response: DailySummaryResponse
    ) : DailySummaryRepository {
        var userId: String? = null
            private set

        var summaryDate: String? = null
            private set

        var timezoneId: String? = null
            private set

        override fun getDailySummary(
            userId: String,
            summaryDate: String,
            timezoneId: String
        ): DailySummaryResponse {
            this.userId = userId
            this.summaryDate = summaryDate
            this.timezoneId = timezoneId
            return response
        }
    }
}
