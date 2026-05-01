package com.woong.monitorstack.settings

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class SharedPreferencesAndroidSyncSettingsTest {
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
}
