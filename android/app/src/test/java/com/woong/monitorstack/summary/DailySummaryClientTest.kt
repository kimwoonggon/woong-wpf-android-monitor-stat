package com.woong.monitorstack.summary

import okhttp3.Interceptor
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Response
import okhttp3.ResponseBody.Companion.toResponseBody
import org.junit.Assert.assertEquals
import org.junit.Test

class DailySummaryClientTest {
    @Test
    fun getDailySummaryCallsServerContractAndParsesUsageTotals() {
        val interceptor = CapturingInterceptor(
            responseJson = """
                {
                  "summaryDate": "2026-04-27",
                  "totalActiveMs": 900000,
                  "totalIdleMs": 120000,
                  "totalWebMs": 240000,
                  "topApps": [
                    { "key": "com.android.chrome", "durationMs": 600000 }
                  ],
                  "topDomains": [
                    { "key": "example.com", "durationMs": 240000 }
                  ]
                }
            """.trimIndent()
        )
        val client = DailySummaryClient(
            baseUrl = "https://server.example",
            httpClient = OkHttpClient.Builder()
                .addInterceptor(interceptor)
                .build()
        )

        val summary = client.getDailySummary(
            userId = "user-1",
            summaryDate = "2026-04-27",
            timezoneId = "Asia/Seoul"
        )

        assertEquals("/api/daily-summaries/2026-04-27", interceptor.path)
        assertEquals("user-1", interceptor.queryParameters["userId"])
        assertEquals("Asia/Seoul", interceptor.queryParameters["timezoneId"])
        assertEquals("2026-04-27", summary.summaryDate)
        assertEquals(900_000, summary.totalActiveMs)
        assertEquals(120_000, summary.totalIdleMs)
        assertEquals(240_000, summary.totalWebMs)
        assertEquals("com.android.chrome", summary.topApps.single().key)
        assertEquals(600_000, summary.topApps.single().durationMs)
        assertEquals("example.com", summary.topDomains.single().key)
        assertEquals(240_000, summary.topDomains.single().durationMs)
    }

    private class CapturingInterceptor(
        private val responseJson: String
    ) : Interceptor {
        var path: String? = null
            private set

        var queryParameters: Map<String, String?> = emptyMap()
            private set

        override fun intercept(chain: Interceptor.Chain): Response {
            val request = chain.request()
            path = request.url.encodedPath
            queryParameters = request.url.queryParameterNames.associateWith {
                request.url.queryParameter(it)
            }

            return Response.Builder()
                .request(request)
                .protocol(okhttp3.Protocol.HTTP_1_1)
                .code(200)
                .message("OK")
                .body(responseJson.toResponseBody("application/json".toMediaType()))
                .build()
        }
    }
}
