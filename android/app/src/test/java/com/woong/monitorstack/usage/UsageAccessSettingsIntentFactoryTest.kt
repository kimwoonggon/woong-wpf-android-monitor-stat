package com.woong.monitorstack.usage

import android.provider.Settings
import org.junit.Assert.assertEquals
import org.junit.Test

class UsageAccessSettingsIntentFactoryTest {
    @Test
    fun actionReturnsUsageAccessSettingsAction() {
        val factory = UsageAccessSettingsIntentFactory()

        assertEquals(Settings.ACTION_USAGE_ACCESS_SETTINGS, factory.action())
    }
}
