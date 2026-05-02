package com.woong.monitorstack.settings

import android.content.Context
import android.widget.Button
import android.widget.EditText
import android.widget.FrameLayout
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.R
import com.woong.monitorstack.sync.AndroidSyncWorker
import com.google.android.material.switchmaterial.SwitchMaterial
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.Robolectric
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class SettingsFragmentManualSyncTest {
    private lateinit var context: Context
    private lateinit var launcher: RecordingManualSyncLauncher
    private lateinit var registrationLauncher: RecordingDeviceRegistrationLauncher
    private lateinit var tokenStore: FakeAndroidSyncTokenStore

    @Before
    fun setUp() {
        context = ApplicationProvider.getApplicationContext()
        context.getSharedPreferences(
            SharedPreferencesAndroidSyncSettings.PreferenceName,
            Context.MODE_PRIVATE
        ).edit().clear().commit()
        tokenStore = FakeAndroidSyncTokenStore()
        SharedPreferencesAndroidSyncSettings.tokenStoreFactory = { tokenStore }
        launcher = RecordingManualSyncLauncher()
        registrationLauncher = RecordingDeviceRegistrationLauncher()
        SettingsFragment.manualSyncLauncherFactory = { launcher }
        SettingsFragment.deviceRegistrationLauncherFactory = { registrationLauncher }
    }

    @After
    fun tearDown() {
        SharedPreferencesAndroidSyncSettings.tokenStoreFactory =
            SharedPreferencesAndroidSyncSettings.defaultTokenStoreFactory()
        SettingsFragment.manualSyncLauncherFactory = SettingsFragment.defaultManualSyncLauncherFactory()
        SettingsFragment.deviceRegistrationLauncherFactory =
            SettingsFragment.defaultDeviceRegistrationLauncherFactory()
    }

    @Test
    fun defaultSyncStateShowsLocalOnlyUnregisteredAndVisibleRepairAction() {
        val activity = launchSettingsFragment()

        assertFalse(activity.findViewById<SwitchMaterial>(R.id.autoSyncSwitch).isChecked)
        assertEquals(
            "Sync is off. Data stays on this Android device.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
        assertEquals(
            "Device not registered. Register / repair is available after sync is turned on.",
            activity.findViewById<TextView>(R.id.syncDeviceRegistrationStatusText).text.toString()
        )
        assertTrue(findButtonByText(activity, "Register / repair device").isShown)
        assertEquals(emptyList<ManualSyncLaunchRequest>(), launcher.requests)
    }

    @Test
    fun authRequiredSyncStateShowsRepairNeededWithoutDisablingOptIn() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setSyncEnabled(true)
        settings.persistRegisteredDevice(
            deviceId = "android-device-1",
            deviceToken = "expired-device-token"
        )
        settings.recordSyncStatus(
            status = AndroidSyncWorker.STATUS_AUTH_REQUIRED,
            message = "Android sync authorization failed. Register this device again."
        )

        val activity = launchSettingsFragment()

        assertTrue(activity.findViewById<SwitchMaterial>(R.id.autoSyncSwitch).isChecked)
        assertEquals(
            "Sync is on. Manual sync will use configured server settings.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
        assertEquals(
            "Sync authorization failed. Register / repair this device before syncing again.",
            activity.findViewById<TextView>(R.id.syncDeviceRegistrationStatusText).text.toString()
        )
        assertTrue(findButtonByText(activity, "Register / repair device").isShown)
    }

    @Test
    fun registeredDeviceStateIsVisibleWithoutTurningSyncOnByDefault() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.persistRegisteredDevice(
            deviceId = "android-device-1",
            deviceToken = "device-token-secret"
        )

        val activity = launchSettingsFragment()

        assertFalse(activity.findViewById<SwitchMaterial>(R.id.autoSyncSwitch).isChecked)
        assertEquals(
            "Sync is off. Data stays on this Android device.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
        assertEquals(
            "Device registered for sync.",
            activity.findViewById<TextView>(R.id.syncDeviceRegistrationStatusText).text.toString()
        )
    }

    @Test
    fun syncConfigurationFieldsLoadAndPersistTrimmedValues() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setServerBaseUrl("https://server.example")
        settings.setDeviceId("android-device-1")
        val activity = launchSettingsFragment()

        val serverUrl = activity.findViewById<EditText>(R.id.syncServerUrlEditText)
        val deviceId = activity.findViewById<EditText>(R.id.syncDeviceIdEditText)
        assertEquals("https://server.example", serverUrl.text.toString())
        assertEquals("android-device-1", deviceId.text.toString())

        serverUrl.setText("  https://api.example.test/  ")
        deviceId.setText("\n android-phone-01\t")

        val reloaded = SharedPreferencesAndroidSyncSettings(context)
        assertEquals("https://api.example.test/", reloaded.serverBaseUrl())
        assertEquals("android-phone-01", reloaded.deviceId())
    }

    @Test
    fun manualSyncWhenSyncOffShowsLocalOnlySkippedAndDoesNotLaunchWorker() {
        val activity = launchSettingsFragment()

        activity.findViewById<EditText>(R.id.syncServerUrlEditText)
            .setText("https://server.example")
        activity.findViewById<EditText>(R.id.syncDeviceIdEditText)
            .setText("android-device-1")
        activity.findViewById<Button>(R.id.manualSyncButton).performClick()

        assertEquals(
            "Manual sync skipped because sync is off. Local only.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
        assertEquals(emptyList<ManualSyncLaunchRequest>(), launcher.requests)
    }

    @Test
    fun manualSyncWhenSyncOnWithMissingConfigurationShowsRequiredMessageWithoutLaunch() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setSyncEnabled(true)
        val activity = launchSettingsFragment()

        activity.findViewById<EditText>(R.id.syncServerUrlEditText).setText("  ")
        activity.findViewById<EditText>(R.id.syncDeviceIdEditText).setText("android-device-1")
        activity.findViewById<Button>(R.id.manualSyncButton).performClick()

        assertEquals(
            "Manual sync needs a server URL and device ID before upload can run.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
        assertEquals(emptyList<ManualSyncLaunchRequest>(), launcher.requests)
    }

    @Test
    fun manualSyncWhenSyncOnWithInvalidServerUrlShowsUrlMessageWithoutLaunch() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setSyncEnabled(true)
        val activity = launchSettingsFragment()

        activity.findViewById<EditText>(R.id.syncServerUrlEditText)
            .setText("http://server.example")
        activity.findViewById<EditText>(R.id.syncDeviceIdEditText).setText("android-device-1")
        activity.findViewById<Button>(R.id.manualSyncButton).performClick()

        assertEquals(
            "Manual sync requires an HTTPS server URL, or HTTP localhost for local development.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
        assertEquals(emptyList<ManualSyncLaunchRequest>(), launcher.requests)

        activity.findViewById<EditText>(R.id.syncServerUrlEditText)
            .setText("https://user:pass@server.example")
        activity.findViewById<Button>(R.id.manualSyncButton).performClick()

        assertEquals(
            "Manual sync requires an HTTPS server URL, or HTTP localhost for local development.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
        assertEquals(emptyList<ManualSyncLaunchRequest>(), launcher.requests)
    }

    @Test
    fun manualSyncWhenSyncOnWithoutRegisteredDeviceShowsRegistrationRequiredWithoutLaunch() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setSyncEnabled(true)
        val activity = launchSettingsFragment()

        activity.findViewById<EditText>(R.id.syncServerUrlEditText)
            .setText("https://server.example")
        activity.findViewById<Button>(R.id.manualSyncButton).performClick()

        assertEquals(
            "Manual sync needs device registration before upload can run.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
        assertEquals(emptyList<ManualSyncLaunchRequest>(), launcher.requests)
    }

    @Test
    fun registerRepairWhenSyncOnPersistsDeviceTokenWithoutLaunchingManualSync() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setSyncEnabled(true)
        registrationLauncher.response = DeviceRegistrationResult(
            deviceId = "server-device-id",
            deviceToken = "device-token-secret"
        )
        val activity = launchSettingsFragment()

        activity.findViewById<EditText>(R.id.syncServerUrlEditText)
            .setText("  https://server.example  ")
        findButtonByText(activity, "Register / repair device").performClick()

        val request = registrationLauncher.requests.single()
        assertEquals("https://server.example", request.baseUrl)
        assertEquals("local-android-user", request.userId)
        assertEquals(2, request.platform)
        assertTrue(request.deviceKey.isNotBlank())
        assertTrue(request.deviceName.isNotBlank())
        assertTrue(request.timezoneId.isNotBlank())
        assertEquals("server-device-id", SharedPreferencesAndroidSyncSettings(context).deviceId())
        assertEquals("device-token-secret", SharedPreferencesAndroidSyncSettings(context).deviceToken())
        assertEquals(
            "server-device-id",
            activity.findViewById<EditText>(R.id.syncDeviceIdEditText).text.toString()
        )
        assertEquals(
            "Device registered. Manual sync can now run.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
        assertEquals(emptyList<ManualSyncLaunchRequest>(), launcher.requests)
    }

    @Test
    fun registerRepairSuccessClearsAuthRequiredDeviceState() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setSyncEnabled(true)
        settings.recordSyncStatus(
            status = AndroidSyncWorker.STATUS_AUTH_REQUIRED,
            message = "Android sync authorization failed. Register this device again."
        )
        registrationLauncher.response = DeviceRegistrationResult(
            deviceId = "server-device-id",
            deviceToken = "replacement-device-token"
        )
        val activity = launchSettingsFragment()

        activity.findViewById<EditText>(R.id.syncServerUrlEditText)
            .setText("https://server.example")
        findButtonByText(activity, "Register / repair device").performClick()

        assertEquals("", SharedPreferencesAndroidSyncSettings(context).lastSyncStatus())
        assertEquals(
            "Device registered for sync.",
            activity.findViewById<TextView>(R.id.syncDeviceRegistrationStatusText).text.toString()
        )
        assertEquals(
            "Device registered. Manual sync can now run.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
    }

    @Test
    fun manualSyncWhenSyncOnWithValidConfigurationLaunchesWorkerWithStoredValues() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setSyncEnabled(true)
        settings.persistRegisteredDevice(
            deviceId = "android-device-1",
            deviceToken = "device-token-secret"
        )
        val activity = launchSettingsFragment()

        activity.findViewById<EditText>(R.id.syncServerUrlEditText)
            .setText("  https://server.example  ")
        activity.findViewById<EditText>(R.id.syncDeviceIdEditText)
            .setText(" android-device-1 ")
        activity.findViewById<Button>(R.id.manualSyncButton).performClick()

        assertEquals(
            listOf(
                ManualSyncLaunchRequest(
                    baseUrl = "https://server.example",
                    deviceId = "android-device-1",
                    pendingLimit = 50
                )
            ),
            launcher.requests
        )
        assertEquals(
            "Manual sync queued for configured server.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
    }

    @Test
    fun manualSyncWhenSyncOnWithLoopbackHttpLaunchesWorkerForLocalDevelopment() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setSyncEnabled(true)
        settings.persistRegisteredDevice(
            deviceId = "android-device-1",
            deviceToken = "device-token-secret"
        )
        val activity = launchSettingsFragment()

        activity.findViewById<EditText>(R.id.syncServerUrlEditText)
            .setText("  http://localhost:5080  ")
        activity.findViewById<EditText>(R.id.syncDeviceIdEditText)
            .setText(" android-device-1 ")
        activity.findViewById<Button>(R.id.manualSyncButton).performClick()

        assertEquals(
            listOf(
                ManualSyncLaunchRequest(
                    baseUrl = "http://localhost:5080",
                    deviceId = "android-device-1",
                    pendingLimit = 50
                )
            ),
            launcher.requests
        )
        assertEquals(
            "Manual sync queued for configured server.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
    }

    private fun launchSettingsFragment(): AppCompatActivity {
        val activity = Robolectric.buildActivity(AppCompatActivity::class.java)
            .setup()
            .get()
        activity.setTheme(R.style.Theme_WoongMonitor)
        val container = FrameLayout(activity).apply { id = ViewId }
        activity.setContentView(container)
        activity.supportFragmentManager
            .beginTransaction()
            .replace(ViewId, SettingsFragment())
            .commitNow()
        return activity
    }

    private fun findButtonByText(
        activity: AppCompatActivity,
        text: String
    ): Button {
        return findButtonByText(activity.window.decorView, text)
            ?: throw AssertionError("Button with text '$text' was not found.")
    }

    private fun findButtonByText(
        view: android.view.View,
        text: String
    ): Button? {
        if (view is Button && view.text.toString() == text) {
            return view
        }
        if (view is android.view.ViewGroup) {
            for (index in 0 until view.childCount) {
                val match = findButtonByText(view.getChildAt(index), text)
                if (match != null) {
                    return match
                }
            }
        }
        return null
    }

    private class RecordingManualSyncLauncher : SettingsFragment.ManualSyncLauncher {
        val requests = mutableListOf<ManualSyncLaunchRequest>()

        override fun enqueue(
            baseUrl: String,
            deviceId: String,
            pendingLimit: Int
        ) {
            requests += ManualSyncLaunchRequest(baseUrl, deviceId, pendingLimit)
        }
    }

    private data class ManualSyncLaunchRequest(
        val baseUrl: String,
        val deviceId: String,
        val pendingLimit: Int
    )

    private class RecordingDeviceRegistrationLauncher : SettingsFragment.DeviceRegistrationLauncher {
        val requests = mutableListOf<SettingsFragment.DeviceRegistrationRequest>()
        var response = DeviceRegistrationResult(
            deviceId = "server-device-id",
            deviceToken = "device-token-secret"
        )

        override fun register(
            request: SettingsFragment.DeviceRegistrationRequest,
            callback: (Result<DeviceRegistrationResult>) -> Unit
        ) {
            requests += request
            callback(Result.success(response))
        }
    }

    private class FakeAndroidSyncTokenStore : AndroidSyncTokenStore {
        private var token = ""

        override fun deviceToken(): String = token

        override fun saveDeviceToken(deviceToken: String) {
            token = deviceToken.trim()
        }

        override fun clearDeviceToken() {
            token = ""
        }
    }

    companion object {
        private const val ViewId = 42_001
    }
}
