package com.woong.monitorstack.sync

import com.squareup.moshi.Moshi
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import com.woong.monitorstack.data.local.SyncOutboxEntity
import com.woong.monitorstack.data.local.SyncOutboxStore
import com.woong.monitorstack.settings.AndroidLocationSettings

class AndroidOutboxSyncProcessor(
    private val deviceId: String,
    private val outbox: SyncOutboxStore,
    private val syncApi: AndroidSyncApi,
    private val locationSettings: AndroidLocationSettings = DisabledLocationSettings,
    private val clock: () -> Long = { System.currentTimeMillis() }
) {
    private val moshi = Moshi.Builder()
        .add(SyncUploadItemStatusJsonAdapter())
        .add(KotlinJsonAdapterFactory())
        .build()
    private val focusSessionAdapter = moshi.adapter(SyncFocusSessionUploadItem::class.java)
    private val locationContextAdapter = moshi.adapter(SyncLocationContextUploadItem::class.java)
    private val currentAppStateAdapter = moshi.adapter(SyncCurrentAppStateUploadItem::class.java)

    fun processPending(limit: Int = DefaultLimit): AndroidOutboxSyncResult {
        val pendingItems = outbox.queryPending(limit)
        val pendingFocusSessions = pendingItems
            .filter { it.aggregateType == FocusSessionAggregateType }
            .map { PendingFocusSessionOutbox(it, parseFocusSession(it)) }
        val pendingLocationContexts = if (locationSettings.isLocationCaptureEnabled()) {
            pendingItems
                .filter { it.aggregateType == LocationContextAggregateType }
                .map { PendingLocationContextOutbox(it, parseLocationContext(it)) }
        } else {
            emptyList()
        }
        val pendingCurrentAppStates = pendingItems
            .filter { it.aggregateType == CurrentAppStateAggregateType }
            .map { PendingCurrentAppStateOutbox(it, parseCurrentAppState(it)) }

        if (
            pendingFocusSessions.isEmpty() &&
            pendingLocationContexts.isEmpty() &&
            pendingCurrentAppStates.isEmpty()
        ) {
            return AndroidOutboxSyncResult(syncedCount = 0, failedCount = 0)
        }

        val updatedAtUtcMillis = clock()
        var syncedCount = 0
        var failedCount = 0

        if (pendingFocusSessions.isNotEmpty()) {
            val uploadResult = syncApi.uploadFocusSessions(
                SyncFocusSessionUploadRequest(
                    deviceId = deviceId,
                    sessions = pendingFocusSessions.map { it.session }
                )
            )
            val resultsByClientId = uploadResult.items.associateBy { it.clientId }
            pendingFocusSessions.forEach { pending ->
                val result = applyUploadResult(
                    outboxItem = pending.outboxItem,
                    itemResult = resultsByClientId[pending.session.clientSessionId],
                    missingResultMessage = "Missing upload result for ${pending.session.clientSessionId}.",
                    updatedAtUtcMillis = updatedAtUtcMillis
                )
                syncedCount += result.syncedCount
                failedCount += result.failedCount
            }
        }

        if (pendingLocationContexts.isNotEmpty()) {
            val uploadResult = syncApi.uploadLocationContexts(
                SyncLocationContextUploadRequest(
                    deviceId = deviceId,
                    contexts = pendingLocationContexts.map { it.context }
                )
            )
            val resultsByClientId = uploadResult.items.associateBy { it.clientId }
            pendingLocationContexts.forEach { pending ->
                val result = applyUploadResult(
                    outboxItem = pending.outboxItem,
                    itemResult = resultsByClientId[pending.context.clientContextId],
                    missingResultMessage = "Missing upload result for ${pending.context.clientContextId}.",
                    updatedAtUtcMillis = updatedAtUtcMillis
                )
                syncedCount += result.syncedCount
                failedCount += result.failedCount
            }
        }

        if (pendingCurrentAppStates.isNotEmpty()) {
            val uploadResult = syncApi.uploadCurrentAppStates(
                SyncCurrentAppStateUploadRequest(
                    deviceId = deviceId,
                    states = pendingCurrentAppStates.map { it.state }
                )
            )
            val resultsByClientId = uploadResult.items.associateBy { it.clientId }
            pendingCurrentAppStates.forEach { pending ->
                val result = applyUploadResult(
                    outboxItem = pending.outboxItem,
                    itemResult = resultsByClientId[pending.state.clientStateId],
                    missingResultMessage = "Missing upload result for ${pending.state.clientStateId}.",
                    updatedAtUtcMillis = updatedAtUtcMillis
                )
                syncedCount += result.syncedCount
                failedCount += result.failedCount
            }
        }

        return AndroidOutboxSyncResult(
            syncedCount = syncedCount,
            failedCount = failedCount
        )
    }

    private fun applyUploadResult(
        outboxItem: SyncOutboxEntity,
        itemResult: SyncUploadItemResult?,
        missingResultMessage: String,
        updatedAtUtcMillis: Long
    ): AndroidOutboxSyncResult {
        return when (itemResult?.status) {
            SyncUploadItemStatus.Accepted,
            SyncUploadItemStatus.Duplicate -> {
                outbox.markSynced(outboxItem.clientItemId, updatedAtUtcMillis)
                AndroidOutboxSyncResult(syncedCount = 1, failedCount = 0)
            }
            SyncUploadItemStatus.Error -> {
                outbox.markFailed(
                    outboxItem.clientItemId,
                    itemResult.errorMessage ?: DefaultUploadError,
                    updatedAtUtcMillis
                )
                AndroidOutboxSyncResult(syncedCount = 0, failedCount = 1)
            }
            null -> {
                outbox.markFailed(
                    outboxItem.clientItemId,
                    missingResultMessage,
                    updatedAtUtcMillis
                )
                AndroidOutboxSyncResult(syncedCount = 0, failedCount = 1)
            }
        }
    }

    private fun parseFocusSession(outboxItem: SyncOutboxEntity): SyncFocusSessionUploadItem {
        return focusSessionAdapter.fromJson(outboxItem.payloadJson)
            ?: throw IllegalArgumentException(
                "Focus session outbox payload is empty: ${outboxItem.clientItemId}"
            )
    }

    private fun parseLocationContext(outboxItem: SyncOutboxEntity): SyncLocationContextUploadItem {
        return locationContextAdapter.fromJson(outboxItem.payloadJson)
            ?: throw IllegalArgumentException(
                "Location context outbox payload is empty: ${outboxItem.clientItemId}"
            )
    }

    private fun parseCurrentAppState(outboxItem: SyncOutboxEntity): SyncCurrentAppStateUploadItem {
        return currentAppStateAdapter.fromJson(outboxItem.payloadJson)
            ?: throw IllegalArgumentException(
                "Current app state outbox payload is empty: ${outboxItem.clientItemId}"
            )
    }

    private data class PendingFocusSessionOutbox(
        val outboxItem: SyncOutboxEntity,
        val session: SyncFocusSessionUploadItem
    )

    private data class PendingLocationContextOutbox(
        val outboxItem: SyncOutboxEntity,
        val context: SyncLocationContextUploadItem
    )

    private data class PendingCurrentAppStateOutbox(
        val outboxItem: SyncOutboxEntity,
        val state: SyncCurrentAppStateUploadItem
    )

    companion object {
        const val FocusSessionAggregateType = "focus_session"
        const val LocationContextAggregateType = "location_context"
        const val CurrentAppStateAggregateType = "current_app_state"
        private const val DefaultLimit = 50
        private const val DefaultUploadError = "Upload failed."
    }

    private object DisabledLocationSettings : AndroidLocationSettings {
        override fun isLocationCaptureEnabled(): Boolean = false
        override fun isPreciseLatitudeLongitudeEnabled(): Boolean = false
        override fun isApproximateLocationPreferred(): Boolean = true
    }
}

data class AndroidOutboxSyncResult(
    val syncedCount: Int,
    val failedCount: Int
)
