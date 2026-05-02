package com.woong.monitorstack.sync

import com.squareup.moshi.Moshi
import com.squareup.moshi.Types
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.LocationCaptureMode
import com.woong.monitorstack.data.local.LocationContextSnapshotEntity
import com.woong.monitorstack.data.local.LocationPermissionState
import com.woong.monitorstack.data.local.SyncOutboxEntity
import com.woong.monitorstack.data.local.SyncOutboxWriter
import com.woong.monitorstack.settings.AndroidLocationSettings
import com.woong.monitorstack.settings.AndroidSyncSettings
import com.woong.monitorstack.usage.FocusSessionSyncOutboxEnqueuer
import kotlinx.coroutines.runBlocking
import org.junit.Assert.assertTrue
import org.junit.Test

class AndroidSyncPayloadPrivacyTest {
    private val moshi = Moshi.Builder()
        .add(SyncUploadItemStatusJsonAdapter())
        .add(KotlinJsonAdapterFactory())
        .build()

    @Test
    fun focusSessionUploadPayloadExcludesBrowserContentAndInputCaptureFields() {
        val payloadJson = moshi.adapter(SyncFocusSessionUploadRequest::class.java)
            .toJson(
                SyncFocusSessionUploadRequest(
                    deviceId = "android-device-1",
                    sessions = listOf(
                        SyncFocusSessionUploadItem(
                            clientSessionId = "android:com.android.chrome:1000:61000",
                            platformAppKey = "com.android.chrome",
                            startedAtUtc = "1970-01-01T00:00:01Z",
                            endedAtUtc = "1970-01-01T00:01:01Z",
                            durationMs = 60_000L,
                            localDate = "1970-01-01",
                            timezoneId = "Asia/Seoul",
                            isIdle = false,
                            source = "android_usage_stats"
                        )
                    )
                )
            )

        assertNoForbiddenPayloadFields(payloadJson)
    }

    @Test
    fun focusSessionOutboxMappingExcludesBrowserContentAndInputCaptureFields() = runBlocking {
        val writer = FakeSyncOutboxWriter()
        val enqueuer = FocusSessionSyncOutboxEnqueuer(outbox = writer)

        enqueuer.enqueueFocusSessions(
            listOf(
                FocusSessionEntity(
                    clientSessionId = "android:com.android.chrome:1000:61000",
                    packageName = "com.android.chrome",
                    startedAtUtcMillis = 1_000L,
                    endedAtUtcMillis = 61_000L,
                    durationMs = 60_000L,
                    localDate = "1970-01-01",
                    timezoneId = "Asia/Seoul",
                    isIdle = false,
                    source = "android_usage_stats"
                )
            )
        )

        assertNoForbiddenPayloadFields(writer.items.single().payloadJson)
    }

    @Test
    fun locationContextUploadPayloadExcludesBrowserContentAndInputCaptureFields() {
        val payloadJson = moshi.adapter(SyncLocationContextUploadRequest::class.java)
            .toJson(
                SyncLocationContextUploadRequest(
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
            )

        assertNoForbiddenPayloadFields(payloadJson)
    }

    @Test
    fun locationContextPayloadFactoryExcludesBrowserContentAndInputCaptureFields() {
        val factory = LocationContextSyncPayloadFactory(
            syncSettings = FakeSyncSettings(isEnabled = true),
            locationSettings = FakeLocationSettings(isLocationEnabled = true),
            timezoneId = "Asia/Seoul"
        )

        val payload = factory.buildPayload(
            deviceId = "android-device-1",
            snapshots = listOf(
                LocationContextSnapshotEntity(
                    id = "location-1",
                    deviceId = "android-device-1",
                    capturedAtUtcMillis = 1_777_334_400_000,
                    latitude = 37.5665,
                    longitude = 126.9780,
                    accuracyMeters = 35.5f,
                    permissionState = LocationPermissionState.GrantedApproximate,
                    captureMode = LocationCaptureMode.AppUsageContext,
                    createdAtUtcMillis = 1_777_680_000_500
                )
            )
        )
        val payloadJson = moshi.adapter(SyncLocationContextUploadRequest::class.java).toJson(payload)

        assertNoForbiddenPayloadFields(payloadJson)
    }

    private fun assertNoForbiddenPayloadFields(payloadJson: String) {
        val payload = mapAdapter.fromJson(payloadJson) ?: error("Payload JSON could not be parsed")
        val keys = flattenKeys(payload)
        val forbiddenMatches = keys.filter { key ->
            forbiddenFieldNames.any { forbidden -> key.equals(forbidden, ignoreCase = true) } ||
                forbiddenFieldFragments.any { forbidden -> key.contains(forbidden, ignoreCase = true) }
        }

        assertTrue(
            "Android sync payload must not expose forbidden privacy fields. Found: $forbiddenMatches in $payloadJson",
            forbiddenMatches.isEmpty()
        )
    }

    private fun flattenKeys(value: Any?): List<String> {
        return when (value) {
            is Map<*, *> -> value.flatMap { (key, child) ->
                listOf(key.toString()) + flattenKeys(child)
            }
            is List<*> -> value.flatMap(::flattenKeys)
            else -> emptyList()
        }
    }

    private val mapAdapter = moshi.adapter<Map<String, Any?>>(
        Types.newParameterizedType(Map::class.java, String::class.java, Any::class.java)
    )

    private companion object {
        val forbiddenFieldNames = setOf(
            "url",
            "fullUrl",
            "browserUrl",
            "pageUrl",
            "path",
            "browserPath",
            "pagePath",
            "title",
            "pageTitle",
            "typedText",
            "textInput",
            "clipboard",
            "clipboardText",
            "screenshot",
            "screenshotPath",
            "screenCapture",
            "touchX",
            "touchY",
            "tapX",
            "tapY",
            "screenX",
            "screenY"
        )
        val forbiddenFieldFragments = setOf(
            "typed",
            "clipboard",
            "screenshot",
            "screenCapture",
            "touchCoordinate",
            "touchCoordinates",
            "globalTouch",
            "pageContent",
            "messageContent",
            "formInput",
            "password"
        )
    }

    private class FakeSyncOutboxWriter : SyncOutboxWriter {
        val items = mutableListOf<SyncOutboxEntity>()

        override fun insert(item: SyncOutboxEntity) {
            items += item
        }
    }

    private class FakeSyncSettings(
        private val isEnabled: Boolean
    ) : AndroidSyncSettings {
        override fun isSyncEnabled(): Boolean = isEnabled
    }

    private class FakeLocationSettings(
        private val isLocationEnabled: Boolean
    ) : AndroidLocationSettings {
        override fun isLocationCaptureEnabled(): Boolean = isLocationEnabled
        override fun isPreciseLatitudeLongitudeEnabled(): Boolean = true
        override fun isApproximateLocationPreferred(): Boolean = false
    }
}
