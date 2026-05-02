package com.woong.monitorstack.settings

import android.content.Context
import android.widget.Button
import android.widget.EditText
import android.widget.FrameLayout
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.R
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.data.local.SyncOutboxEntity
import com.woong.monitorstack.data.local.SyncOutboxStatus
import com.woong.monitorstack.sync.AndroidSyncAuthenticationException
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
import java.util.concurrent.FutureTask

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class SettingsFragmentManualSyncTest {
    private lateinit var context: Context
    private lateinit var launcher: RecordingManualSyncLauncher
    private lateinit var registrationLauncher: RecordingDeviceRegistrationLauncher
    private lateinit var disconnectLauncher: RecordingDeviceDisconnectLauncher
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
        disconnectLauncher = RecordingDeviceDisconnectLauncher()
        SettingsFragment.manualSyncLauncherFactory = { launcher }
        SettingsFragment.deviceRegistrationLauncherFactory = { registrationLauncher }
        SettingsFragment.deviceDisconnectLauncherFactory = { disconnectLauncher }
        SettingsFragment.usageAccessStatusReaderFactory = {
            FakeUsageAccessStatusReader(hasUsageAccess = false)
        }
    }

    @After
    fun tearDown() {
        SharedPreferencesAndroidSyncSettings.tokenStoreFactory =
            SharedPreferencesAndroidSyncSettings.defaultTokenStoreFactory()
        SettingsFragment.manualSyncLauncherFactory = SettingsFragment.defaultManualSyncLauncherFactory()
        SettingsFragment.deviceRegistrationLauncherFactory =
            SettingsFragment.defaultDeviceRegistrationLauncherFactory()
        SettingsFragment.deviceDisconnectLauncherFactory =
            SettingsFragment.defaultDeviceDisconnectLauncherFactory()
        SettingsFragment.usageAccessStatusReaderFactory =
            SettingsFragment.defaultUsageAccessStatusReaderFactory()
    }

    @Test
    fun usageAccessStatusReflectsCurrentPermissionState() {
        SettingsFragment.usageAccessStatusReaderFactory = {
            FakeUsageAccessStatusReader(hasUsageAccess = false)
        }
        val missingActivity = launchSettingsFragment()

        assertEquals(
            "Usage Access permission: Missing",
            missingActivity.findViewById<TextView>(R.id.usageAccessStatusRow).text.toString()
        )

        SettingsFragment.usageAccessStatusReaderFactory = {
            FakeUsageAccessStatusReader(hasUsageAccess = true)
        }
        val grantedActivity = launchSettingsFragment()

        assertEquals(
            "Usage Access permission: Granted",
            grantedActivity.findViewById<TextView>(R.id.usageAccessStatusRow).text.toString()
        )
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
    fun disconnectWhenSyncOffClearsLocalDeviceStateAndKeepsSyncOff() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.persistRegisteredDevice(
            deviceId = "android-device-1",
            deviceToken = "device-token-secret"
        )
        val activity = launchSettingsFragment()

        findButtonByText(activity, "Disconnect device").performClick()

        val reloaded = SharedPreferencesAndroidSyncSettings(context)
        assertFalse(reloaded.isSyncEnabled())
        assertEquals("", reloaded.deviceId())
        assertEquals("", reloaded.deviceToken())
        assertEquals(
            "Device disconnected locally. Sync is off and data remains on this Android device.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
        assertEquals(
            "Device not registered. Register / repair is available after sync is turned on.",
            activity.findViewById<TextView>(R.id.syncDeviceRegistrationStatusText).text.toString()
        )
        assertEquals(emptyList<ManualSyncLaunchRequest>(), launcher.requests)
    }

    @Test
    fun disconnectWhenSyncOnRevokesTokenThenClearsLocalRegistrationState() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setSyncEnabled(true)
        settings.setServerBaseUrl("https://server.example")
        settings.persistRegisteredDevice(
            deviceId = "android-device-1",
            deviceToken = "device-token-secret"
        )
        val activity = launchSettingsFragment()

        findButtonByText(activity, "Disconnect device").performClick()

        assertEquals(
            listOf(
                DeviceDisconnectRequest(
                    baseUrl = "https://server.example",
                    deviceId = "android-device-1",
                    deviceToken = "device-token-secret"
                )
            ),
            disconnectLauncher.requests
        )
        val reloaded = SharedPreferencesAndroidSyncSettings(context)
        assertFalse(reloaded.isSyncEnabled())
        assertEquals("", reloaded.deviceId())
        assertEquals("", reloaded.deviceToken())
        assertEquals(
            "Device disconnected. Sync is off and local usage data remains on this Android device.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
        assertEquals(emptyList<ManualSyncLaunchRequest>(), launcher.requests)
    }

    @Test
    fun disconnectRevokeFailureKeepsLocalRegistrationAndShowsSafeFailureStatus() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setSyncEnabled(true)
        settings.setServerBaseUrl("https://server.example")
        settings.persistRegisteredDevice(
            deviceId = "android-device-1",
            deviceToken = "device-token-secret"
        )
        disconnectLauncher.result = Result.failure(IllegalStateException("revoked elsewhere"))
        val activity = launchSettingsFragment()

        findButtonByText(activity, "Disconnect device").performClick()

        val reloaded = SharedPreferencesAndroidSyncSettings(context)
        assertTrue(reloaded.isSyncEnabled())
        assertEquals("android-device-1", reloaded.deviceId())
        assertEquals("device-token-secret", reloaded.deviceToken())
        assertEquals(
            "Device disconnect failed. Local registration is unchanged and usage data remains local.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
        assertEquals(
            "Device registered for sync.",
            activity.findViewById<TextView>(R.id.syncDeviceRegistrationStatusText).text.toString()
        )
        assertEquals(emptyList<ManualSyncLaunchRequest>(), launcher.requests)
    }

    @Test
    fun disconnectAuthFailureKeepsRegistrationMarksRepairRequiredAndPreservesPendingOutbox() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setSyncEnabled(true)
        settings.setServerBaseUrl("https://server.example")
        settings.persistRegisteredDevice(
            deviceId = "android-device-1",
            deviceToken = "expired-device-token"
        )
        disconnectLauncher.result = Result.failure(
            AndroidSyncAuthenticationException(
                statusCode = 401,
                message = "Device token revocation failed with HTTP 401."
            )
        )
        runDatabaseTask {
            val database = MonitorDatabase.getInstance(context)
            database.clearAllTables()
            database.syncOutboxDao().insert(
                SyncOutboxEntity(
                    clientItemId = "pending-focus-outbox",
                    aggregateType = "focus_session",
                    payloadJson = """{"clientSessionId":"focus-session-1"}""",
                    status = SyncOutboxStatus.Pending,
                    retryCount = 0,
                    lastError = null,
                    createdAtUtcMillis = 1_000L,
                    updatedAtUtcMillis = 1_000L
                )
            )
        }
        val activity = launchSettingsFragment()

        findButtonByText(activity, "Disconnect device").performClick()

        val reloaded = SharedPreferencesAndroidSyncSettings(context)
        assertTrue(reloaded.isSyncEnabled())
        assertEquals("android-device-1", reloaded.deviceId())
        assertEquals("expired-device-token", reloaded.deviceToken())
        assertEquals(AndroidSyncWorker.STATUS_AUTH_REQUIRED, reloaded.lastSyncStatus())
        assertEquals(
            "Device disconnect needs Register / repair. Local registration and pending data are unchanged.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
        assertEquals(
            "Sync authorization failed. Register / repair this device before syncing again.",
            activity.findViewById<TextView>(R.id.syncDeviceRegistrationStatusText).text.toString()
        )
        assertEquals(emptyList<ManualSyncLaunchRequest>(), launcher.requests)
        runDatabaseTask {
            val pending = MonitorDatabase.getInstance(context)
                .syncOutboxDao()
                .queryPending(10)
            assertEquals(1, pending.size)
            assertEquals("pending-focus-outbox", pending.single().clientItemId)
            assertEquals(SyncOutboxStatus.Pending, pending.single().status)
        }
    }

    @Test
    fun backgroundCollectionSwitchLoadsAndPersistsCollectionSetting() {
        SharedPreferencesAndroidUsageCollectionSettings(context)
            .setCollectionEnabled(false)
        val activity = launchSettingsFragment()
        val backgroundCollectionSwitch = activity.findViewById<SwitchMaterial>(
            R.id.backgroundCollectionSwitch
        )

        assertFalse(backgroundCollectionSwitch.isChecked)

        backgroundCollectionSwitch.performClick()

        assertTrue(SharedPreferencesAndroidUsageCollectionSettings(context).isCollectionEnabled())

        backgroundCollectionSwitch.performClick()

        assertFalse(SharedPreferencesAndroidUsageCollectionSettings(context).isCollectionEnabled())
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
    fun loopbackHttpEndpointShowsLocalDevelopmentNonProductionLabel() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setServerBaseUrl("http://10.0.2.2:5080")

        val activity = launchSettingsFragment()

        assertEquals(
            "Local development endpoint. Not for production sync.",
            activity.findViewById<TextView>(R.id.syncLocalDevelopmentEndpointText).text.toString()
        )
        assertTrue(activity.findViewById<TextView>(R.id.syncLocalDevelopmentEndpointText).isShown)
        assertFalse(activity.findViewById<SwitchMaterial>(R.id.autoSyncSwitch).isChecked)
    }

    @Test
    fun httpsProductionEndpointDoesNotShowLocalDevelopmentLabel() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setServerBaseUrl("https://sync.example")

        val activity = launchSettingsFragment()

        assertEquals(
            "",
            activity.findViewById<TextView>(R.id.syncLocalDevelopmentEndpointText).text.toString()
        )
        assertFalse(activity.findViewById<TextView>(R.id.syncLocalDevelopmentEndpointText).isShown)
        assertFalse(activity.findViewById<SwitchMaterial>(R.id.autoSyncSwitch).isChecked)
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
    fun manualSyncWhenAuthRequiredDoesNotLaunchWorkerUntilRegisterRepairSucceeds() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setSyncEnabled(true)
        settings.setServerBaseUrl("https://server.example")
        settings.persistRegisteredDevice(
            deviceId = "old-device-id",
            deviceToken = "expired-device-token"
        )
        settings.recordSyncStatus(
            status = AndroidSyncWorker.STATUS_AUTH_REQUIRED,
            message = "Android sync authorization failed. Register this device again."
        )
        val activity = launchSettingsFragment()

        activity.findViewById<Button>(R.id.manualSyncButton).performClick()

        assertEquals(
            "Manual sync needs Register / repair before upload can run.",
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
    fun registerRepairSuccessReplacesExpiredDeviceAndTokenAndClearsAuthRequiredState() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setSyncEnabled(true)
        settings.persistRegisteredDevice(
            deviceId = "old-device-id",
            deviceToken = "expired-device-token"
        )
        settings.recordSyncStatus(
            status = AndroidSyncWorker.STATUS_AUTH_REQUIRED,
            message = "Android sync authorization failed. Register this device again."
        )
        registrationLauncher.response = DeviceRegistrationResult(
            deviceId = "new-device-id",
            deviceToken = "replacement-device-token"
        )
        val activity = launchSettingsFragment()

        activity.findViewById<EditText>(R.id.syncServerUrlEditText)
            .setText("https://server.example")
        findButtonByText(activity, "Register / repair device").performClick()

        val reloaded = SharedPreferencesAndroidSyncSettings(context)
        assertEquals("new-device-id", reloaded.deviceId())
        assertEquals("replacement-device-token", reloaded.deviceToken())
        assertEquals("", reloaded.lastSyncStatus())
        assertEquals(
            "new-device-id",
            activity.findViewById<EditText>(R.id.syncDeviceIdEditText).text.toString()
        )
        assertEquals(emptyList<ManualSyncLaunchRequest>(), launcher.requests)
    }

    @Test
    fun registerRepairFailureKeepsExpiredDeviceTokenAndPendingOutboxSafe() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setSyncEnabled(true)
        settings.setServerBaseUrl("https://server.example")
        settings.persistRegisteredDevice(
            deviceId = "old-device-id",
            deviceToken = "expired-device-token"
        )
        settings.recordSyncStatus(
            status = AndroidSyncWorker.STATUS_AUTH_REQUIRED,
            message = "Android sync authorization failed. Register this device again."
        )
        registrationLauncher.result = Result.failure(IllegalStateException("server offline"))
        runDatabaseTask {
            val database = MonitorDatabase.getInstance(context)
            database.clearAllTables()
            database.syncOutboxDao().insert(
                SyncOutboxEntity(
                    clientItemId = "pending-focus-outbox",
                    aggregateType = "focus_session",
                    payloadJson = """{"clientSessionId":"focus-session-1"}""",
                    status = SyncOutboxStatus.Pending,
                    retryCount = 0,
                    lastError = null,
                    createdAtUtcMillis = 1_000L,
                    updatedAtUtcMillis = 1_000L
                )
            )
        }
        val activity = launchSettingsFragment()

        findButtonByText(activity, "Register / repair device").performClick()

        val reloaded = SharedPreferencesAndroidSyncSettings(context)
        assertEquals("old-device-id", reloaded.deviceId())
        assertEquals("expired-device-token", reloaded.deviceToken())
        assertEquals(AndroidSyncWorker.STATUS_AUTH_REQUIRED, reloaded.lastSyncStatus())
        assertEquals(
            "Device registration failed. Existing registration and pending local data are unchanged.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
        assertEquals(
            "Sync authorization failed. Register / repair this device before syncing again.",
            activity.findViewById<TextView>(R.id.syncDeviceRegistrationStatusText).text.toString()
        )
        assertEquals(emptyList<ManualSyncLaunchRequest>(), launcher.requests)
        runDatabaseTask {
            val pending = MonitorDatabase.getInstance(context)
                .syncOutboxDao()
                .queryPending(10)
            assertEquals(1, pending.size)
            assertEquals("pending-focus-outbox", pending.single().clientItemId)
            assertEquals(SyncOutboxStatus.Pending, pending.single().status)
        }
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

    private fun <T> runDatabaseTask(block: () -> T): T {
        val task = FutureTask<T> { block() }
        Thread(task).also { thread ->
            thread.start()
            thread.join()
        }
        return task.get()
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
        var result: Result<DeviceRegistrationResult>? = null

        override fun register(
            request: SettingsFragment.DeviceRegistrationRequest,
            callback: (Result<DeviceRegistrationResult>) -> Unit
        ) {
            requests += request
            callback(result ?: Result.success(response))
        }
    }

    private class RecordingDeviceDisconnectLauncher : SettingsFragment.DeviceDisconnectLauncher {
        val requests = mutableListOf<DeviceDisconnectRequest>()
        var result: Result<Unit> = Result.success(Unit)

        override fun disconnect(
            request: DeviceDisconnectRequest,
            callback: (Result<Unit>) -> Unit
        ) {
            requests += request
            callback(result)
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

    private class FakeUsageAccessStatusReader(
        private val hasUsageAccess: Boolean
    ) : SettingsFragment.UsageAccessStatusReader {
        override fun hasUsageAccess(packageName: String): Boolean = hasUsageAccess
    }

    companion object {
        private const val ViewId = 42_001
    }
}
