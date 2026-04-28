package com.woong.monitorstack.usage

import com.squareup.moshi.Moshi
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.SyncOutboxEntity
import com.woong.monitorstack.data.local.SyncOutboxStatus
import com.woong.monitorstack.data.local.SyncOutboxWriter
import com.woong.monitorstack.sync.AndroidOutboxSyncProcessor
import com.woong.monitorstack.sync.SyncFocusSessionUploadItem
import java.time.Instant

class FocusSessionSyncOutboxEnqueuer(
    private val outbox: SyncOutboxWriter,
    private val clock: () -> Long = { System.currentTimeMillis() }
) : UsageSyncOutboxEnqueuer {
    private val payloadAdapter = Moshi.Builder()
        .add(KotlinJsonAdapterFactory())
        .build()
        .adapter(SyncFocusSessionUploadItem::class.java)

    override suspend fun enqueueFocusSessions(sessions: List<FocusSessionEntity>) {
        if (sessions.isEmpty()) {
            return
        }

        val now = clock()
        sessions.forEach { session ->
            outbox.insert(
                SyncOutboxEntity(
                    clientItemId = "${AndroidOutboxSyncProcessor.FocusSessionAggregateType}:${session.clientSessionId}",
                    aggregateType = AndroidOutboxSyncProcessor.FocusSessionAggregateType,
                    payloadJson = payloadAdapter.toJson(session.toUploadItem()),
                    status = SyncOutboxStatus.Pending,
                    retryCount = 0,
                    lastError = null,
                    createdAtUtcMillis = now,
                    updatedAtUtcMillis = now
                )
            )
        }
    }

    private fun FocusSessionEntity.toUploadItem(): SyncFocusSessionUploadItem {
        return SyncFocusSessionUploadItem(
            clientSessionId = clientSessionId,
            platformAppKey = packageName,
            startedAtUtc = Instant.ofEpochMilli(startedAtUtcMillis).toString(),
            endedAtUtc = Instant.ofEpochMilli(endedAtUtcMillis).toString(),
            durationMs = durationMs,
            localDate = localDate,
            timezoneId = timezoneId,
            isIdle = isIdle,
            source = source
        )
    }
}
