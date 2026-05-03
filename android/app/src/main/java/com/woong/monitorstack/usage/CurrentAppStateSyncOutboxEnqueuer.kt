package com.woong.monitorstack.usage

import com.squareup.moshi.Moshi
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import com.woong.monitorstack.data.local.CurrentAppStateEntity
import com.woong.monitorstack.data.local.SyncOutboxEntity
import com.woong.monitorstack.data.local.SyncOutboxStatus
import com.woong.monitorstack.data.local.SyncOutboxWriter
import com.woong.monitorstack.sync.AndroidOutboxSyncProcessor
import com.woong.monitorstack.sync.SyncCurrentAppStateUploadItem
import java.time.Instant

interface CurrentAppStateOutboxEnqueuer {
    suspend fun enqueueCurrentAppState(state: CurrentAppStateEntity)
}

class CurrentAppStateSyncOutboxEnqueuer(
    private val outbox: SyncOutboxWriter,
    private val clock: () -> Long = { System.currentTimeMillis() }
) : CurrentAppStateOutboxEnqueuer {
    private val payloadAdapter = Moshi.Builder()
        .add(KotlinJsonAdapterFactory())
        .build()
        .adapter(SyncCurrentAppStateUploadItem::class.java)

    override suspend fun enqueueCurrentAppState(state: CurrentAppStateEntity) {
        val now = clock()
        outbox.insert(
            SyncOutboxEntity(
                clientItemId = "${AndroidOutboxSyncProcessor.CurrentAppStateAggregateType}:${state.clientStateId}",
                aggregateType = AndroidOutboxSyncProcessor.CurrentAppStateAggregateType,
                payloadJson = payloadAdapter.toJson(state.toUploadItem()),
                status = SyncOutboxStatus.Pending,
                retryCount = 0,
                lastError = null,
                createdAtUtcMillis = now,
                updatedAtUtcMillis = now
            )
        )
    }

    private fun CurrentAppStateEntity.toUploadItem(): SyncCurrentAppStateUploadItem {
        return SyncCurrentAppStateUploadItem(
            clientStateId = clientStateId,
            platform = AndroidPlatformCode,
            platformAppKey = packageName,
            observedAtUtc = Instant.ofEpochMilli(observedAtUtcMillis).toString(),
            localDate = localDate,
            timezoneId = timezoneId,
            status = status.name,
            source = source
        )
    }

    private companion object {
        const val AndroidPlatformCode = 2
    }
}

object NoopCurrentAppStateOutboxEnqueuer : CurrentAppStateOutboxEnqueuer {
    override suspend fun enqueueCurrentAppState(state: CurrentAppStateEntity) = Unit
}
