package com.woong.monitorstack.sync

import com.woong.monitorstack.data.local.LocationContextSnapshotEntity
import com.woong.monitorstack.settings.AndroidLocationSettings
import com.woong.monitorstack.settings.AndroidSyncSettings
import java.time.Instant
import java.time.ZoneId
import java.util.TimeZone

class LocationContextSyncPayloadFactory(
    private val syncSettings: AndroidSyncSettings,
    private val locationSettings: AndroidLocationSettings,
    private val timezoneId: String = TimeZone.getDefault().id
) {
    fun buildPayload(
        deviceId: String,
        snapshots: List<LocationContextSnapshotEntity>
    ): SyncLocationContextUploadRequest {
        if (!syncSettings.isSyncEnabled() || !locationSettings.isLocationCaptureEnabled()) {
            return SyncLocationContextUploadRequest(deviceId = deviceId, contexts = emptyList())
        }

        val zoneId = ZoneId.of(timezoneId)
        return SyncLocationContextUploadRequest(
            deviceId = deviceId,
            contexts = snapshots.map { snapshot ->
                val capturedAtUtc = Instant.ofEpochMilli(snapshot.capturedAtUtcMillis)
                SyncLocationContextUploadItem(
                    clientContextId = snapshot.id,
                    capturedAtUtc = capturedAtUtc.toString(),
                    localDate = capturedAtUtc.atZone(zoneId).toLocalDate().toString(),
                    timezoneId = timezoneId,
                    latitude = snapshot.latitude,
                    longitude = snapshot.longitude,
                    accuracyMeters = snapshot.accuracyMeters,
                    captureMode = snapshot.captureMode.name,
                    permissionState = snapshot.permissionState.name,
                    source = LocationContextSource
                )
            }
        )
    }
}

data class SyncLocationContextUploadRequest(
    val deviceId: String,
    val contexts: List<SyncLocationContextUploadItem>
)

data class SyncLocationContextUploadItem(
    val clientContextId: String,
    val capturedAtUtc: String,
    val localDate: String,
    val timezoneId: String,
    val latitude: Double?,
    val longitude: Double?,
    val accuracyMeters: Float?,
    val captureMode: String,
    val permissionState: String,
    val source: String
)

private const val LocationContextSource = "android_location_context"
