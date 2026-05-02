package com.woong.monitorstack.settings

import android.content.Context
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class AndroidKeystoreSyncTokenStoreInstrumentedTest {
    private lateinit var context: Context

    @Before
    fun setUp() {
        context = InstrumentationRegistry.getInstrumentation().targetContext.applicationContext
        ordinarySettingsPreferences().edit().clear().commit()
        AndroidKeystoreSyncTokenStore(context).clearDeviceToken()
    }

    @Test
    fun savesReadsAndClearsDeviceTokenWithoutPlaintextOrdinarySettingsEntry() {
        val store = AndroidKeystoreSyncTokenStore(context)

        store.saveDeviceToken("  connected-device-token-secret  ")

        assertEquals("connected-device-token-secret", AndroidKeystoreSyncTokenStore(context).deviceToken())
        assertFalse(ordinarySettingsPreferences().contains("device_token"))
        assertFalse(ordinarySettingsPreferences().all.values.contains("connected-device-token-secret"))

        store.clearDeviceToken()

        assertEquals("", AndroidKeystoreSyncTokenStore(context).deviceToken())
        assertFalse(ordinarySettingsPreferences().contains("device_token"))
        assertFalse(ordinarySettingsPreferences().all.values.contains("connected-device-token-secret"))
    }

    private fun ordinarySettingsPreferences() = context.getSharedPreferences(
        SharedPreferencesAndroidSyncSettings.PreferenceName,
        Context.MODE_PRIVATE
    )
}
