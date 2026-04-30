package com.woong.monitorstack.dashboard

import com.github.mikephil.charting.charts.BarChart
import com.github.mikephil.charting.charts.LineChart
import com.github.mikephil.charting.components.XAxis
import com.github.mikephil.charting.formatter.ValueFormatter
import kotlin.math.abs
import kotlin.math.roundToInt

class DashboardChartConfigurator {
    fun configureHourlyChart(chart: LineChart) {
        chart.description.isEnabled = false
        chart.setNoDataText("No sessions yet")
        chart.axisRight.isEnabled = false
        chart.axisLeft.axisMinimum = 0f
        chart.axisLeft.valueFormatter = MinuteAxisValueFormatter()
        chart.xAxis.position = XAxis.XAxisPosition.BOTTOM
        chart.xAxis.granularity = 1f
        chart.xAxis.isGranularityEnabled = true
        chart.xAxis.valueFormatter = HourOfDayAxisValueFormatter()
    }

    fun configureAppUsageChart(chart: BarChart, appLabels: List<String> = emptyList()) {
        chart.description.isEnabled = false
        chart.setNoDataText("No sessions yet")
        chart.axisRight.isEnabled = false
        chart.axisLeft.axisMinimum = 0f
        chart.axisLeft.valueFormatter = MinuteAxisValueFormatter()
        chart.xAxis.position = XAxis.XAxisPosition.BOTTOM
        chart.xAxis.granularity = 1f
        chart.xAxis.isGranularityEnabled = true
        chart.xAxis.valueFormatter = IndexedLabelAxisValueFormatter(appLabels)
        chart.setFitBars(true)
    }
}

class HourOfDayAxisValueFormatter : ValueFormatter() {
    override fun getFormattedValue(value: Float): String {
        if (!value.isWholeNumber()) {
            return ""
        }

        val hour = value.roundToInt()
        return if (hour in 0..23) {
            "%02d".format(hour)
        } else {
            ""
        }
    }
}

class MinuteAxisValueFormatter : ValueFormatter() {
    override fun getFormattedValue(value: Float): String {
        if (value < 0f) {
            return ""
        }

        return "${value.roundToInt()}m"
    }
}

class IndexedLabelAxisValueFormatter(
    private val labels: List<String>
) : ValueFormatter() {
    override fun getFormattedValue(value: Float): String {
        if (!value.isWholeNumber()) {
            return ""
        }

        val index = value.roundToInt()
        return labels.getOrNull(index).orEmpty()
    }
}

private fun Float.isWholeNumber(): Boolean {
    return abs(this - roundToInt()) < 0.001f
}
