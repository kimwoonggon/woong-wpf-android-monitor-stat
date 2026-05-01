package com.woong.monitorstack.dashboard

import android.content.Context
import androidx.core.content.ContextCompat
import com.github.mikephil.charting.charts.BarLineChartBase
import com.github.mikephil.charting.charts.BarChart
import com.github.mikephil.charting.charts.LineChart
import com.github.mikephil.charting.components.XAxis
import com.github.mikephil.charting.data.BarDataSet
import com.github.mikephil.charting.data.BarEntry
import com.github.mikephil.charting.data.Entry
import com.github.mikephil.charting.data.LineDataSet
import com.github.mikephil.charting.formatter.ValueFormatter
import com.woong.monitorstack.R
import kotlin.math.abs
import kotlin.math.roundToInt

class DashboardChartConfigurator {
    fun configureHourlyChart(chart: LineChart) {
        applyReadableVisualStyle(chart)
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
        applyReadableVisualStyle(chart)
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

    fun configureHourlyBarChart(chart: BarChart) {
        applyReadableVisualStyle(chart)
        chart.setNoDataText("No sessions yet")
        chart.axisRight.isEnabled = false
        chart.axisLeft.axisMinimum = 0f
        chart.axisLeft.valueFormatter = MinuteAxisValueFormatter()
        chart.xAxis.position = XAxis.XAxisPosition.BOTTOM
        chart.xAxis.granularity = 1f
        chart.xAxis.isGranularityEnabled = true
        chart.xAxis.valueFormatter = HourOfDayAxisValueFormatter()
        chart.setFitBars(true)
    }

    fun configureDailyTrendChart(chart: LineChart, dayLabels: List<String>) {
        applyReadableVisualStyle(chart)
        chart.setNoDataText("No sessions yet")
        chart.axisRight.isEnabled = false
        chart.axisLeft.axisMinimum = 0f
        chart.axisLeft.valueFormatter = MinuteAxisValueFormatter()
        chart.xAxis.position = XAxis.XAxisPosition.BOTTOM
        chart.xAxis.granularity = 1f
        chart.xAxis.isGranularityEnabled = true
        chart.xAxis.valueFormatter = IndexedLabelAxisValueFormatter(dayLabels)
    }

    fun createFocusBarDataSet(
        context: Context,
        entries: List<BarEntry>,
        label: String
    ): BarDataSet {
        val primary = ContextCompat.getColor(context, R.color.wms_primary)
        return BarDataSet(entries, label).apply {
            color = primary
            valueTextColor = primary
            setDrawValues(false)
            highLightColor = primary
        }
    }

    fun createTrendLineDataSet(
        context: Context,
        entries: List<Entry>,
        label: String
    ): LineDataSet {
        val primary = ContextCompat.getColor(context, R.color.wms_primary)
        val surface = ContextCompat.getColor(context, R.color.wms_surface)
        return LineDataSet(entries, label).apply {
            color = primary
            setCircleColor(primary)
            circleHoleColor = surface
            valueTextColor = primary
            lineWidth = 2.5f
            circleRadius = 4f
            circleHoleRadius = 2f
            setDrawValues(false)
            highLightColor = primary
        }
    }

    private fun applyReadableVisualStyle(chart: BarLineChartBase<*>) {
        val context = chart.context
        val mutedText = ContextCompat.getColor(context, R.color.wms_text_muted)
        val border = ContextCompat.getColor(context, R.color.wms_border)

        chart.description.isEnabled = false
        chart.legend.isEnabled = false
        chart.setTouchEnabled(false)
        chart.setScaleEnabled(false)
        chart.setHighlightPerTapEnabled(false)
        chart.setHighlightPerDragEnabled(false)
        chart.setDrawGridBackground(false)
        chart.setNoDataTextColor(mutedText)

        chart.xAxis.textColor = mutedText
        chart.xAxis.textSize = 11f
        chart.xAxis.gridColor = border
        chart.xAxis.axisLineColor = border

        chart.axisLeft.textColor = mutedText
        chart.axisLeft.textSize = 11f
        chart.axisLeft.gridColor = border
        chart.axisLeft.axisLineColor = border
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
