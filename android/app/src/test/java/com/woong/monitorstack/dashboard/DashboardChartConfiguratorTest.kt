package com.woong.monitorstack.dashboard

import android.content.Context
import android.view.ContextThemeWrapper
import androidx.core.content.ContextCompat
import androidx.test.core.app.ApplicationProvider
import com.github.mikephil.charting.charts.BarChart
import com.github.mikephil.charting.charts.LineChart
import com.github.mikephil.charting.components.XAxis
import com.github.mikephil.charting.data.BarEntry
import com.github.mikephil.charting.data.Entry
import com.woong.monitorstack.R
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class DashboardChartConfiguratorTest {
    @Test
    fun chartConfiguratorsApplyReadableSharedVisualStyle() {
        val context = testContext()
        val chart = BarChart(context)

        DashboardChartConfigurator().configureHourlyBarChart(chart)

        val mutedText = ContextCompat.getColor(context, R.color.wms_text_muted)
        val border = ContextCompat.getColor(context, R.color.wms_border)

        assertFalse(chart.description.isEnabled)
        assertFalse(chart.legend.isEnabled)
        assertFalse(chart.isHighlightPerTapEnabled)
        assertFalse(chart.isHighlightPerDragEnabled)
        assertEquals(mutedText, chart.xAxis.textColor)
        assertEquals(mutedText, chart.axisLeft.textColor)
        assertEquals(border, chart.xAxis.gridColor)
        assertEquals(border, chart.axisLeft.gridColor)
    }

    @Test
    fun focusBarDataSetUsesBrandColorAndHidesRawValueLabels() {
        val context = testContext()
        val dataSet = DashboardChartConfigurator().createFocusBarDataSet(
            context = context,
            entries = listOf(BarEntry(9f, 30f)),
            label = "Hourly focus"
        )

        assertEquals(ContextCompat.getColor(context, R.color.wms_primary), dataSet.color)
        assertFalse(dataSet.isDrawValuesEnabled)
    }

    @Test
    fun trendLineDataSetUsesBrandColorAndHidesRawValueLabels() {
        val context = testContext()
        val dataSet = DashboardChartConfigurator().createTrendLineDataSet(
            context = context,
            entries = listOf(Entry(0f, 120f)),
            label = "Daily trend"
        )

        assertEquals(ContextCompat.getColor(context, R.color.wms_primary), dataSet.color)
        assertEquals(ContextCompat.getColor(context, R.color.wms_primary), dataSet.circleColors.first())
        assertFalse(dataSet.isDrawValuesEnabled)
    }

    @Test
    fun hourlyChartUsesHourLabelsAndMinuteAxis() {
        val chart = LineChart(testContext())

        DashboardChartConfigurator().configureHourlyChart(chart)

        assertEquals(XAxis.XAxisPosition.BOTTOM, chart.xAxis.position)
        assertEquals("09", chart.xAxis.valueFormatter.getFormattedValue(9f))
        assertEquals("10", chart.xAxis.valueFormatter.getFormattedValue(10f))
        assertEquals("", chart.xAxis.valueFormatter.getFormattedValue(9.3f))
        assertEquals("60m", chart.axisLeft.valueFormatter.getFormattedValue(60f))
        assertEquals("", chart.axisLeft.valueFormatter.getFormattedValue(-0.5f))
        assertFalse(chart.axisRight.isEnabled)
    }

    @Test
    fun appUsageChartUsesAppLabelsAndMinuteAxis() {
        val chart = BarChart(testContext())

        DashboardChartConfigurator().configureAppUsageChart(
            chart = chart,
            appLabels = listOf("Chrome", "YouTube")
        )

        assertEquals(XAxis.XAxisPosition.BOTTOM, chart.xAxis.position)
        assertEquals("Chrome", chart.xAxis.valueFormatter.getFormattedValue(0f))
        assertEquals("YouTube", chart.xAxis.valueFormatter.getFormattedValue(1f))
        assertEquals("", chart.xAxis.valueFormatter.getFormattedValue(-0.5f))
        assertEquals("45m", chart.axisLeft.valueFormatter.getFormattedValue(45f))
        assertEquals("", chart.axisLeft.valueFormatter.getFormattedValue(-0.5f))
        assertFalse(chart.axisRight.isEnabled)
    }

    @Test
    fun hourlyBarChartUsesHourLabelsAndMinuteAxis() {
        val chart = BarChart(testContext())

        DashboardChartConfigurator().configureHourlyBarChart(chart)

        assertEquals(XAxis.XAxisPosition.BOTTOM, chart.xAxis.position)
        assertEquals("09", chart.xAxis.valueFormatter.getFormattedValue(9f))
        assertEquals("18", chart.xAxis.valueFormatter.getFormattedValue(18f))
        assertEquals("30m", chart.axisLeft.valueFormatter.getFormattedValue(30f))
        assertFalse(chart.axisRight.isEnabled)
    }

    @Test
    fun dailyTrendChartUsesDayLabelsAndMinuteAxis() {
        val chart = LineChart(testContext())

        DashboardChartConfigurator().configureDailyTrendChart(
            chart = chart,
            dayLabels = listOf("04/27", "04/28")
        )

        assertEquals(XAxis.XAxisPosition.BOTTOM, chart.xAxis.position)
        assertEquals("04/27", chart.xAxis.valueFormatter.getFormattedValue(0f))
        assertEquals("04/28", chart.xAxis.valueFormatter.getFormattedValue(1f))
        assertEquals("", chart.xAxis.valueFormatter.getFormattedValue(1.5f))
        assertEquals("90m", chart.axisLeft.valueFormatter.getFormattedValue(90f))
        assertFalse(chart.axisRight.isEnabled)
    }

    private fun testContext(): Context {
        return ContextThemeWrapper(
            ApplicationProvider.getApplicationContext(),
            R.style.Theme_WoongMonitor
        )
    }
}
