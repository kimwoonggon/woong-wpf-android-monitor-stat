package com.woong.monitorstack.settings

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.After
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class SharedPreferencesAndroidSyncSettingsTest {
    private lateinit var tokenStore: FakeAndroidSyncTokenStore

    @Before
    fun setUp() {
        tokenStore = FakeAndroidSyncTokenStore()
        SharedPreferencesAndroidSyncSettings.tokenStoreFactory = { tokenStore }
    }

    @After
    fun tearDown() {
        SharedPreferencesAndroidSyncSettings.tokenStoreFactory =
            SharedPreferencesAndroidSyncSettings.defaultTokenStoreFactory()
    }

    @Test
    fun syncEnabledDefaultsToFalseAndPersistsTrue() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        context.getSharedPreferences(
            SharedPreferencesAndroidSyncSettings.PreferenceName,
            Context.MODE_PRIVATE
        ).edit().clear().commit()
        val settings = SharedPreferencesAndroidSyncSettings(context)

        assertFalse(settings.isSyncEnabled())

        settings.setSyncEnabled(true)

        assertTrue(SharedPreferencesAndroidSyncSettings(context).isSyncEnabled())
    }

    @Test
    fun serverBaseUrlAndDeviceIdDefaultBlankPersistAndTrimWhitespace() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        context.getSharedPreferences(
            SharedPreferencesAndroidSyncSettings.PreferenceName,
            Context.MODE_PRIVATE
        ).edit().clear().commit()
        val settings = SharedPreferencesAndroidSyncSettings(context)

        assertEquals("", settings.serverBaseUrl())
        assertEquals("", settings.deviceId())
        assertFalse(settings.isSyncEnabled())

        settings.setServerBaseUrl("  https://api.example.test  ")
        settings.setDeviceId("\n android-phone-01\t")

        val reloaded = SharedPreferencesAndroidSyncSettings(context)
        assertEquals("https://api.example.test", reloaded.serverBaseUrl())
        assertEquals("android-phone-01", reloaded.deviceId())
        assertFalse(reloaded.isSyncEnabled())
    }

    @Test
    fun deviceTokenDefaultsBlankAndRegistrationPersistsDeviceIdAndToken() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        context.getSharedPreferences(
            SharedPreferencesAndroidSyncSettings.PreferenceName,
            Context.MODE_PRIVATE
        ).edit().clear().commit()
        val settings = SharedPreferencesAndroidSyncSettings(context)

        assertEquals("", settings.deviceToken())
        assertFalse(settings.isSyncEnabled())

        settings.persistRegisteredDevice(
            deviceId = " server-device-id ",
            deviceToken = "\n device-token-secret\t"
        )

        val reloaded = SharedPreferencesAndroidSyncSettings(context)
        assertEquals("server-device-id", reloaded.deviceId())
        assertEquals("device-token-secret", reloaded.deviceToken())
        assertFalse(reloaded.isSyncEnabled())
    }

    @Test
    fun registrationDoesNotStoreDeviceTokenInOrdinarySettingsPreferences() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val preferences = context.getSharedPreferences(
            SharedPreferencesAndroidSyncSettings.PreferenceName,
            Context.MODE_PRIVATE
        )
        preferences.edit().clear().commit()
        val settings = SharedPreferencesAndroidSyncSettings(context)

        settings.persistRegisteredDevice(
            deviceId = "server-device-id",
            deviceToken = "device-token-secret"
        )

        assertFalse(preferences.contains("device_token"))
        assertFalse(preferences.all.values.contains("device-token-secret"))
    }

    @Test
    fun legacyPlaintextDeviceTokenMigratesOutOfOrdinarySettingsPreferencesOnRead() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val preferences = context.getSharedPreferences(
            SharedPreferencesAndroidSyncSettings.PreferenceName,
            Context.MODE_PRIVATE
        )
        preferences.edit()
            .clear()
            .putString("device_token", " legacy-device-token ")
            .commit()
        val settings = SharedPreferencesAndroidSyncSettings(context)

        assertEquals("legacy-device-token", settings.deviceToken())
        assertFalse(preferences.contains("device_token"))
        assertFalse(preferences.all.values.contains(" legacy-device-token "))
    }

    @Test
    fun syncStatusPersistsForSettingsAndRegistrationClearsIt() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        context.getSharedPreferences(
            SharedPreferencesAndroidSyncSettings.PreferenceName,
            Context.MODE_PRIVATE
        ).edit().clear().commit()
        val settings = SharedPreferencesAndroidSyncSettings(context)

        settings.recordSyncStatus(
            status = "auth_required",
            message = "Register again."
        )

        assertEquals("auth_required", SharedPreferencesAndroidSyncSettings(context).lastSyncStatus())

        settings.persistRegisteredDevice(
            deviceId = "server-device-id",
            deviceToken = "device-token-secret"
        )

        assertEquals("", SharedPreferencesAndroidSyncSettings(context).lastSyncStatus())
    }

    @Test
    fun clearSyncConfigurationClearsServerUrlDeviceIdTokenAndDisablesSync() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        context.getSharedPreferences(
            SharedPreferencesAndroidSyncSettings.PreferenceName,
            Context.MODE_PRIVATE
        ).edit().clear().commit()
        val settings = SharedPreferencesAndroidSyncSettings(context)
        settings.setSyncEnabled(true)
        settings.setServerBaseUrl("https://server.example")
        settings.persistRegisteredDevice(
            deviceId = "server-device-id",
            deviceToken = "device-token-secret"
        )
        settings.recordSyncStatus(
            status = "auth_required",
            message = "Register again."
        )

        settings.clearSyncConfiguration()

        val reloaded = SharedPreferencesAndroidSyncSettings(context)
        assertFalse(reloaded.isSyncEnabled())
        assertEquals("", reloaded.serverBaseUrl())
        assertEquals("", reloaded.deviceId())
        assertEquals("", reloaded.deviceToken())
        assertEquals("", reloaded.lastSyncStatus())
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
}
