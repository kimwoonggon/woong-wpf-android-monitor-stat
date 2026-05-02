package com.woong.monitorstack.data.local

import androidx.room.Entity
import androidx.room.Index
import androidx.room.PrimaryKey

@Entity(
    tableName = "location_visits",
    indices = [
        Index(
            value = ["deviceId", "locationKey", "lastCapturedAtUtcMillis"],
            name = "index_location_visits_device_key_last"
        ),
        Index(
            value = ["deviceId", "firstCapturedAtUtcMillis", "lastCapturedAtUtcMillis"],
            name = "index_location_visits_device_time"
        )
    ]
)
data class LocationVisitEntity(
    @PrimaryKey val id: String,
    val deviceId: String,
    val locationKey: String,
    val latitude: Double,
    val longitude: Double,
    val coordinatePrecisionDecimals: Int,
    val firstCapturedAtUtcMillis: Long,
    val lastCapturedAtUtcMillis: Long,
    val durationMs: Long,
    val sampleCount: Int,
    val accuracyMeters: Float?,
    val permissionState: LocationPermissionState,
    val captureMode: LocationCaptureMode,
    val createdAtUtcMillis: Long,
    val updatedAtUtcMillis: Long
)
