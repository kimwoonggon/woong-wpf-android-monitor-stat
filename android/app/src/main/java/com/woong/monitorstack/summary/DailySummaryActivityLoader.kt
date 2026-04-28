package com.woong.monitorstack.summary

import java.time.LocalDate

class DailySummaryActivityLoader(
    private val repositoryFactory: (String) -> DailySummaryRepository = { DailySummaryClient(it) },
    private val today: () -> LocalDate = { LocalDate.now() }
) {
    fun loadPreviousDay(request: DailySummaryActivityLoadRequest): DailySummaryUiState {
        val viewModel = DailySummaryViewModel(
            repository = repositoryFactory(request.baseUrl),
            today = today
        )
        viewModel.loadPreviousDay(
            userId = request.userId,
            timezoneId = request.timezoneId
        )

        return viewModel.state
    }
}

data class DailySummaryActivityLoadRequest(
    val userId: String,
    val baseUrl: String,
    val timezoneId: String
) {
    init {
        require(userId.isNotBlank()) { "userId must not be blank." }
        require(baseUrl.isNotBlank()) { "baseUrl must not be blank." }
        require(timezoneId.isNotBlank()) { "timezoneId must not be blank." }
    }
}
