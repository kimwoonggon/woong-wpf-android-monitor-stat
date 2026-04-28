package com.woong.monitorstack.summary

interface DailySummaryRepository {
    fun getDailySummary(
        userId: String,
        summaryDate: String,
        timezoneId: String
    ): DailySummaryResponse
}
