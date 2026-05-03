package com.woong.monitorstack.sync

import com.woong.monitorstack.data.local.SyncOutboxEntity
import com.woong.monitorstack.data.local.SyncOutboxStatus
import com.woong.monitorstack.data.local.SyncOutboxStore
import com.woong.monitorstack.settings.AndroidLocationSettings
import org.junit.Assert.assertEquals
import org.junit.Test

class AndroidOutboxSyncProcessorTest {
    @Test
    fun processPendingTreatsAcceptedAndDuplicateAsSyncedAndErrorAsRetryableFailure() {
        val outbox = FakeSyncOutboxStore(
            listOf(
                focusOutboxItem("outbox-accepted", "session-accepted"),
                focusOutboxItem("outbox-duplicate", "session-duplicate"),
                focusOutboxItem("outbox-error", "session-error")
            )
        )
        val syncApi = FakeAndroidSyncApi(
            SyncUploadBatchResult(
                items = listOf(
                    SyncUploadItemResult(
                        clientId = "session-accepted",
                        status = SyncUploadItemStatus.Accepted,
                        errorMessage = null
                    ),
                    SyncUploadItemResult(
                        clientId = "session-duplicate",
                        status = SyncUploadItemStatus.Duplicate,
                        errorMessage = null
                    ),
                    SyncUploadItemResult(
                        clientId = "session-error",
                        status = SyncUploadItemStatus.Error,
                        errorMessage = "server rejected"
                    )
                )
            )
        )
        val processor = AndroidOutboxSyncProcessor(
            deviceId = "device-1",
            outbox = outbox,
            syncApi = syncApi,
            clock = { 9_000L }
        )

        val result = processor.processPending(limit = 10)

        assertEquals("device-1", syncApi.request.deviceId)
        assertEquals(
            listOf("session-accepted", "session-duplicate", "session-error"),
            syncApi.request.sessions.map { it.clientSessionId }
        )
        assertEquals(listOf("outbox-accepted", "outbox-duplicate"), outbox.syncedClientItemIds)
        assertEquals(
            listOf(FailedOutboxItem("outbox-error", "server rejected", 9_000L)),
            outbox.failedItems
        )
        assertEquals(
            AndroidOutboxSyncResult(syncedCount = 2, failedCount = 1),
            result
        )
    }

    @Test
    fun processPendingLeavesLocationContextPendingWhenLocationOptInIsOff() {
        val outbox = FakeSyncOutboxStore(
            listOf(locationOutboxItem("location-outbox-1", "location-context-1"))
        )
        val syncApi = FakeAndroidSyncApi(
            focusResult = SyncUploadBatchResult(items = emptyList()),
            locationResult = SyncUploadBatchResult(items = emptyList())
        )
        val processor = AndroidOutboxSyncProcessor(
            deviceId = "device-1",
            outbox = outbox,
            syncApi = syncApi,
            locationSettings = FakeLocationSettings(isLocationEnabled = false),
            clock = { 9_000L }
        )

        val result = processor.processPending(limit = 10)

        assertEquals(null, syncApi.locationRequest)
        assertEquals(emptyList<String>(), outbox.syncedClientItemIds)
        assertEquals(emptyList<FailedOutboxItem>(), outbox.failedItems)
        assertEquals(
            AndroidOutboxSyncResult(syncedCount = 0, failedCount = 0),
            result
        )
    }

    @Test
    fun processPendingUploadsLocationContextsAndMarksAcceptedRowsSyncedWhenLocationOptInIsOn() {
        val outbox = FakeSyncOutboxStore(
            listOf(
                locationOutboxItem("location-outbox-1", "location-context-1"),
                locationOutboxItem(
                    clientItemId = "location-outbox-2",
                    clientContextId = "location-context-2",
                    latitude = null,
                    longitude = null,
                    accuracyMeters = null
                )
            )
        )
        val syncApi = FakeAndroidSyncApi(
            focusResult = SyncUploadBatchResult(items = emptyList()),
            locationResult = SyncUploadBatchResult(
                items = listOf(
                    SyncUploadItemResult(
                        clientId = "location-context-1",
                        status = SyncUploadItemStatus.Accepted,
                        errorMessage = null
                    ),
                    SyncUploadItemResult(
                        clientId = "location-context-2",
                        status = SyncUploadItemStatus.Duplicate,
                        errorMessage = null
                    )
                )
            )
        )
        val processor = AndroidOutboxSyncProcessor(
            deviceId = "device-1",
            outbox = outbox,
            syncApi = syncApi,
            locationSettings = FakeLocationSettings(isLocationEnabled = true),
            clock = { 9_000L }
        )

        val result = processor.processPending(limit = 10)

        val request = requireNotNull(syncApi.locationRequest)
        assertEquals("device-1", request.deviceId)
        assertEquals(
            listOf("location-context-1", "location-context-2"),
            request.contexts.map { it.clientContextId }
        )
        assertEquals("2026-04-28T00:00:00Z", request.contexts[0].capturedAtUtc)
        assertEquals("2026-04-28", request.contexts[0].localDate)
        assertEquals("Asia/Seoul", request.contexts[0].timezoneId)
        assertEquals(37.5665, request.contexts[0].latitude ?: 0.0, 0.0001)
        assertEquals(126.9780, request.contexts[0].longitude ?: 0.0, 0.0001)
        assertEquals(35.5f, request.contexts[0].accuracyMeters ?: 0f, 0.0001f)
        assertEquals("AppUsageContext", request.contexts[0].captureMode)
        assertEquals("GrantedApproximate", request.contexts[0].permissionState)
        assertEquals("android_location_context", request.contexts[0].source)
        assertEquals(null, request.contexts[1].latitude)
        assertEquals(null, request.contexts[1].longitude)
        assertEquals(null, request.contexts[1].accuracyMeters)
        assertEquals(listOf("location-outbox-1", "location-outbox-2"), outbox.syncedClientItemIds)
        assertEquals(emptyList<FailedOutboxItem>(), outbox.failedItems)
        assertEquals(
            AndroidOutboxSyncResult(syncedCount = 2, failedCount = 0),
            result
        )
    }

