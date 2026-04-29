package com.woong.monitorstack.settings

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.woong.monitorstack.databinding.ActivitySettingsBinding
import com.woong.monitorstack.summary.NotificationPermissionController
import com.woong.monitorstack.usage.UsageAccessSettingsIntentFactory

class SettingsActivity : AppCompatActivity() {
    companion object {
        const val EXTRA_SYNC_FAILED_COUNT = "com.woong.monitorstack.settings.SYNC_FAILED_COUNT"
        const val EXTRA_SYNC_FAILURE_MESSAGE = "com.woong.monitorstack.settings.SYNC_FAILURE_MESSAGE"
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val binding = ActivitySettingsBinding.inflate(layoutInflater)
        setContentView(binding.root)
        val usageAccessSettings = UsageAccessSettingsIntentFactory()
        binding.openUsageAccessSettingsButton.setOnClickListener {
            startActivity(usageAccessSettings.createIntent())
        }
        val notificationPermissionController = NotificationPermissionController(this)
        binding.requestNotificationPermissionButton.setOnClickListener {
            notificationPermissionController.requestIfNeeded()
        }
        renderLocationSettings(binding, SharedPreferencesAndroidLocationSettings(this))
        renderSyncStatus(binding)
    }

    private fun renderLocationSettings(
        binding: ActivitySettingsBinding,
        settings: SharedPreferencesAndroidLocationSettings
    ) {
        binding.locationContextCheckBox.isChecked = settings.isLocationCaptureEnabled()
        binding.preciseLatitudeLongitudeCheckBox.isChecked =
            settings.isPreciseLatitudeLongitudeEnabled()
        binding.preciseLatitudeLongitudeCheckBox.isEnabled = settings.isLocationCaptureEnabled()
        binding.requestLocationPermissionButton.isEnabled = settings.isLocationCaptureEnabled()
        val locationPermissionController = LocationPermissionController(this)
        binding.requestLocationPermissionButton.setOnClickListener {
            locationPermissionController.requestIfNeeded(settings)
        }

        binding.locationContextCheckBox.setOnCheckedChangeListener { _, isChecked ->
            settings.setLocationCaptureEnabled(isChecked)
            binding.preciseLatitudeLongitudeCheckBox.isEnabled = isChecked
            binding.requestLocationPermissionButton.isEnabled = isChecked
            if (!isChecked) {
                binding.preciseLatitudeLongitudeCheckBox.isChecked = false
            }
        }
        binding.preciseLatitudeLongitudeCheckBox.setOnCheckedChangeListener { _, isChecked ->
            settings.setPreciseLatitudeLongitudeEnabled(isChecked)
        }
    }

    private fun renderSyncStatus(binding: ActivitySettingsBinding) {
        val failedCount = intent.getIntExtra(EXTRA_SYNC_FAILED_COUNT, 0)
        val failureMessage = intent.getStringExtra(EXTRA_SYNC_FAILURE_MESSAGE).orEmpty()
        if (failedCount > 0 && failureMessage.isNotBlank()) {
            binding.syncStatusText.text = getString(
                com.woong.monitorstack.R.string.sync_failure_status,
                failedCount,
                failureMessage
            )
        }
    }
}
