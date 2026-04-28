package com.woong.monitorstack.dashboard

data class DashboardSessionRow(
    val packageName: String,
    val startedAtLocalText: String,
    val durationText: String
)

data class DashboardSnapshot(
    val totalActiveMs: Long,
    val topAppPackageName: String?,
    val idleMs: Long,
    val recentSessions: List<DashboardSessionRow>
)

data class DashboardUiState(
    val selectedPeriod: DashboardPeriod? = null,
    val totalActiveMs: Long = 0,
    val topAppPackageName: String? = null,
    val idleMs: Long = 0,
    val recentSessions: List<DashboardSessionRow> = emptyList()
)
