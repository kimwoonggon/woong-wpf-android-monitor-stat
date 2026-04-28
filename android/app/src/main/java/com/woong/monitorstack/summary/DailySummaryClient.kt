package com.woong.monitorstack.summary

import com.squareup.moshi.Moshi
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import java.io.IOException
import okhttp3.HttpUrl.Companion.toHttpUrl
import okhttp3.OkHttpClient
import okhttp3.Request

class DailySummaryClient(
    baseUrl: String,
    private val httpClient: OkHttpClient = OkHttpClient()
) {
    private val normalizedBaseUrl = baseUrl.trimEnd('/')
    private val moshi = Moshi.Builder()
        .add(KotlinJsonAdapterFactory())
        .build()
    private val responseAdapter = moshi.adapter(DailySummaryResponse::class.java)

    init {
        require(normalizedBaseUrl.isNotBlank()) { "baseUrl must not be blank." }
    }

    fun getDailySummary(
        userId: String,
        summaryDate: String,
        timezoneId: String
    ): DailySummaryResponse {
        val url = "$normalizedBaseUrl/api/daily-summaries/$summaryDate"
            .toHttpUrl()
            .newBuilder()
            .addQueryParameter("userId", userId)
            .addQueryParameter("timezoneId", timezoneId)
            .build()
        val request = Request.Builder()
            .url(url)
            .get()
            .build()

        httpClient.newCall(request).execute().use { response ->
            if (!response.isSuccessful) {
                throw IOException("Daily summary query failed with HTTP ${response.code}.")
            }

            val responseJson = response.body.string()
            return responseAdapter.fromJson(responseJson)
                ?: throw IOException("Daily summary query returned an empty response.")
        }
    }
}

data class DailySummaryResponse(
    val summaryDate: String,
    val totalActiveMs: Long,
    val totalIdleMs: Long,
    val totalWebMs: Long,
    val topApps: List<UsageTotalResponse>,
    val topDomains: List<UsageTotalResponse>
)

data class UsageTotalResponse(
    val key: String,
    val durationMs: Long
)
