package com.woong.monitorstack.dashboard

import android.content.Context
import android.view.ContextThemeWrapper
import androidx.test.core.app.ApplicationProvider
import com.github.mikephil.charting.charts.BarChart
import com.github.mikephil.charting.charts.LineChart
import com.github.mikephil.charting.components.XAxis
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

    private fun testContext(): Context {
        return ContextThemeWrapper(
            ApplicationProvider.getApplicationContext(),
            R.style.Theme_WoongMonitor
        )
    }
}
