package com.woong.monitorstack.dashboard

class DashboardViewModel(
    private val repository: DashboardRepository
) {
    var state: DashboardUiState = DashboardUiState()
        private set

    fun selectPeriod(period: DashboardPeriod) {
        val snapshot = repository.load(period)
        state = DashboardUiState(
            selectedPeriod = period,
            totalActiveMs = snapshot.totalActiveMs,
            topAppPackageName = snapshot.topAppPackageName,
            idleMs = snapshot.idleMs,
            recentSessions = snapshot.recentSessions
        )
    }
}
