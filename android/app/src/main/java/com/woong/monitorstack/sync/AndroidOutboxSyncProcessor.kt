package com.woong.monitorstack.sync

import com.squareup.moshi.Moshi
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import com.woong.monitorstack.data.local.SyncOutboxEntity
import com.woong.monitorstack.data.local.SyncOutboxStore

class AndroidOutboxSyncProcessor(
    private val deviceId: String,
    private val outbox: SyncOutboxStore,
    private val syncApi: AndroidSyncApi,
    private val clock: () -> Long = { System.currentTimeMillis() }
) {
    private val moshi = Moshi.Builder()
        .add(SyncUploadItemStatusJsonAdapter())
        .add(KotlinJsonAdapterFactory())
        .build()
    private val focusSessionAdapter = moshi.adapter(SyncFocusSessionUploadItem::class.java)

    fun processPending(limit: Int = DefaultLimit): AndroidOutboxSyncResult {
        val pendingFocusSessions = outbox.queryPending(limit)
            .filter { it.aggregateType == FocusSessionAggregateType }
            .map { PendingFocusSessionOutbox(it, parseFocusSession(it)) }

        if (pendingFocusSessions.isEmpty()) {
            return AndroidOutboxSyncResult(syncedCount = 0, failedCount = 0)
        }

        val uploadResult = syncApi.uploadFocusSessions(
            SyncFocusSessionUploadRequest(
                deviceId = deviceId,
                sessions = pendingFocusSessions.map { it.session }
            )
        )
        val resultsByClientId = uploadResult.items.associateBy { it.clientId }
        val updatedAtUtcMillis = clock()
        var syncedCount = 0
        var failedCount = 0

        pendingFocusSessions.forEach { pending ->
            val itemResult = resultsByClientId[pending.session.clientSessionId]
            when (itemResult?.status) {
                SyncUploadItemStatus.Accepted,
                SyncUploadItemStatus.Duplicate -> {
                    outbox.markSynced(pending.outboxItem.clientItemId, updatedAtUtcMillis)
                    syncedCount += 1
                }
                SyncUploadItemStatus.Error -> {
                    outbox.markFailed(
                        pending.outboxItem.clientItemId,
                        itemResult.errorMessage ?: DefaultUploadError,
                        updatedAtUtcMillis
                    )
                    failedCount += 1
                }
                null -> {
                    outbox.markFailed(
                        pending.outboxItem.clientItemId,
                        "Missing upload result for ${pending.session.clientSessionId}.",
                        updatedAtUtcMillis
                    )
                    failedCount += 1
                }
            }
        }

        return AndroidOutboxSyncResult(
            syncedCount = syncedCount,
            failedCount = failedCount
        )
    }

    private fun parseFocusSession(outboxItem: SyncOutboxEntity): SyncFocusSessionUploadItem {
        return focusSessionAdapter.fromJson(outboxItem.payloadJson)
            ?: throw IllegalArgumentException(
                "Focus session outbox payload is empty: ${outboxItem.clientItemId}"
            )
    }

    private data class PendingFocusSessionOutbox(
        val outboxItem: SyncOutboxEntity,
        val session: SyncFocusSessionUploadItem
    )

    companion object {
        const val FocusSessionAggregateType = "focus_session"
        private const val DefaultLimit = 50
        private const val DefaultUploadError = "Upload failed."
    }
}

data class AndroidOutboxSyncResult(
    val syncedCount: Int,
    val failedCount: Int
)
