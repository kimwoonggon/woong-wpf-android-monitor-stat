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
class SharedPreferencesAndroidUsageCollectionSettingsTest {
    @Test
    fun collectionEnabledDefaultsToFalseAndPersistsTrue() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        context.getSharedPreferences(
            SharedPreferencesAndroidUsageCollectionSettings.PreferenceName,
            Context.MODE_PRIVATE
        ).edit().clear().commit()
        val settings = SharedPreferencesAndroidUsageCollectionSettings(context)

        assertFalse(settings.isCollectionEnabled())

        settings.setCollectionEnabled(true)

        assertTrue(SharedPreferencesAndroidUsageCollectionSettings(context).isCollectionEnabled())
    }
}
