package com.woong.monitorstack.sync

import com.squareup.moshi.Moshi
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import java.io.IOException
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody

class AndroidSyncClient(
    baseUrl: String,
    private val httpClient: OkHttpClient = OkHttpClient(),
    private val deviceToken: String = ""
) : AndroidSyncApi {
    private val normalizedBaseUrl = baseUrl.trimEnd('/')
    private val moshi = Moshi.Builder()
        .add(SyncUploadItemStatusJsonAdapter())
        .add(KotlinJsonAdapterFactory())
        .build()
    private val focusSessionRequestAdapter = moshi.adapter(SyncFocusSessionUploadRequest::class.java)
    private val locationContextRequestAdapter = moshi.adapter(SyncLocationContextUploadRequest::class.java)
    private val deviceRegistrationRequestAdapter =
        moshi.adapter(SyncDeviceRegistrationRequest::class.java)
    private val deviceRegistrationResponseAdapter =
        moshi.adapter(SyncDeviceRegistrationResponse::class.java)
    private val resultAdapter = moshi.adapter(SyncUploadBatchResult::class.java)

    init {
        require(normalizedBaseUrl.isNotBlank()) { "baseUrl must not be blank." }
    }

    fun registerDevice(request: SyncDeviceRegistrationRequest): SyncDeviceRegistrationResponse {
        val httpRequest = Request.Builder()
            .url("$normalizedBaseUrl/api/devices/register")
            .post(deviceRegistrationRequestAdapter.toJson(request).toRequestBody(JsonMediaType))
            .build()

        httpClient.newCall(httpRequest).execute().use { response ->
            if (!response.isSuccessful) {
                throw IOException("Device registration failed with HTTP ${response.code}.")
            }

            val responseJson = response.body.string()
            return deviceRegistrationResponseAdapter.fromJson(responseJson)
                ?: throw IOException("Device registration returned an empty response.")
        }
    }

    fun revokeDeviceToken(deviceId: String) {
        val httpRequest = Request.Builder()
            .url("$normalizedBaseUrl/api/devices/${deviceId.trim()}/token/revoke")
            .post(ByteArray(0).toRequestBody(JsonMediaType))
            .addDeviceTokenHeader()
            .build()

        httpClient.newCall(httpRequest).execute().use { response ->
            if (!response.isSuccessful) {
                throwUploadFailure(
                    operation = "Device token revocation",
                    statusCode = response.code
                )
            }
        }
    }

    override fun uploadFocusSessions(request: SyncFocusSessionUploadRequest): SyncUploadBatchResult {
        val httpRequest = Request.Builder()
            .url("$normalizedBaseUrl/api/focus-sessions/upload")
            .post(focusSessionRequestAdapter.toJson(request).toRequestBody(JsonMediaType))
            .addDeviceTokenHeader()
            .build()

        httpClient.newCall(httpRequest).execute().use { response ->
            if (!response.isSuccessful) {
                throwUploadFailure(
                    operation = "Focus session upload",
                    statusCode = response.code
                )
            }

            val responseJson = response.body.string()
            return resultAdapter.fromJson(responseJson)
                ?: throw IOException("Focus session upload returned an empty response.")
        }
    }

    override fun uploadLocationContexts(request: SyncLocationContextUploadRequest): SyncUploadBatchResult {
        val httpRequest = Request.Builder()
            .url("$normalizedBaseUrl/api/location-contexts/upload")
            .post(locationContextRequestAdapter.toJson(request).toRequestBody(JsonMediaType))
            .addDeviceTokenHeader()
            .build()

        httpClient.newCall(httpRequest).execute().use { response ->
            if (!response.isSuccessful) {
                throwUploadFailure(
                    operation = "Location context upload",
                    statusCode = response.code
                )
            }

            val responseJson = response.body.string()
            return resultAdapter.fromJson(responseJson)
                ?: throw IOException("Location context upload returned an empty response.")
        }
    }

    companion object {
        const val DeviceTokenHeaderName = "X-Device-Token"
        private val JsonMediaType = "application/json; charset=utf-8".toMediaType()
    }

    private fun Request.Builder.addDeviceTokenHeader(): Request.Builder {
        val trimmedDeviceToken = deviceToken.trim()
        return if (trimmedDeviceToken.isBlank()) {
            this
        } else {
            header(DeviceTokenHeaderName, trimmedDeviceToken)
        }
    }

    private fun throwUploadFailure(
        operation: String,
        statusCode: Int
    ): Nothing {
        val message = "$operation failed with HTTP $statusCode."
        if (statusCode == 401 || statusCode == 403) {
            throw AndroidSyncAuthenticationException(statusCode, message)
        }
        if (statusCode == 400 || statusCode == 422) {
            throw AndroidSyncValidationException(statusCode, message)
        }
        throw IOException(message)
    }
}
