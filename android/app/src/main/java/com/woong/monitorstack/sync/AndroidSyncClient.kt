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
    private val httpClient: OkHttpClient = OkHttpClient()
) : AndroidSyncApi {
    private val normalizedBaseUrl = baseUrl.trimEnd('/')
    private val moshi = Moshi.Builder()
        .add(SyncUploadItemStatusJsonAdapter())
        .add(KotlinJsonAdapterFactory())
        .build()
    private val focusSessionRequestAdapter = moshi.adapter(SyncFocusSessionUploadRequest::class.java)
    private val locationContextRequestAdapter = moshi.adapter(SyncLocationContextUploadRequest::class.java)
    private val resultAdapter = moshi.adapter(SyncUploadBatchResult::class.java)

    init {
        require(normalizedBaseUrl.isNotBlank()) { "baseUrl must not be blank." }
    }

    override fun uploadFocusSessions(request: SyncFocusSessionUploadRequest): SyncUploadBatchResult {
        val httpRequest = Request.Builder()
            .url("$normalizedBaseUrl/api/focus-sessions/upload")
            .post(focusSessionRequestAdapter.toJson(request).toRequestBody(JsonMediaType))
            .build()

        httpClient.newCall(httpRequest).execute().use { response ->
            if (!response.isSuccessful) {
                throw IOException("Focus session upload failed with HTTP ${response.code}.")
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
            .build()

        httpClient.newCall(httpRequest).execute().use { response ->
            if (!response.isSuccessful) {
                throw IOException("Location context upload failed with HTTP ${response.code}.")
            }

            val responseJson = response.body.string()
            return resultAdapter.fromJson(responseJson)
                ?: throw IOException("Location context upload returned an empty response.")
        }
    }

    companion object {
        private val JsonMediaType = "application/json; charset=utf-8".toMediaType()
    }
}
