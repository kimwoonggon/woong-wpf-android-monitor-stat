package com.woong.monitorstack.settings

import android.content.Context
import com.woong.monitorstack.BuildConfig
import java.util.UUID

interface AndroidSyncSettings {
    fun isSyncEnabled(): Boolean
    fun serverBaseUrl(): String = ""
    fun deviceId(): String = ""
    fun deviceToken(): String = ""
    fun lastSyncStatus(): String = ""
    fun recordSyncStatus(status: String, message: String) = Unit
    fun clearSyncStatus() = Unit
}

class SharedPreferencesAndroidSyncSettings @JvmOverloads constructor(
    context: Context,
    private val tokenStore: AndroidSyncTokenStore =
        tokenStoreFactory(context.applicationContext),
    private val productionSyncBaseUrl: String = BuildConfig.PRODUCTION_SYNC_BASE_URL
) : AndroidSyncSettings {
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
        val userEnteredBaseUrl = preferences.getString(KeyServerBaseUrl, "").orEmpty()
        if (userEnteredBaseUrl.isNotBlank()) {
            return userEnteredBaseUrl
        }

        return AndroidSyncServerUrlValidator.productionEndpointOrBlank(productionSyncBaseUrl)
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
        val token = tokenStore.deviceToken()
        if (token.isNotBlank()) {
            return token
        }

        val legacyPlaintextToken = preferences.getString(KeyDeviceToken, "").orEmpty().trim()
        if (legacyPlaintextToken.isBlank()) {
            return ""
        }

        tokenStore.saveDeviceToken(legacyPlaintextToken)
        preferences.edit().remove(KeyDeviceToken).apply()
        return legacyPlaintextToken
    }

    fun persistRegisteredDevice(
        deviceId: String,
        deviceToken: String
    ) {
        tokenStore.saveDeviceToken(deviceToken)
        preferences.edit()
            .putString(KeyDeviceId, deviceId.trim())
            .remove(KeyDeviceToken)
            .remove(KeyLastSyncStatus)
            .remove(KeyLastSyncMessage)
            .apply()
    }

    override fun lastSyncStatus(): String {
        return preferences.getString(KeyLastSyncStatus, "").orEmpty()
    }

    override fun recordSyncStatus(status: String, message: String) {
        preferences.edit()
            .putString(KeyLastSyncStatus, status.trim())
            .putString(KeyLastSyncMessage, message.trim())
            .apply()
    }

    override fun clearSyncStatus() {
        preferences.edit()
            .remove(KeyLastSyncStatus)
            .remove(KeyLastSyncMessage)
            .apply()
    }

    fun clearSyncConfiguration() {
        tokenStore.clearDeviceToken()
        preferences.edit()
            .putBoolean(KeySyncEnabled, false)
            .remove(KeyServerBaseUrl)
            .remove(KeyDeviceId)
            .remove(KeyDeviceToken)
            .remove(KeyLastSyncStatus)
            .remove(KeyLastSyncMessage)
            .apply()
    }

    companion object {
        const val PreferenceName = "woong_monitor_settings"
        private const val KeySyncEnabled = "sync_enabled"
        private const val KeyServerBaseUrl = "server_base_url"
        private const val KeyDeviceId = "device_id"
        private const val KeyDeviceKey = "device_key"
        private const val KeyDeviceToken = "device_token"
        private const val KeyLastSyncStatus = "last_sync_status"
        private const val KeyLastSyncMessage = "last_sync_message"

        fun defaultTokenStoreFactory(): (Context) -> AndroidSyncTokenStore = {
            AndroidKeystoreSyncTokenStore(it)
        }

        var tokenStoreFactory: (Context) -> AndroidSyncTokenStore =
            defaultTokenStoreFactory()
    }
}
