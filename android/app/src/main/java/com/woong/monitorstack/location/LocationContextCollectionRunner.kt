package com.woong.monitorstack.location

import com.squareup.moshi.Moshi
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import com.woong.monitorstack.data.local.LocationContextSnapshotDao
import com.woong.monitorstack.data.local.LocationContextSnapshotEntity
import com.woong.monitorstack.data.local.SyncOutboxEntity
import com.woong.monitorstack.data.local.SyncOutboxStatus
import com.woong.monitorstack.data.local.SyncOutboxWriter
import com.woong.monitorstack.sync.AndroidOutboxSyncProcessor
import com.woong.monitorstack.sync.SyncLocationContextUploadItem
import java.time.Instant
import java.time.ZoneId
import java.util.TimeZone

class LocationContextCollectionRunner(
    private val provider: RuntimeLocationSnapshotProvider,
    private val snapshotDao: LocationContextSnapshotDao,
    private val outbox: SyncOutboxWriter,
    private val timezoneId: String = TimeZone.getDefault().id,
    private val clock: () -> Long = System::currentTimeMillis
) {
    private val payloadAdapter = Moshi.Builder()
        .add(KotlinJsonAdapterFactory())
        .build()
        .adapter(SyncLocationContextUploadItem::class.java)

    fun collect(deviceId: String): LocationContextCollectionResult {
        val snapshot = provider.captureSnapshot(deviceId) ?: return LocationContextCollectionResult.Skipped

        snapshotDao.insert(snapshot)
        outbox.insert(snapshot.toOutboxItem())

        return LocationContextCollectionResult.Captured
    }

    private fun LocationContextSnapshotEntity.toOutboxItem(): SyncOutboxEntity {
        val now = clock()
        return SyncOutboxEntity(
            clientItemId = "${AndroidOutboxSyncProcessor.LocationContextAggregateType}:$id",
            aggregateType = AndroidOutboxSyncProcessor.LocationContextAggregateType,
            payloadJson = payloadAdapter.toJson(toUploadItem()),
            status = SyncOutboxStatus.Pending,
            retryCount = 0,
            lastError = null,
            createdAtUtcMillis = now,
            updatedAtUtcMillis = now
        )
    }

    private fun LocationContextSnapshotEntity.toUploadItem(): SyncLocationContextUploadItem {
        val capturedAtUtc = Instant.ofEpochMilli(capturedAtUtcMillis)
        return SyncLocationContextUploadItem(
            clientContextId = id,
            capturedAtUtc = capturedAtUtc.toString(),
            localDate = capturedAtUtc.atZone(ZoneId.of(timezoneId)).toLocalDate().toString(),
            timezoneId = timezoneId,
            latitude = latitude,
            longitude = longitude,
            accuracyMeters = accuracyMeters,
            captureMode = captureMode.name,
            permissionState = permissionState.name,
            source = LocationContextSource
        )
    }
}

enum class LocationContextCollectionResult {
    Captured,
    Skipped
}

private const val LocationContextSource = "android_location_context"
