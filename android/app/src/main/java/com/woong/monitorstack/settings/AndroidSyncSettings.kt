package com.woong.monitorstack.settings

import android.content.Context
import java.util.UUID

interface AndroidSyncSettings {
    fun isSyncEnabled(): Boolean
    fun serverBaseUrl(): String = ""
    fun deviceId(): String = ""
    fun deviceToken(): String = ""
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

    override fun serverBaseUrl(): String {
        return preferences.getString(KeyServerBaseUrl, "").orEmpty()
    }

    fun setServerBaseUrl(serverBaseUrl: String) {
        preferences.edit().putString(KeyServerBaseUrl, serverBaseUrl.trim()).apply()
    }

    override fun deviceId(): String {
        return preferences.getString(KeyDeviceId, "").orEmpty()
    }

    fun setDeviceId(deviceId: String) {
        preferences.edit().putString(KeyDeviceId, deviceId.trim()).apply()
    }

    fun deviceKey(): String {
        val existingDeviceKey = preferences.getString(KeyDeviceKey, "").orEmpty()
        if (existingDeviceKey.isNotBlank()) {
            return existingDeviceKey
        }

        val generatedDeviceKey = "android-${UUID.randomUUID()}"
        preferences.edit().putString(KeyDeviceKey, generatedDeviceKey).apply()
        return generatedDeviceKey
    }

    override fun deviceToken(): String {
        return preferences.getString(KeyDeviceToken, "").orEmpty()
    }

    fun persistRegisteredDevice(
        deviceId: String,
        deviceToken: String
    ) {
        preferences.edit()
            .putString(KeyDeviceId, deviceId.trim())
            .putString(KeyDeviceToken, deviceToken.trim())
            .apply()
    }

    fun clearSyncConfiguration() {
        preferences.edit()
            .putBoolean(KeySyncEnabled, false)
            .remove(KeyServerBaseUrl)
            .remove(KeyDeviceId)
            .remove(KeyDeviceToken)
            .apply()
    }

    companion object {
        const val PreferenceName = "woong_monitor_settings"
        private const val KeySyncEnabled = "sync_enabled"
        private const val KeyServerBaseUrl = "server_base_url"
        private const val KeyDeviceId = "device_id"
        private const val KeyDeviceKey = "device_key"
        private const val KeyDeviceToken = "device_token"
    }
}
