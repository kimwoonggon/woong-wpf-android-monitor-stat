package com.woong.monitorstack.sync

import okhttp3.Interceptor
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Response
import okhttp3.ResponseBody.Companion.toResponseBody
import okio.Buffer
import org.junit.Assert.assertEquals
import org.junit.Assert.assertThrows
import org.junit.Assert.assertTrue
import org.junit.Test

class AndroidSyncClientTest {
    @Test
    fun registerDevicePostsServerContractPayloadAndParsesDeviceToken() {
        val interceptor = CapturingInterceptor(
            responseJson = """
                {
                  "deviceId":"android-device-id",
                  "userId":"user-1",
                  "platform":"android",
                  "deviceKey":"android-device-key",
                  "deviceName":"Pixel",
                  "timezoneId":"Asia/Seoul",
                  "deviceToken":"device-token-secret",
                  "createdAtUtc":"2026-05-01T00:00:00Z",
                  "lastSeenAtUtc":"2026-05-01T00:00:00Z",
                  "isNew":true
                }
            """.trimIndent()
        )
        val httpClient = OkHttpClient.Builder()
            .addInterceptor(interceptor)
            .build()
        val syncClient = AndroidSyncClient(
            baseUrl = "https://server.example",
            httpClient = httpClient
        )

        val result = syncClient.registerDevice(
            SyncDeviceRegistrationRequest(
                userId = "user-1",
                platform = 2,
                deviceKey = "android-device-key",
                deviceName = "Pixel",
                timezoneId = "Asia/Seoul"
            )
        )

        assertEquals("/api/devices/register", interceptor.path)
        assertTrue(interceptor.body.contains(""""userId":"user-1""""))
        assertTrue(interceptor.body.contains(""""platform":2"""))
        assertTrue(interceptor.body.contains(""""deviceKey":"android-device-key""""))
        assertTrue(interceptor.body.contains(""""deviceName":"Pixel""""))
        assertTrue(interceptor.body.contains(""""timezoneId":"Asia/Seoul""""))
        assertEquals("android-device-id", result.deviceId)
        assertEquals("device-token-secret", result.deviceToken)
    }

    @Test
    fun uploadFocusSessionsClassifiesUnauthorizedAsAuthenticationFailure() {
        val interceptor = CapturingInterceptor(responseJson = "", responseCode = 401)
        val httpClient = OkHttpClient.Builder()
            .addInterceptor(interceptor)
            .build()
        val syncClient = AndroidSyncClient(
            baseUrl = "https://server.example",
            httpClient = httpClient,
            deviceToken = "expired-token"
        )

        val exception = assertThrows(AndroidSyncAuthenticationException::class.java) {
            syncClient.uploadFocusSessions(focusSessionUploadRequest())
        }

        assertEquals(401, exception.statusCode)
    }

    @Test
    fun uploadLocationContextsClassifiesForbiddenAsAuthenticationFailure() {
        val interceptor = CapturingInterceptor(responseJson = "", responseCode = 403)
        val httpClient = OkHttpClient.Builder()
            .addInterceptor(interceptor)
            .build()
        val syncClient = AndroidSyncClient(
            baseUrl = "https://server.example",
            httpClient = httpClient,
            deviceToken = "forbidden-token"
        )

        val exception = assertThrows(AndroidSyncAuthenticationException::class.java) {
            syncClient.uploadLocationContexts(locationContextUploadRequest())
        }

        assertEquals(403, exception.statusCode)
    }

    @Test
    fun uploadFocusSessionsClassifiesBadRequestAsValidationFailure() {
        val interceptor = CapturingInterceptor(responseJson = "", responseCode = 400)
        val httpClient = OkHttpClient.Builder()
            .addInterceptor(interceptor)
            .build()
        val syncClient = AndroidSyncClient(
            baseUrl = "https://server.example",
            httpClient = httpClient,
            deviceToken = "device-token-secret"
        )

        val exception = assertThrows(AndroidSyncValidationException::class.java) {
            syncClient.uploadFocusSessions(focusSessionUploadRequest())
        }

        assertEquals(400, exception.statusCode)
    }

    @Test
    fun uploadLocationContextsClassifiesUnprocessableEntityAsValidationFailure() {
        val interceptor = CapturingInterceptor(responseJson = "", responseCode = 422)
        val httpClient = OkHttpClient.Builder()
            .addInterceptor(interceptor)
            .build()
        val syncClient = AndroidSyncClient(
            baseUrl = "https://server.example",
            httpClient = httpClient,
            deviceToken = "device-token-secret"
        )

        val exception = assertThrows(AndroidSyncValidationException::class.java) {
            syncClient.uploadLocationContexts(locationContextUploadRequest())
        }

        assertEquals(422, exception.statusCode)
    }

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
            httpClient = httpClient,
            deviceToken = "device-token-secret"
        )

        val result = syncClient.uploadFocusSessions(focusSessionUploadRequest())

        assertEquals("/api/focus-sessions/upload", interceptor.path)
        assertEquals("device-token-secret", interceptor.deviceTokenHeader)
        assertTrue(interceptor.body.contains(""""deviceId":"device-1""""))
        assertTrue(interceptor.body.contains(""""clientSessionId":"session-1""""))
        assertEquals("session-1", result.items.single().clientId)
        assertEquals(SyncUploadItemStatus.Accepted, result.items.single().status)
    }

    @Test
    fun uploadLocationContextsPostsServerContractPayloadAndParsesBatchResult() {
        val interceptor = CapturingInterceptor(
            responseJson = """{"items":[{"clientId":"location-1","status":1,"errorMessage":null}]}"""
        )
        val httpClient = OkHttpClient.Builder()
            .addInterceptor(interceptor)
            .build()
        val syncClient = AndroidSyncClient(
            baseUrl = "https://server.example",
            httpClient = httpClient,
            deviceToken = "device-token-secret"
        )

        val result = syncClient.uploadLocationContexts(locationContextUploadRequest())

        assertEquals("/api/location-contexts/upload", interceptor.path)
        assertEquals("device-token-secret", interceptor.deviceTokenHeader)
        assertTrue(interceptor.body.contains(""""deviceId":"android-device-1""""))
        assertTrue(interceptor.body.contains(""""contexts":["""))
        assertTrue(interceptor.body.contains(""""clientContextId":"location-1""""))
        assertTrue(interceptor.body.contains(""""capturedAtUtc":"2026-04-28T00:00:00Z""""))
        assertTrue(interceptor.body.contains(""""localDate":"2026-04-28""""))
        assertTrue(interceptor.body.contains(""""timezoneId":"Asia/Seoul""""))
        assertTrue(interceptor.body.contains(""""latitude":37.5665"""))
        assertTrue(interceptor.body.contains(""""longitude":126.978"""))
        assertTrue(interceptor.body.contains(""""accuracyMeters":35.5"""))
        assertTrue(interceptor.body.contains(""""captureMode":"AppUsageContext""""))
        assertTrue(interceptor.body.contains(""""permissionState":"GrantedApproximate""""))
        assertTrue(interceptor.body.contains(""""source":"android_location_context""""))
        assertEquals("location-1", result.items.single().clientId)
        assertEquals(SyncUploadItemStatus.Accepted, result.items.single().status)
    }

    private class CapturingInterceptor(
        private val responseJson: String,
        private val responseCode: Int = 200
    ) : Interceptor {
        var path: String? = null
            private set

        var body: String = ""
            private set

        var deviceTokenHeader: String? = null
            private set

        override fun intercept(chain: Interceptor.Chain): Response {
            val request = chain.request()
            val buffer = Buffer()
            request.body?.writeTo(buffer)
            path = request.url.encodedPath
            body = buffer.readUtf8()
            deviceTokenHeader = request.header("X-Device-Token")

            return Response.Builder()
                .request(request)
                .protocol(okhttp3.Protocol.HTTP_1_1)
                .code(responseCode)
                .message("OK")
                .body(responseJson.toResponseBody("application/json".toMediaType()))
                .build()
        }
    }

    private fun focusSessionUploadRequest(): SyncFocusSessionUploadRequest {
        return SyncFocusSessionUploadRequest(
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
    }

    private fun locationContextUploadRequest(): SyncLocationContextUploadRequest {
        return SyncLocationContextUploadRequest(
            deviceId = "android-device-1",
            contexts = listOf(
                SyncLocationContextUploadItem(
                    clientContextId = "location-1",
                    capturedAtUtc = "2026-04-28T00:00:00Z",
                    localDate = "2026-04-28",
                    timezoneId = "Asia/Seoul",
                    latitude = 37.5665,
                    longitude = 126.9780,
                    accuracyMeters = 35.5f,
                    captureMode = "AppUsageContext",
                    permissionState = "GrantedApproximate",
                    source = "android_location_context"
                )
            )
        )
    }
}
