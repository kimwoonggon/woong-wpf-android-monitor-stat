package com.woong.monitorstack.sessions

import android.content.Context
import android.view.ContextThemeWrapper
import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.R
import com.woong.monitorstack.databinding.FragmentAppDetailBinding
import kotlin.math.roundToInt
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class AppDetailFirstViewportLayoutTest {
    @Test
    fun appDetailKeepsChartCompactAndSessionListVisibleInFirstViewport() {
        val context = ContextThemeWrapper(
            ApplicationProvider.getApplicationContext<Context>(),
            R.style.Theme_WoongMonitor
        )
        val binding = FragmentAppDetailBinding.inflate(LayoutInflater.from(context))
        val density = context.resources.displayMetrics.density

        assertTrue(
            "Selected app header should stay compact enough to make room for details.",
            (binding.appDetailIdentityRow.layoutParams as ViewGroup.MarginLayoutParams)
                .topMargin <= 12.dp(density)
        )
        assertTrue(
            "Selected app icon should be readable without consuming first-viewport height.",
            binding.appDetailIconPlaceholder.layoutParams.height <= 48.dp(density)
        )
        assertTrue(
            "Hourly app chart should be compact enough for the first session row to be visible.",
            binding.appHourlyChartCard.layoutParams.height <= 176.dp(density)
        )
        assertTrue(
            "Session list should follow the chart tightly in the first viewport.",
            (binding.appDetailSessionsCard.layoutParams as ViewGroup.MarginLayoutParams)
                .topMargin <= 10.dp(density)
        )
        assertEquals(
            "Session rows should remain part of the page flow instead of a nested clipped scroller.",
            ViewGroup.LayoutParams.WRAP_CONTENT,
            binding.appDetailSessionsRecyclerView.layoutParams.height
        )
    }

    private fun Int.dp(density: Float): Int = (this * density).roundToInt()
}
