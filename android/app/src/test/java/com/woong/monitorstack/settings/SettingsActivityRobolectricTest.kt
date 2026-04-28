package com.woong.monitorstack.settings

import android.content.Context
import android.content.Intent
import android.view.ContextThemeWrapper
import android.view.LayoutInflater
import android.widget.TextView
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.R
import com.woong.monitorstack.databinding.ActivitySettingsBinding
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.Robolectric
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class SettingsActivityRobolectricTest {
    @Test
    fun settingsActivityDisplaysClearUsageAccessGuidanceAndSyncOffStatus() {
        val context = ContextThemeWrapper(
            ApplicationProvider.getApplicationContext<Context>(),
            R.style.Theme_WoongMonitor
        )
        val binding = ActivitySettingsBinding.inflate(LayoutInflater.from(context))

        assertNotNull(binding.openUsageAccessSettingsButton)
        assertEquals(
            "Usage Access is needed to calculate app usage statistics.",
            binding.usageAccessGuidanceText.text.toString()
        )
        assertEquals(
            "This app does not collect messages, passwords, form input, or global touch coordinates.",
            binding.sensitiveDataBoundaryText.text.toString()
        )
        assertEquals("Open Usage Access settings", binding.openUsageAccessSettingsButton.text.toString())
        assertEquals("Sync is off. Data stays on this Android device.", binding.syncStatusText.text.toString())
        assertEquals(
            "Morning summary notifications require notification permission on Android 13+.",
            binding.notificationPermissionGuidanceText.text.toString()
        )
        assertEquals("Allow notifications", binding.requestNotificationPermissionButton.text.toString())
    }

    @Test
    fun settingsActivityDisplaysRetryableSyncFailureStatusFromIntent() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val intent = Intent(context, SettingsActivity::class.java)
            .putExtra(SettingsActivity.EXTRA_SYNC_FAILED_COUNT, 2)
            .putExtra(SettingsActivity.EXTRA_SYNC_FAILURE_MESSAGE, "server unavailable")

        val activity = Robolectric.buildActivity(SettingsActivity::class.java, intent)
            .setup()
            .get()

        val syncStatus = activity.findViewById<TextView>(R.id.syncStatusText)

        assertEquals(
            "Sync failed for 2 queued items: server unavailable. Data remains local and will retry.",
            syncStatus.text.toString()
        )
    }
}
