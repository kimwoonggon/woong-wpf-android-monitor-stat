package com.woong.monitorstack.sync

import com.woong.monitorstack.data.local.SyncOutboxEntity
import com.woong.monitorstack.data.local.SyncOutboxStatus
import com.woong.monitorstack.data.local.SyncOutboxStore
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
        private val result: SyncUploadBatchResult
    ) : AndroidSyncApi {
        lateinit var request: SyncFocusSessionUploadRequest
            private set

        override fun uploadFocusSessions(
            request: SyncFocusSessionUploadRequest
        ): SyncUploadBatchResult {
            this.request = request
            return result
        }
    }

    private data class FailedOutboxItem(
        val clientItemId: String,
        val lastError: String,
        val updatedAtUtcMillis: Long
    )
}