    @Test
    fun processPendingUploadsCurrentAppStatesAndMarksAcceptedRowsSynced() {
        val outbox = FakeSyncOutboxStore(
            listOf(
                currentAppStateOutboxItem("current-outbox-1", "current-state-1"),
                currentAppStateOutboxItem("current-outbox-2", "current-state-2")
            )
        )
        val syncApi = FakeAndroidSyncApi(
            focusResult = SyncUploadBatchResult(items = emptyList()),
            currentAppStateResult = SyncUploadBatchResult(
                items = listOf(
                    SyncUploadItemResult(
                        clientId = "current-state-1",
                        status = SyncUploadItemStatus.Accepted,
                        errorMessage = null
                    ),
                    SyncUploadItemResult(
                        clientId = "current-state-2",
                        status = SyncUploadItemStatus.Duplicate,
                        errorMessage = null
                    )
                )
            )
        )
        val processor = AndroidOutboxSyncProcessor(
            deviceId = "device-1",
            outbox = outbox,
            syncApi = syncApi,
            clock = { 9_000L }
        )

        val result = processor.processPending(limit = 10)

        val request = requireNotNull(syncApi.currentAppStateRequest)
        assertEquals("device-1", request.deviceId)
        assertEquals(
            listOf("current-state-1", "current-state-2"),
            request.states.map { it.clientStateId }
        )
        assertEquals(2, request.states[0].platform)
        assertEquals("com.android.chrome", request.states[0].platformAppKey)
        assertEquals("2026-05-03T12:00:00Z", request.states[0].observedAtUtc)
        assertEquals("2026-05-03", request.states[0].localDate)
        assertEquals("Asia/Seoul", request.states[0].timezoneId)
        assertEquals("Active", request.states[0].status)
        assertEquals("android_usage_stats_current_app", request.states[0].source)
        assertEquals(listOf("current-outbox-1", "current-outbox-2"), outbox.syncedClientItemIds)
        assertEquals(emptyList<FailedOutboxItem>(), outbox.failedItems)
        assertEquals(
            AndroidOutboxSyncResult(syncedCount = 2, failedCount = 0),
            result
        )
    }

    @Test
    fun processPendingMarksRejectedLocationContextRowsAsRetryableFailure() {
        val outbox = FakeSyncOutboxStore(
            listOf(locationOutboxItem("location-outbox-error", "location-context-error"))
        )
        val syncApi = FakeAndroidSyncApi(
            focusResult = SyncUploadBatchResult(items = emptyList()),
            locationResult = SyncUploadBatchResult(
                items = listOf(
                    SyncUploadItemResult(
                        clientId = "location-context-error",
                        status = SyncUploadItemStatus.Error,
                        errorMessage = "location rejected"
                    )
                )
            )
        )
        val processor = AndroidOutboxSyncProcessor(
            deviceId = "device-1",
            outbox = outbox,
            syncApi = syncApi,
            locationSettings = FakeLocationSettings(isLocationEnabled = true),
            clock = { 9_000L }
        )

        val result = processor.processPending(limit = 10)

        assertEquals(emptyList<String>(), outbox.syncedClientItemIds)
        assertEquals(
            listOf(FailedOutboxItem("location-outbox-error", "location rejected", 9_000L)),
            outbox.failedItems
        )
        assertEquals(
            AndroidOutboxSyncResult(syncedCount = 0, failedCount = 1),
            result
        )
    }

