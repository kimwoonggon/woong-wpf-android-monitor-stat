package com.woong.monitorstack.settings

import android.content.Context

interface AndroidUsageCollectionSettings {
    fun isCollectionEnabled(): Boolean
}

class SharedPreferencesAndroidUsageCollectionSettings(
    context: Context
) : AndroidUsageCollectionSettings {
    private val preferences = context.applicationContext.getSharedPreferences(
        PreferenceName,
        Context.MODE_PRIVATE
    )

    override fun isCollectionEnabled(): Boolean {
        return preferences.getBoolean(KeyCollectionEnabled, true)
    }

    fun setCollectionEnabled(enabled: Boolean) {
        preferences.edit().putBoolean(KeyCollectionEnabled, enabled).apply()
    }

    companion object {
        const val PreferenceName = "woong_monitor_settings"
        private const val KeyCollectionEnabled = "usage_collection_enabled"
    }
}
