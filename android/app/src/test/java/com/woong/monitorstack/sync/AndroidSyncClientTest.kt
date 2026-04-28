package com.woong.monitorstack.sync

import okhttp3.Interceptor
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Response
import okhttp3.ResponseBody.Companion.toResponseBody
import okio.Buffer
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

class AndroidSyncClientTest {
    @Test
    fun uploadFocusSessionsPostsContractPayloadAndParsesBatchResult() {
        val interceptor = CapturingInterceptor(
            responseJson = """{"items":[{"clientId":"session-1","status":1,"errorMessage":null}]}"""
        )
        val httpClient = OkHttpClient.Builder()
            .addInterceptor(interceptor)
            .build()
        val syncClient = AndroidSyncClient(
            baseUrl = "https://server.example",
            httpClient = httpClient
        )

        val result = syncClient.uploadFocusSessions(
            SyncFocusSessionUploadRequest(
                deviceId = "device-1",
                sessions = listOf(
                    SyncFocusSessionUploadItem(
                        clientSessionId = "session-1",
                        platformAppKey = "com.android.chrome",
                        startedAtUtc = "2026-04-28T00:00:00Z",
                        endedAtUtc = "2026-04-28T00:15:00Z",
                        durationMs = 900_000,
                        localDate = "2026-04-28",
                        timezoneId = "Asia/Seoul",
                        isIdle = false,
                        source = "usage_stats"
                    )
                )
            )
        )

        assertEquals("/api/focus-sessions/upload", interceptor.path)
        assertTrue(interceptor.body.contains(""""deviceId":"device-1""""))
        assertTrue(interceptor.body.contains(""""clientSessionId":"session-1""""))
        assertEquals("session-1", result.items.single().clientId)
        assertEquals(SyncUploadItemStatus.Accepted, result.items.single().status)
    }

    private class CapturingInterceptor(
        private val responseJson: String
    ) : Interceptor {
        var path: String? = null
            private set

        var body: String = ""
            private set

        override fun intercept(chain: Interceptor.Chain): Response {
            val request = chain.request()
            val buffer = Buffer()
            request.body?.writeTo(buffer)
            path = request.url.encodedPath
            body = buffer.readUtf8()

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
