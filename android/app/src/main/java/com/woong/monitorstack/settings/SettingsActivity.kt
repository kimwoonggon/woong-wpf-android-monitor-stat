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
        renderSyncStatus(binding)
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
