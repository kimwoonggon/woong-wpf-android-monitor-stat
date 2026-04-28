package com.woong.monitorstack.summary

import java.time.LocalDate
import org.junit.Assert.assertEquals
import org.junit.Test

class DailySummaryActivityLoaderTest {
    @Test
    fun loadPreviousDayUsesRepositoryFactoryAndFormatsState() {
        val repository = FakeDailySummaryRepository(
            response = DailySummaryResponse(
                summaryDate = "2026-04-27",
                totalActiveMs = 900_000,
                totalIdleMs = 120_000,
                totalWebMs = 240_000,
                topApps = listOf(UsageTotalResponse("Chrome", 600_000)),
                topDomains = listOf(UsageTotalResponse("example.com", 240_000))
            )
        )
        var baseUrl: String? = null
        val loader = DailySummaryActivityLoader(
            repositoryFactory = {
                baseUrl = it
                repository
            },
            today = { LocalDate.of(2026, 4, 28) }
        )

        val state = loader.loadPreviousDay(
            DailySummaryActivityLoadRequest(
                userId = "user-1",
                baseUrl = "https://server.example",
                timezoneId = "Asia/Seoul"
            )
        )

        assertEquals("https://server.example", baseUrl)
        assertEquals("user-1", repository.userId)
        assertEquals("2026-04-27", repository.summaryDate)
        assertEquals("Asia/Seoul", repository.timezoneId)
        assertEquals("2026-04-27", state.summaryDateText)
        assertEquals("15m", state.activeTimeText)
        assertEquals("2m", state.idleTimeText)
        assertEquals("4m", state.webTimeText)
        assertEquals("Chrome", state.topAppText)
        assertEquals("example.com", state.topDomainText)
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
