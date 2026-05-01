package com.woong.monitorstack.dashboard

import com.github.mikephil.charting.data.BarEntry
import com.github.mikephil.charting.data.Entry
import com.github.mikephil.charting.data.PieEntry

data class DashboardActivityBucket(
    val hourOfDay: Int,
    val durationMs: Long
) {
    init {
        require(hourOfDay in 0..23) { "hourOfDay must be between 0 and 23." }
        require(durationMs >= 0) { "durationMs must not be negative." }
    }
}

data class DashboardUsageSlice(
    val label: String,
    val durationMs: Long
) {
    init {
        require(label.isNotBlank()) { "label must not be blank." }
        require(durationMs >= 0) { "durationMs must not be negative." }
    }
}

data class DashboardDailyActivityBucket(
    val localDate: String,
    val durationMs: Long
) {
    init {
        require(localDate.isNotBlank()) { "localDate must not be blank." }
        require(durationMs >= 0) { "durationMs must not be negative." }
    }
}

data class DashboardChartData(
    val hourlyActivity: List<DashboardActivityBucket> = emptyList(),
    val appUsage: List<DashboardUsageSlice> = emptyList(),
    val domainUsage: List<DashboardUsageSlice> = emptyList(),
    val dailyActivity: List<DashboardDailyActivityBucket> = emptyList()
)

data class DashboardChartEntries(
    val activityEntries: List<Entry>,
    val appEntries: List<BarEntry>,
    val appLabels: List<String>,
    val domainEntries: List<PieEntry>
)
