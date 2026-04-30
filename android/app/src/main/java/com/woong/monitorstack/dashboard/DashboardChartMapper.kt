package com.woong.monitorstack.dashboard

import com.github.mikephil.charting.data.BarEntry
import com.github.mikephil.charting.data.Entry
import com.github.mikephil.charting.data.PieEntry

class DashboardChartMapper {
    fun map(data: DashboardChartData): DashboardChartEntries {
        return DashboardChartEntries(
            activityEntries = data.hourlyActivity.map { bucket ->
                Entry(bucket.hourOfDay.toFloat(), bucket.durationMs.toMinutesFloat())
            },
            appEntries = data.appUsage.mapIndexed { index, slice ->
                BarEntry(index.toFloat(), slice.durationMs.toMinutesFloat())
            },
            appLabels = data.appUsage.map { it.label },
            domainEntries = data.domainUsage.map { slice ->
                PieEntry(slice.durationMs.toMinutesFloat(), slice.label)
            }
        )
    }

    private fun Long.toMinutesFloat(): Float = this / 60_000f
}
