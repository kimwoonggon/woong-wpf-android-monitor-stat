package com.woong.monitorstack.settings

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.appcompat.app.AppCompatActivity
import androidx.fragment.app.Fragment
import com.woong.monitorstack.R
import com.woong.monitorstack.databinding.FragmentSettingsBinding
import com.woong.monitorstack.summary.NotificationPermissionController
import com.woong.monitorstack.usage.UsageAccessSettingsIntentFactory

class SettingsFragment : Fragment() {
    private var binding: FragmentSettingsBinding? = null

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        val fragmentBinding = FragmentSettingsBinding.inflate(inflater, container, false)
        binding = fragmentBinding
        return fragmentBinding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        val fragmentBinding = requireNotNull(binding)
        val usageAccessSettings = UsageAccessSettingsIntentFactory()
        fragmentBinding.openUsageAccessSettingsButton.setOnClickListener {
            startActivity(usageAccessSettings.createIntent())
        }

        val hostActivity = requireActivity() as AppCompatActivity
        val notificationPermissionController = NotificationPermissionController(hostActivity)
        fragmentBinding.requestNotificationPermissionButton.setOnClickListener {
            notificationPermissionController.requestIfNeeded()
        }

        renderLocationSettings(
            fragmentBinding,
            SharedPreferencesAndroidLocationSettings(requireContext())
        )
        renderSyncSettings(
            fragmentBinding,
            SharedPreferencesAndroidSyncSettings(requireContext())
        )
    }

    override fun onDestroyView() {
        binding = null
        super.onDestroyView()
    }

    private fun renderLocationSettings(
        binding: FragmentSettingsBinding,
        settings: SharedPreferencesAndroidLocationSettings
    ) {
        binding.locationContextCheckBox.isChecked = settings.isLocationCaptureEnabled()
        binding.preciseLatitudeLongitudeCheckBox.isChecked =
            settings.isPreciseLatitudeLongitudeEnabled()
        binding.preciseLatitudeLongitudeCheckBox.isEnabled = settings.isLocationCaptureEnabled()
        binding.requestLocationPermissionButton.isEnabled = settings.isLocationCaptureEnabled()

        val locationPermissionController = LocationPermissionController(
            requireActivity() as AppCompatActivity
        )
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

    private fun renderSyncSettings(
        binding: FragmentSettingsBinding,
        settings: SharedPreferencesAndroidSyncSettings
    ) {
        fun renderStatus(enabled: Boolean) {
            binding.syncStatusText.text = if (enabled) {
                getString(R.string.sync_enabled_status)
            } else {
                getString(R.string.sync_local_only_status)
            }
        }

        binding.autoSyncSwitch.isChecked = settings.isSyncEnabled()
        binding.manualSyncButton.isEnabled = true
        renderStatus(binding.autoSyncSwitch.isChecked)

        binding.autoSyncSwitch.setOnCheckedChangeListener { _, isChecked ->
            settings.setSyncEnabled(isChecked)
            renderStatus(isChecked)
        }

        binding.manualSyncButton.setOnClickListener {
            if (settings.isSyncEnabled()) {
                binding.syncStatusText.text = getString(R.string.sync_enabled_status)
            } else {
                binding.syncStatusText.text = getString(R.string.sync_manual_skipped_status)
            }
        }
    }
}
