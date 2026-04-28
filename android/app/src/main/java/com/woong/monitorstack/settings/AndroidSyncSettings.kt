package com.woong.monitorstack.settings

import android.content.Context

interface AndroidSyncSettings {
    fun isSyncEnabled(): Boolean
}

class SharedPreferencesAndroidSyncSettings(context: Context) : AndroidSyncSettings {
    private val preferences = context.applicationContext.getSharedPreferences(
        PreferenceName,
        Context.MODE_PRIVATE
    )

    override fun isSyncEnabled(): Boolean {
        return preferences.getBoolean(KeySyncEnabled, false)
    }

    fun setSyncEnabled(enabled: Boolean) {
        preferences.edit().putBoolean(KeySyncEnabled, enabled).apply()
    }

    companion object {
        const val PreferenceName = "woong_monitor_settings"
        private const val KeySyncEnabled = "sync_enabled"
    }
}
