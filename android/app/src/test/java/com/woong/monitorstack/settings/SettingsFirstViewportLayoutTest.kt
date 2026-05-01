package com.woong.monitorstack.settings

import android.content.Context
import android.view.ContextThemeWrapper
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.R
import com.woong.monitorstack.databinding.FragmentSettingsBinding
import kotlin.math.roundToInt
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class SettingsFirstViewportLayoutTest {
    @Test
    fun settingsKeepsSectionsCompactWhileShowingSafeDefaultsAndSyncFields() {
        val context = ContextThemeWrapper(
            ApplicationProvider.getApplicationContext<Context>(),
            R.style.Theme_WoongMonitor
        )
        val binding = FragmentSettingsBinding.inflate(LayoutInflater.from(context))
        val density = context.resources.displayMetrics.density

        assertTrue(
            "Settings cards should start close to the heading like the reference.",
            binding.permissionsSettingsCard.topMargin() <= 12.dp(density)
        )
        assertTrue(
            "Settings sections should use compact card spacing.",
            binding.collectionSettingsCard.topMargin() <= 8.dp(density)
        )
        assertTrue(
            "Sync section should remain close enough for first-viewport scanning.",
            binding.syncSettingsCard.topMargin() <= 8.dp(density)
        )
        assertTrue(
            "Permission row should stay compact while preserving Usage Access visibility.",
            binding.usageAccessStatusRow.layoutParams.height <= 40.dp(density)
        )
        assertTrue(
            "Usage Access action should remain visible without dominating the viewport.",
            binding.openUsageAccessSettingsButton.layoutParams.height <= 44.dp(density)
        )
        assertTrue(
            "Collection toggle row should be compact and readable.",
            binding.backgroundCollectionRow.layoutParams.height <= 44.dp(density)
        )
        assertTrue(
            "Sync toggle row should be compact and readable.",
            binding.autoSyncRow.layoutParams.height <= 44.dp(density)
        )
        assertFalse("Sync must remain opt-in and off by default.", binding.autoSyncSwitch.isChecked)
        assertTrue(
            "Server URL remains accessible in the sync card.",
            binding.syncServerUrlEditText.visibility == View.VISIBLE
        )
        assertTrue(
            "Device ID remains accessible in the sync card.",
            binding.syncDeviceIdEditText.visibility == View.VISIBLE
        )
        assertTrue(
            "Sync fields should be compact enough to leave privacy/location/storage sections reachable.",
            binding.syncServerUrlEditText.layoutParams.height <= 44.dp(density)
        )
        assertTrue(
            "Location safe default section should follow sync with compact spacing.",
            binding.locationSettingsCard.topMargin() <= 8.dp(density)
        )
        assertFalse(
            "Location context must remain opt-in and off by default.",
            binding.locationContextCheckBox.isChecked
        )
        assertTrue(
            "Privacy/storage section should remain available after location settings.",
            binding.privacyStorageSettingsCard.visibility == View.VISIBLE
        )
    }

    private fun View.topMargin(): Int {
        return (layoutParams as ViewGroup.MarginLayoutParams).topMargin
    }

    private fun Int.dp(density: Float): Int = (this * density).roundToInt()
}
