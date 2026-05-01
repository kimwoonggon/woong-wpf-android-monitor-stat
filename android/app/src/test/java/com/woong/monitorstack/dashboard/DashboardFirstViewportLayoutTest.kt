package com.woong.monitorstack.dashboard

import android.content.Context
import android.view.ContextThemeWrapper
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.R
import com.woong.monitorstack.databinding.FragmentDashboardBinding
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

    private fun ViewGroup.indexOfChild(child: View): Int {
        return (0 until childCount).firstOrNull { index -> getChildAt(index) == child } ?: -1
    }
}
