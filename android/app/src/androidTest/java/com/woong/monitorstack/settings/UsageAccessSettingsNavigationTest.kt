package com.woong.monitorstack.settings

import android.content.Context
import androidx.test.core.app.ActivityScenario
import androidx.test.core.app.ApplicationProvider
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.matcher.ViewMatchers.withId
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.uiautomator.By
import androidx.test.uiautomator.UiDevice
import androidx.test.uiautomator.Until
import com.woong.monitorstack.R
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class UsageAccessSettingsNavigationTest {
    @Test
    fun usageAccessButtonOpensSystemSettings() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val device = UiDevice.getInstance(InstrumentationRegistry.getInstrumentation())

        ActivityScenario.launch<SettingsActivity>(
            android.content.Intent(context, SettingsActivity::class.java)
        ).use {
            onView(withId(R.id.openUsageAccessSettingsButton)).perform(click())

            val openedSettings = device.wait(
                Until.hasObject(By.pkg("com.android.settings")),
                SettingsOpenTimeoutMs
            )

            assertTrue(openedSettings)
        }
    }

    companion object {
        private const val SettingsOpenTimeoutMs = 5_000L
    }
}
