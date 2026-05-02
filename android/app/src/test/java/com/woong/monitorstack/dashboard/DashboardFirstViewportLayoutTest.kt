package com.woong.monitorstack.dashboard

import android.content.Context
import android.view.ContextThemeWrapper
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.R
import com.woong.monitorstack.databinding.FragmentDashboardBinding
import kotlin.math.roundToInt
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class DashboardFirstViewportLayoutTest {
    @Test
    fun dashboardPrioritizesHourlyChartAndTopAppsBeforeLocationContext() {
        val context = ContextThemeWrapper(
            ApplicationProvider.getApplicationContext<Context>(),
            R.style.Theme_WoongMonitor
        )
        val binding = FragmentDashboardBinding.inflate(LayoutInflater.from(context))

        val content = binding.dashboardContent
        val periodIndex = content.indexOfChild(binding.periodFilterScroll)
        val chartIndex = content.indexOfChild(binding.hourlyFocusChartCard)
        val topAppsIndex = content.indexOfChild(binding.topAppsCard)
        val locationIndex = content.indexOfChild(binding.locationContextCard)

        assertTrue("Period filters should appear before dashboard analytics", periodIndex < chartIndex)
        assertTrue("Hourly chart should appear before optional location context", chartIndex < locationIndex)
        assertTrue("Top apps should appear before optional location context", topAppsIndex < locationIndex)
    }

    @Test
    fun dashboardHourlyChartUsesCompactFirstViewportCard() {
        val context = ContextThemeWrapper(
            ApplicationProvider.getApplicationContext<Context>(),
            R.style.Theme_WoongMonitor
        )
        val binding = FragmentDashboardBinding.inflate(LayoutInflater.from(context))
        val density = context.resources.displayMetrics.density

        assertTrue(
            "Hourly chart card should stay compact enough to leave a visible Top apps hint.",
            binding.hourlyFocusChartCard.layoutParams.height <= 176.dp(density)
        )
        assertTrue(
            "Top apps should follow the chart tightly for first-viewport readability.",
            (binding.topAppsCard.layoutParams as ViewGroup.MarginLayoutParams).topMargin <= 10.dp(density)
        )
        assertEquals(
            "Chart view should fill its compact card instead of forcing extra fixed height.",
            0,
            binding.hourlyFocusChart.layoutParams.height
        )
    }

    @Test
    fun dashboardKeepsRecentSessionsAheadOfOptionalLocationContext() {
        val context = ContextThemeWrapper(
            ApplicationProvider.getApplicationContext<Context>(),
            R.style.Theme_WoongMonitor
        )
        val binding = FragmentDashboardBinding.inflate(LayoutInflater.from(context))
        val density = context.resources.displayMetrics.density
        val content = binding.dashboardContent

        val topAppsIndex = content.indexOfChild(binding.topAppsCard)
        val recentSessionsIndex = content.indexOfChild(binding.recentSessionsCard)
        val locationIndex = content.indexOfChild(binding.locationContextCard)

        assertTrue(
            "Recent sessions should follow core app-usage analytics before optional location context.",
            topAppsIndex < recentSessionsIndex
        )
        assertTrue(
            "Recent sessions should not be pushed below the optional location context.",
            recentSessionsIndex < locationIndex
        )
        assertTrue(
            "Recent sessions need bottom spacing so rows are not clipped by the shell navigation.",
            (binding.recentSessionsCard.layoutParams as ViewGroup.MarginLayoutParams)
                .bottomMargin >= 32.dp(density)
        )
    }

    private fun ViewGroup.indexOfChild(child: View): Int {
        return (0 until childCount).firstOrNull { index -> getChildAt(index) == child } ?: -1
    }

    private fun Int.dp(density: Float): Int = (this * density).roundToInt()
}
