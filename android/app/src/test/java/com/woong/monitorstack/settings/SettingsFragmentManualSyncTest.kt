package com.woong.monitorstack.settings

import android.content.Context
import android.widget.Button
import android.widget.EditText
import android.widget.FrameLayout
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.R
import org.junit.After
import org.junit.Assert.assertEquals
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

    @Before
    fun setUp() {
        context = ApplicationProvider.getApplicationContext()
        context.getSharedPreferences(
            SharedPreferencesAndroidSyncSettings.PreferenceName,
            Context.MODE_PRIVATE
        ).edit().clear().commit()
        launcher = RecordingManualSyncLauncher()
        SettingsFragment.manualSyncLauncherFactory = { launcher }
    }

    @After
    fun tearDown() {
        SettingsFragment.manualSyncLauncherFactory = SettingsFragment.defaultManualSyncLauncherFactory()
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
    fun manualSyncWhenSyncOnWithValidConfigurationLaunchesWorkerWithStoredValues() {
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setSyncEnabled(true)
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

    companion object {
        private const val ViewId = 42_001
    }
}
