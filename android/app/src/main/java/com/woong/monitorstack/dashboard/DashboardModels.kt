package com.woong.monitorstack.dashboard

data class DashboardSessionRow(
    val appName: String,
    val packageName: String,
    val startedAtLocalText: String,
    val durationText: String,
    val durationMs: Long = 0L
)

data class DashboardSnapshot(
    val totalActiveMs: Long,
    val topAppName: String? = null,
    val idleMs: Long,
    val recentSessions: List<DashboardSessionRow>,
    val chartData: DashboardChartData = DashboardChartData(),
    val locationContext: DashboardLocationContext = DashboardLocationContext(),
    val topAppPackageName: String? = topAppName
)

data class DashboardUiState(
    val selectedPeriod: DashboardPeriod? = null,
    val totalActiveMs: Long = 0,
    val topAppName: String? = null,
    val idleMs: Long = 0,
    val recentSessions: List<DashboardSessionRow> = emptyList(),
    val chartData: DashboardChartData = DashboardChartData(),
    val locationContext: DashboardLocationContext = DashboardLocationContext(),
    val topAppPackageName: String? = topAppName
)

data class DashboardLocationContext(
    val statusText: String = "Location capture off",
    val latitudeText: String = "Latitude not stored",
    val longitudeText: String = "Longitude not stored",
    val accuracyText: String = "Accuracy unavailable",
    val capturedAtLocalText: String = "No location captured",
    val visitStatsText: String = "No location visits",
    val topVisitText: String = "No location statistics",
    val mapPoints: List<LocationMapPoint> = emptyList()
)

data class LocationMapPoint(
    val latitude: Double,
    val longitude: Double,
    val durationMs: Long,
    val sampleCount: Int
)
