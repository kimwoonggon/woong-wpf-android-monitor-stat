package com.woong.monitorstack.usage

import android.content.Context
import android.view.ContextThemeWrapper
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.R
import com.woong.monitorstack.databinding.FragmentPermissionOnboardingBinding
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class PermissionOnboardingFragmentLayoutTest {
    @Test
    fun permissionOnboardingStatesCollectedMetadataAndExcludedSensitiveData() {
        val context = ContextThemeWrapper(
            ApplicationProvider.getApplicationContext<Context>(),
            R.style.Theme_WoongMonitor
        )
        val binding = FragmentPermissionOnboardingBinding.inflate(LayoutInflater.from(context))
        val screenText = binding.root.visibleText().joinToString(" ")

        listOf("app name", "package name", "start time", "end time", "duration").forEach {
            assertTrue("Permission screen should state it collects $it.", screenText.contains(it))
        }
        listOf("keyboard input", "screen contents", "passwords", "touch coordinates").forEach {
            assertTrue("Permission screen should state it does not collect $it.", screenText.contains(it))
        }
    }

    private fun View.visibleText(): Sequence<String> = sequence {
        if (this@visibleText.visibility != View.VISIBLE) {
            return@sequence
        }
        if (this@visibleText is TextView) {
            yield(text.toString().lowercase())
        }
        if (this@visibleText is ViewGroup) {
            for (index in 0 until childCount) {
                yieldAll(getChildAt(index).visibleText())
            }
        }
    }
}
