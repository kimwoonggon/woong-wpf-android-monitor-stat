package com.woong.monitorstack.sync

import com.woong.monitorstack.data.local.LocationContextSnapshotEntity
import com.woong.monitorstack.settings.AndroidLocationSettings
import com.woong.monitorstack.settings.AndroidSyncSettings
import java.time.Instant

class LocationContextSyncPayloadFactory(
    private val syncSettings: AndroidSyncSettings,
    private val locationSettings: AndroidLocationSettings
) {
    fun buildPayload(snapshots: List<LocationContextSnapshotEntity>): SyncLocationContextUploadRequest {
        if (!syncSettings.isSyncEnabled() || !locationSettings.isLocationCaptureEnabled()) {
            return SyncLocationContextUploadRequest(items = emptyList())
        }

        return SyncLocationContextUploadRequest(
            items = snapshots.map { snapshot ->
                SyncLocationContextUploadItem(
                    clientSnapshotId = snapshot.id,
                    capturedAtUtc = Instant.ofEpochMilli(snapshot.capturedAtUtcMillis).toString(),
                    latitude = snapshot.latitude,
                    longitude = snapshot.longitude,
                    accuracyMeters = snapshot.accuracyMeters,
                    permissionState = snapshot.permissionState.name,
                    captureMode = snapshot.captureMode.name
                )
            }
        )
    }
}

data class SyncLocationContextUploadRequest(
    val items: List<SyncLocationContextUploadItem>
)

data class SyncLocationContextUploadItem(
    val clientSnapshotId: String,
    val capturedAtUtc: String,
    val latitude: Double?,
    val longitude: Double?,
    val accuracyMeters: Float?,
    val permissionState: String,
    val captureMode: String
)
