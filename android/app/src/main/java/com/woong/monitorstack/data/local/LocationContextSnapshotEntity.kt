package com.woong.monitorstack.data.local

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "location_context_snapshots")
data class LocationContextSnapshotEntity(
    @PrimaryKey val id: String,
    val deviceId: String,
    val capturedAtUtcMillis: Long,
    val latitude: Double?,
    val longitude: Double?,
    val accuracyMeters: Float?,
    val permissionState: LocationPermissionState,
    val captureMode: LocationCaptureMode,
    val createdAtUtcMillis: Long
)

enum class LocationPermissionState {
    NotGranted,
    GrantedApproximate,
    GrantedPrecise
}

enum class LocationCaptureMode {
    DisabledOrUnavailable,
    AppUsageContext
}
