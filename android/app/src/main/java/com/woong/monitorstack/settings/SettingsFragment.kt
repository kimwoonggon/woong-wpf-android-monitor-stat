package com.woong.monitorstack.settings

import android.content.Context
import android.os.Build
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.util.TypedValue
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.LinearLayout
import androidx.appcompat.app.AppCompatActivity
import androidx.fragment.app.Fragment
import androidx.core.widget.doAfterTextChanged
import androidx.work.OneTimeWorkRequestBuilder
import androidx.work.WorkManager
import androidx.work.workDataOf
import com.woong.monitorstack.R
import com.woong.monitorstack.databinding.FragmentSettingsBinding
import com.woong.monitorstack.summary.NotificationPermissionController
import com.woong.monitorstack.sync.AndroidSyncClient
import com.woong.monitorstack.sync.AndroidSyncWorker
import com.woong.monitorstack.sync.SyncDeviceRegistrationRequest
import com.woong.monitorstack.usage.AndroidUsageAccessPermissionReader
import com.woong.monitorstack.usage.UsageAccessPermissionChecker
import com.woong.monitorstack.usage.UsageAccessSettingsIntentFactory
import com.google.android.material.button.MaterialButton
import java.util.TimeZone

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
        renderPermissionSettings(
            fragmentBinding,
            usageAccessStatusReaderFactory(requireContext())
        )
        renderCollectionSettings(
            fragmentBinding,
            SharedPreferencesAndroidUsageCollectionSettings(requireContext())
        )

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

    override fun onResume() {
        super.onResume()
        binding?.let { fragmentBinding ->
            renderPermissionSettings(
                fragmentBinding,
                usageAccessStatusReaderFactory(requireContext())
            )
        }
    }

    override fun onDestroyView() {
        binding = null
        super.onDestroyView()
    }

    private fun renderPermissionSettings(
        binding: FragmentSettingsBinding,
        usageAccessStatusReader: UsageAccessStatusReader
    ) {
        val statusText = if (usageAccessStatusReader.hasUsageAccess(requireContext().packageName)) {
            R.string.usage_access_status_granted
        } else {
            R.string.usage_access_status_missing
        }
        binding.usageAccessStatusRow.text = getString(statusText)
    }

    private fun renderCollectionSettings(
        binding: FragmentSettingsBinding,
        settings: SharedPreferencesAndroidUsageCollectionSettings
    ) {
        binding.backgroundCollectionSwitch.isChecked = settings.isCollectionEnabled()
        binding.backgroundCollectionSwitch.setOnCheckedChangeListener { _, isChecked ->
            settings.setCollectionEnabled(isChecked)
        }
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
        val manualSyncLauncher = manualSyncLauncherFactory(requireContext())
        val deviceRegistrationLauncher = deviceRegistrationLauncherFactory(requireContext())
        val deviceDisconnectLauncher = deviceDisconnectLauncherFactory(requireContext())
        val registerRepairButton = ensureRegisterRepairButton(binding)
        val disconnectButton = ensureDisconnectButton(binding)

        fun renderStatus(enabled: Boolean) {
            binding.syncStatusText.text = if (enabled) {
                getString(R.string.sync_enabled_status)
            } else {
                getString(R.string.sync_local_only_status)
            }
            binding.syncDeviceRegistrationStatusText.text = when {
                settings.lastSyncStatus() == AndroidSyncWorker.STATUS_AUTH_REQUIRED ->
                    getString(R.string.sync_auth_required_status)
                settings.deviceId().isNotBlank() && settings.deviceToken().isNotBlank() ->
                    getString(R.string.sync_device_registered_status)
                else -> getString(R.string.sync_device_unregistered_status)
            }
        }

        binding.autoSyncSwitch.isChecked = settings.isSyncEnabled()
        binding.syncServerUrlEditText.setText(settings.serverBaseUrl())
        binding.syncDeviceIdEditText.setText(settings.deviceId())
        binding.manualSyncButton.isEnabled = true
        renderStatus(binding.autoSyncSwitch.isChecked)

        binding.syncServerUrlEditText.doAfterTextChanged { value ->
            settings.setServerBaseUrl(value?.toString().orEmpty())
        }
        binding.syncDeviceIdEditText.doAfterTextChanged { value ->
            settings.setDeviceId(value?.toString().orEmpty())
        }

        binding.autoSyncSwitch.setOnCheckedChangeListener { _, isChecked ->
            settings.setSyncEnabled(isChecked)
            renderStatus(isChecked)
        }

        binding.manualSyncButton.setOnClickListener {
            if (settings.isSyncEnabled()) {
                val serverBaseUrl = binding.syncServerUrlEditText.text.toString().trim()
                val deviceId = binding.syncDeviceIdEditText.text.toString().trim()
                settings.setServerBaseUrl(serverBaseUrl)
                settings.setDeviceId(deviceId)

                if (serverBaseUrl.isBlank()) {
                    binding.syncStatusText.text = getString(R.string.sync_missing_configuration_status)
                    return@setOnClickListener
                }
                if (!AndroidSyncServerUrlValidator.isValid(serverBaseUrl)) {
                    binding.syncStatusText.text = getString(R.string.sync_invalid_server_url_status)
                    return@setOnClickListener
                }
                if (deviceId.isBlank() || settings.deviceToken().isBlank()) {
                    binding.syncStatusText.text = getString(R.string.sync_registration_required_status)
                    return@setOnClickListener
                }

                manualSyncLauncher.enqueue(
                    baseUrl = serverBaseUrl,
                    deviceId = deviceId,
                    pendingLimit = AndroidSyncWorker.DEFAULT_PENDING_LIMIT
                )
                binding.syncStatusText.text = getString(R.string.sync_manual_queued_status)
            } else {
                binding.syncStatusText.text = getString(R.string.sync_manual_skipped_status)
            }
        }

        registerRepairButton.setOnClickListener {
            if (!settings.isSyncEnabled()) {
                binding.syncStatusText.text = getString(R.string.sync_registration_skipped_status)
                return@setOnClickListener
            }

            val serverBaseUrl = binding.syncServerUrlEditText.text.toString().trim()
            settings.setServerBaseUrl(serverBaseUrl)
            if (serverBaseUrl.isBlank()) {
                binding.syncStatusText.text = getString(R.string.sync_missing_configuration_status)
                return@setOnClickListener
            }
            if (!AndroidSyncServerUrlValidator.isValid(serverBaseUrl)) {
                binding.syncStatusText.text = getString(R.string.sync_invalid_server_url_status)
                return@setOnClickListener
            }

            binding.syncStatusText.text = getString(R.string.sync_registration_in_progress_status)
            deviceRegistrationLauncher.register(
                DeviceRegistrationRequest(
                    baseUrl = serverBaseUrl,
                    userId = LocalAndroidUserId,
                    platform = AndroidPlatformCode,
                    deviceKey = settings.deviceKey(),
                    deviceName = Build.MODEL.takeIf { it.isNotBlank() } ?: "Android device",
                    timezoneId = TimeZone.getDefault().id
                )
            ) { result ->
                val currentBinding = this.binding ?: return@register
                result.onSuccess { registration ->
                    settings.persistRegisteredDevice(
                        deviceId = registration.deviceId,
                        deviceToken = registration.deviceToken
                    )
                    currentBinding.syncDeviceIdEditText.setText(registration.deviceId)
                    currentBinding.syncDeviceRegistrationStatusText.text =
                        getString(R.string.sync_device_registered_status)
                    currentBinding.syncStatusText.text =
                        getString(R.string.sync_registration_success_status)
                }.onFailure {
                    currentBinding.syncStatusText.text =
                        getString(R.string.sync_registration_failed_status)
                }
            }
        }

        disconnectButton.setOnClickListener {
            val serverBaseUrl = binding.syncServerUrlEditText.text.toString().trim()
            val deviceId = binding.syncDeviceIdEditText.text.toString().trim()
            val deviceToken = settings.deviceToken()

            if (!settings.isSyncEnabled()) {
                clearLocalSyncRegistration(settings, binding)
                binding.syncStatusText.text =
                    getString(R.string.sync_disconnect_local_success_status)
                return@setOnClickListener
            }

            if (serverBaseUrl.isBlank() || deviceId.isBlank() || deviceToken.isBlank()) {
                clearLocalSyncRegistration(settings, binding)
                binding.syncStatusText.text =
                    getString(R.string.sync_disconnect_local_success_status)
                return@setOnClickListener
            }
            if (!AndroidSyncServerUrlValidator.isValid(serverBaseUrl)) {
                binding.syncStatusText.text = getString(R.string.sync_invalid_server_url_status)
                return@setOnClickListener
            }

            binding.syncStatusText.text = getString(R.string.sync_disconnect_in_progress_status)
            deviceDisconnectLauncher.disconnect(
                DeviceDisconnectRequest(
                    baseUrl = serverBaseUrl,
                    deviceId = deviceId,
                    deviceToken = deviceToken
                )
            ) { result ->
                val currentBinding = this.binding ?: return@disconnect
                result.onSuccess {
                    clearLocalSyncRegistration(settings, currentBinding)
                    currentBinding.syncStatusText.text =
                        getString(R.string.sync_disconnect_success_status)
                }.onFailure {
                    currentBinding.syncStatusText.text =
                        getString(R.string.sync_disconnect_failed_status)
                }
            }
        }
    }

    private fun clearLocalSyncRegistration(
        settings: SharedPreferencesAndroidSyncSettings,
        binding: FragmentSettingsBinding
    ) {
        settings.clearSyncConfiguration()
        binding.autoSyncSwitch.isChecked = false
        binding.syncServerUrlEditText.setText("")
        binding.syncDeviceIdEditText.setText("")
        binding.syncDeviceRegistrationStatusText.text =
            getString(R.string.sync_device_unregistered_status)
    }

    private fun ensureRegisterRepairButton(binding: FragmentSettingsBinding): MaterialButton {
        val parent = binding.manualSyncButton.parent as ViewGroup
        val existingButton = parent.findViewWithTag<MaterialButton>(RegisterRepairButtonTag)
        if (existingButton != null) {
            return existingButton
        }

        val button = MaterialButton(requireContext()).apply {
            tag = RegisterRepairButtonTag
            text = getString(R.string.sync_register_repair_device)
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                dpToPx(44)
            ).apply {
                topMargin = dpToPx(8)
            }
        }
        val manualSyncIndex = parent.indexOfChild(binding.manualSyncButton)
        parent.addView(button, manualSyncIndex)
        return button
    }

    private fun ensureDisconnectButton(binding: FragmentSettingsBinding): MaterialButton {
        val parent = binding.manualSyncButton.parent as ViewGroup
        val existingButton = parent.findViewWithTag<MaterialButton>(DisconnectButtonTag)
        if (existingButton != null) {
            return existingButton
        }

        val button = MaterialButton(requireContext()).apply {
            tag = DisconnectButtonTag
            text = getString(R.string.sync_disconnect_device)
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                dpToPx(44)
            ).apply {
                topMargin = dpToPx(8)
            }
        }
        val manualSyncIndex = parent.indexOfChild(binding.manualSyncButton)
        parent.addView(button, manualSyncIndex)
        return button
    }

    private fun dpToPx(dp: Int): Int {
        return TypedValue.applyDimension(
            TypedValue.COMPLEX_UNIT_DIP,
            dp.toFloat(),
            resources.displayMetrics
        ).toInt()
    }

    interface ManualSyncLauncher {
        fun enqueue(
            baseUrl: String,
            deviceId: String,
            pendingLimit: Int
        )
    }

    private class WorkManagerManualSyncLauncher(
        private val context: android.content.Context
    ) : ManualSyncLauncher {
        override fun enqueue(
            baseUrl: String,
            deviceId: String,
            pendingLimit: Int
        ) {
            val request = OneTimeWorkRequestBuilder<AndroidSyncWorker>()
                .setInputData(
                    workDataOf(
                        AndroidSyncWorker.KEY_BASE_URL to baseUrl,
                        AndroidSyncWorker.KEY_DEVICE_ID to deviceId,
                        AndroidSyncWorker.KEY_PENDING_LIMIT to pendingLimit
                    )
                )
                .build()
            WorkManager.getInstance(context.applicationContext).enqueue(request)
        }
    }

    interface UsageAccessStatusReader {
        fun hasUsageAccess(packageName: String): Boolean
    }

    private class AndroidUsageAccessStatusReader(
        context: Context
    ) : UsageAccessStatusReader {
        private val checker = UsageAccessPermissionChecker(
            AndroidUsageAccessPermissionReader(context.applicationContext)
        )

        override fun hasUsageAccess(packageName: String): Boolean {
            return checker.hasUsageAccess(packageName)
        }
    }

    data class DeviceRegistrationRequest(
        val baseUrl: String,
        val userId: String,
        val platform: Int,
        val deviceKey: String,
        val deviceName: String,
        val timezoneId: String
    )

    interface DeviceRegistrationLauncher {
        fun register(
            request: DeviceRegistrationRequest,
            callback: (Result<DeviceRegistrationResult>) -> Unit
        )
    }

    interface DeviceDisconnectLauncher {
        fun disconnect(
            request: DeviceDisconnectRequest,
            callback: (Result<Unit>) -> Unit
        )
    }

    private class AndroidSyncClientDeviceRegistrationLauncher : DeviceRegistrationLauncher {
        private val mainHandler = Handler(Looper.getMainLooper())

        override fun register(
            request: DeviceRegistrationRequest,
            callback: (Result<DeviceRegistrationResult>) -> Unit
        ) {
            Thread(
                {
                    val result = runCatching {
                        val response = AndroidSyncClient(request.baseUrl).registerDevice(
                            SyncDeviceRegistrationRequest(
                                userId = request.userId,
                                platform = request.platform,
                                deviceKey = request.deviceKey,
                                deviceName = request.deviceName,
                                timezoneId = request.timezoneId
                            )
                        )
                        DeviceRegistrationResult(
                            deviceId = response.deviceId,
                            deviceToken = response.deviceToken
                        )
                    }
                    mainHandler.post { callback(result) }
                },
                "AndroidSyncDeviceRegistration"
            ).start()
        }
    }

    private class AndroidSyncClientDeviceDisconnectLauncher : DeviceDisconnectLauncher {
        private val mainHandler = Handler(Looper.getMainLooper())

        override fun disconnect(
            request: DeviceDisconnectRequest,
            callback: (Result<Unit>) -> Unit
        ) {
            Thread(
                {
                    val result = runCatching {
                        AndroidSyncClient(
                            baseUrl = request.baseUrl,
                            deviceToken = request.deviceToken
                        ).revokeDeviceToken(request.deviceId)
                    }
                    mainHandler.post { callback(result) }
                },
                "AndroidSyncDeviceDisconnect"
            ).start()
        }
    }

    companion object {
        private const val RegisterRepairButtonTag = "sync_register_repair_button"
        private const val DisconnectButtonTag = "sync_disconnect_button"
        private const val LocalAndroidUserId = "local-android-user"
        private const val AndroidPlatformCode = 2

        fun defaultManualSyncLauncherFactory(): (android.content.Context) -> ManualSyncLauncher = {
            WorkManagerManualSyncLauncher(it)
        }

        fun defaultDeviceRegistrationLauncherFactory():
            (android.content.Context) -> DeviceRegistrationLauncher = {
                AndroidSyncClientDeviceRegistrationLauncher()
            }

        fun defaultDeviceDisconnectLauncherFactory():
            (android.content.Context) -> DeviceDisconnectLauncher = {
                AndroidSyncClientDeviceDisconnectLauncher()
            }

        fun defaultUsageAccessStatusReaderFactory(): (android.content.Context) -> UsageAccessStatusReader = {
            AndroidUsageAccessStatusReader(it)
        }

        var manualSyncLauncherFactory: (android.content.Context) -> ManualSyncLauncher =
            defaultManualSyncLauncherFactory()

        var deviceRegistrationLauncherFactory:
            (android.content.Context) -> DeviceRegistrationLauncher =
            defaultDeviceRegistrationLauncherFactory()

        var deviceDisconnectLauncherFactory:
            (android.content.Context) -> DeviceDisconnectLauncher =
            defaultDeviceDisconnectLauncherFactory()

        var usageAccessStatusReaderFactory: (android.content.Context) -> UsageAccessStatusReader =
            defaultUsageAccessStatusReaderFactory()
    }
}

data class DeviceDisconnectRequest(
    val baseUrl: String,
    val deviceId: String,
    val deviceToken: String
)

data class DeviceRegistrationResult(
    val deviceId: String,
    val deviceToken: String
)
