package com.woong.monitorstack.settings

import android.content.Context
import androidx.test.core.app.ApplicationProvider
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
}
