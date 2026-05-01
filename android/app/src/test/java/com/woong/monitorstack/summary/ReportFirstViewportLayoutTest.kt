package com.woong.monitorstack.summary

import android.content.Context
import android.view.ContextThemeWrapper
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.R
import com.woong.monitorstack.databinding.FragmentReportBinding
import kotlin.math.roundToInt
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class ReportFirstViewportLayoutTest {
    @Test
    fun reportKeepsTrendCompactAndTopAppsVisibleInFirstViewport() {
        val context = ContextThemeWrapper(
            ApplicationProvider.getApplicationContext<Context>(),
            R.style.Theme_WoongMonitor
        )
        val binding = FragmentReportBinding.inflate(LayoutInflater.from(context))
        val density = context.resources.displayMetrics.density

        assertTrue(
            "Period filters should stay close to the report heading.",
            (binding.reportPeriodScroll.layoutParams as ViewGroup.MarginLayoutParams)
                .topMargin <= 12.dp(density)
        )
        assertTrue(
            "Date range should not create a large gap before summary cards.",
            (binding.reportDateRangeText.layoutParams as ViewGroup.MarginLayoutParams)
                .topMargin <= 8.dp(density)
        )
        assertEquals(
            "The first viewport should reserve summaries for total and daily cards; top apps belong in the ranked list.",
            View.GONE,
            binding.reportTopAppCard.root.visibility
        )
        assertTrue(
            "Trend chart should stay compact enough for the ranked top-app rows to begin.",
            binding.reportTrendChartCard.layoutParams.height <= 176.dp(density)
        )
        assertTrue(
            "Top apps should follow the trend chart tightly.",
            (binding.reportTopAppsCard.layoutParams as ViewGroup.MarginLayoutParams)
                .topMargin <= 10.dp(density)
        )
        assertEquals(
            "Top app rows should remain in the page flow instead of a nested clipped scroller.",
            ViewGroup.LayoutParams.WRAP_CONTENT,
            binding.reportTopAppsRecyclerView.layoutParams.height
        )
    }

    private fun Int.dp(density: Float): Int = (this * density).roundToInt()
}
