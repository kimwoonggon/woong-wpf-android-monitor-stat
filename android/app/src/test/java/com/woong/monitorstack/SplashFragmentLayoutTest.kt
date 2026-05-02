package com.woong.monitorstack

import android.content.Context
import android.view.ContextThemeWrapper
import android.view.LayoutInflater
import android.widget.ProgressBar
import android.widget.TextView
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.databinding.FragmentSplashBinding
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class SplashFragmentLayoutTest {
    @Test
    fun splashLayoutMatchesRequiredFigmaHierarchy() {
        val context = ContextThemeWrapper(
            ApplicationProvider.getApplicationContext<Context>(),
            R.style.Theme_WoongMonitor
        )
        val binding = FragmentSplashBinding.inflate(LayoutInflater.from(context))

        assertEquals(32.dp(context), binding.splashRoot.paddingStart)
        assertEquals(48.dp(context), binding.splashRoot.paddingTop)
        assertEquals(82.dp(context), binding.splashLogoContainer.layoutParams.width)
        assertEquals(82.dp(context), binding.splashLogoContainer.layoutParams.height)
        assertNotNull(binding.splashLogoBars.drawable)
        assertText(binding.appTitleText, context.getString(R.string.app_name))
        assertText(binding.appSubtitleText, context.getString(R.string.android_focus_tracker))
        assertTrue(binding.loadingIndicator is ProgressBar)
        assertText(binding.loadingText, context.getString(R.string.loading_korean))
    }

    private fun assertText(view: TextView, expected: String) {
        assertEquals(expected, view.text.toString())
    }

    private fun Int.dp(context: Context): Int {
        return (this * context.resources.displayMetrics.density).toInt()
    }
}