    private fun focusOutboxItem(
        clientItemId: String,
        clientSessionId: String
    ): SyncOutboxEntity {
        return SyncOutboxEntity(
            clientItemId = clientItemId,
            aggregateType = "focus_session",
            payloadJson = """
                {
                  "clientSessionId": "$clientSessionId",
                  "platformAppKey": "com.android.chrome",
                  "startedAtUtc": "2026-04-28T00:00:00Z",
                  "endedAtUtc": "2026-04-28T00:15:00Z",
                  "durationMs": 900000,
                  "localDate": "2026-04-28",
                  "timezoneId": "Asia/Seoul",
                  "isIdle": false,
                  "source": "usage_stats"
                }
            """.trimIndent(),
            status = SyncOutboxStatus.Pending,
            retryCount = 0,
            lastError = null,
            createdAtUtcMillis = 1_000,
            updatedAtUtcMillis = 1_000
        )
    }

    private fun locationOutboxItem(
        clientItemId: String,
        clientContextId: String,
        latitude: Double? = 37.5665,
        longitude: Double? = 126.9780,
        accuracyMeters: Float? = 35.5f
    ): SyncOutboxEntity {
        val latitudeJson = latitude?.toString() ?: "null"
        val longitudeJson = longitude?.toString() ?: "null"
        val accuracyJson = accuracyMeters?.toString() ?: "null"
        return SyncOutboxEntity(
            clientItemId = clientItemId,
            aggregateType = "location_context",
            payloadJson = """
                {
                  "clientContextId": "$clientContextId",
                  "capturedAtUtc": "2026-04-28T00:00:00Z",
                  "localDate": "2026-04-28",
                  "timezoneId": "Asia/Seoul",
                  "latitude": $latitudeJson,
                  "longitude": $longitudeJson,
                  "accuracyMeters": $accuracyJson,
                  "captureMode": "AppUsageContext",
                  "permissionState": "GrantedApproximate",
                  "source": "android_location_context"
                }
            """.trimIndent(),
            status = SyncOutboxStatus.Pending,
            retryCount = 0,
            lastError = null,
            createdAtUtcMillis = 1_000,
            updatedAtUtcMillis = 1_000
        )
    }

    private fun currentAppStateOutboxItem(
        clientItemId: String,
        clientStateId: String
    ): SyncOutboxEntity {
        return SyncOutboxEntity(
            clientItemId = clientItemId,
            aggregateType = "current_app_state",
            payloadJson = """
                {
                  "clientStateId": "$clientStateId",
                  "platform": 2,
                  "platformAppKey": "com.android.chrome",
                  "observedAtUtc": "2026-05-03T12:00:00Z",
                  "localDate": "2026-05-03",
                  "timezoneId": "Asia/Seoul",
                  "status": "Active",
                  "source": "android_usage_stats_current_app"
                }
            """.trimIndent(),
            status = SyncOutboxStatus.Pending,
            retryCount = 0,
            lastError = null,
            createdAtUtcMillis = 1_000,
            updatedAtUtcMillis = 1_000
        )
    }

    private class FakeSyncOutboxStore(
        private val pendingItems: List<SyncOutboxEntity>
    ) : SyncOutboxStore {
        val syncedClientItemIds = mutableListOf<String>()
        val failedItems = mutableListOf<FailedOutboxItem>()

        override fun queryPending(limit: Int): List<SyncOutboxEntity> = pendingItems.take(limit)

        override fun markSynced(
            clientItemId: String,
            updatedAtUtcMillis: Long
        ) {
            syncedClientItemIds += clientItemId
        }

        override fun markFailed(
            clientItemId: String,
            lastError: String,
            updatedAtUtcMillis: Long
        ) {
            failedItems += FailedOutboxItem(clientItemId, lastError, updatedAtUtcMillis)
        }
    }

    private class FakeAndroidSyncApi(
        private val focusResult: SyncUploadBatchResult,
        private val locationResult: SyncUploadBatchResult = SyncUploadBatchResult(items = emptyList()),
        private val currentAppStateResult: SyncUploadBatchResult = SyncUploadBatchResult(items = emptyList())
    ) : AndroidSyncApi {
        lateinit var request: SyncFocusSessionUploadRequest
            private set

        var locationRequest: SyncLocationContextUploadRequest? = null
            private set

        var currentAppStateRequest: SyncCurrentAppStateUploadRequest? = null
            private set

        override fun uploadFocusSessions(
            request: SyncFocusSessionUploadRequest
        ): SyncUploadBatchResult {
            this.request = request
            return focusResult
        }

        override fun uploadLocationContexts(
            request: SyncLocationContextUploadRequest
        ): SyncUploadBatchResult {
            locationRequest = request
            return locationResult
        }

        override fun uploadCurrentAppStates(
            request: SyncCurrentAppStateUploadRequest
        ): SyncUploadBatchResult {
            currentAppStateRequest = request
            return currentAppStateResult
        }
    }

    private class FakeLocationSettings(
        private val isLocationEnabled: Boolean
    ) : AndroidLocationSettings {
        override fun isLocationCaptureEnabled(): Boolean = isLocationEnabled
        override fun isPreciseLatitudeLongitudeEnabled(): Boolean = false
        override fun isApproximateLocationPreferred(): Boolean = true
    }

    private data class FailedOutboxItem(
        val clientItemId: String,
        val lastError: String,
        val updatedAtUtcMillis: Long
    )
}
