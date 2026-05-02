package com.woong.monitorstack.settings

import android.content.Context
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import java.security.KeyStore
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec

interface AndroidSyncTokenStore {
    fun deviceToken(): String
    fun saveDeviceToken(deviceToken: String)
    fun clearDeviceToken()
}

class AndroidKeystoreSyncTokenStore(context: Context) : AndroidSyncTokenStore {
    private val preferences = context.applicationContext.getSharedPreferences(
        PreferenceName,
        Context.MODE_PRIVATE
    )

    override fun deviceToken(): String {
        val encryptedToken = preferences.getString(KeyEncryptedDeviceToken, null)
            ?: return ""
        val initializationVector = preferences.getString(KeyInitializationVector, null)
            ?: return ""

        return runCatching {
            val cipher = Cipher.getInstance(CipherTransformation)
            cipher.init(
                Cipher.DECRYPT_MODE,
                secretKey(),
                GCMParameterSpec(AuthenticationTagBits, initializationVector.decodeBase64())
            )
            String(cipher.doFinal(encryptedToken.decodeBase64()), Charsets.UTF_8)
        }.getOrDefault("")
    }

    override fun saveDeviceToken(deviceToken: String) {
        val trimmedToken = deviceToken.trim()
        if (trimmedToken.isBlank()) {
            clearDeviceToken()
            return
        }

        val cipher = Cipher.getInstance(CipherTransformation)
        cipher.init(Cipher.ENCRYPT_MODE, secretKey())
        val encryptedToken = cipher.doFinal(trimmedToken.toByteArray(Charsets.UTF_8))

        preferences.edit()
            .putString(KeyEncryptedDeviceToken, encryptedToken.encodeBase64())
            .putString(KeyInitializationVector, cipher.iv.encodeBase64())
            .apply()
    }

    override fun clearDeviceToken() {
        preferences.edit()
            .remove(KeyEncryptedDeviceToken)
            .remove(KeyInitializationVector)
            .apply()
    }

    private fun secretKey(): SecretKey {
        val keyStore = KeyStore.getInstance(AndroidKeystoreProvider).apply {
            load(null)
        }
        if (!keyStore.containsAlias(KeyAlias)) {
            return generateSecretKey()
        }

        val entry = keyStore.getEntry(KeyAlias, null) as KeyStore.SecretKeyEntry
        return entry.secretKey
    }

    private fun generateSecretKey(): SecretKey {
        val keyGenerator = KeyGenerator.getInstance(
            KeyProperties.KEY_ALGORITHM_AES,
            AndroidKeystoreProvider
        )
        val keySpec = KeyGenParameterSpec.Builder(
            KeyAlias,
            KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT
        )
            .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
            .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
            .setRandomizedEncryptionRequired(true)
            .build()
        keyGenerator.init(keySpec)
        return keyGenerator.generateKey()
    }

    private fun ByteArray.encodeBase64(): String {
        return Base64.encodeToString(this, Base64.NO_WRAP)
    }

    private fun String.decodeBase64(): ByteArray {
        return Base64.decode(this, Base64.NO_WRAP)
    }

    private companion object {
        private const val PreferenceName = "woong_monitor_sync_token"
        private const val KeyEncryptedDeviceToken = "device_token_ciphertext"
        private const val KeyInitializationVector = "device_token_iv"
        private const val KeyAlias = "woong_monitor_android_sync_device_token"
        private const val AndroidKeystoreProvider = "AndroidKeyStore"
        private const val CipherTransformation = "AES/GCM/NoPadding"
        private const val AuthenticationTagBits = 128
    }
}
