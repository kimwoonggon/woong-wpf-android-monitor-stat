package com.woong.monitorstack.location

import android.Manifest
import android.content.Context
import android.content.pm.PackageManager
import androidx.core.content.ContextCompat
import com.woong.monitorstack.data.local.LocationCaptureMode
import com.woong.monitorstack.data.local.LocationContextSnapshotEntity
import com.woong.monitorstack.data.local.LocationPermissionState
import com.woong.monitorstack.settings.AndroidLocationSettings
import java.util.UUID

interface RuntimeLocationSnapshotProvider {
    fun captureSnapshot(deviceId: String): LocationContextSnapshotEntity?
}

data class RuntimeLocationReading(
    val latitude: Double,
    val longitude: Double,
    val accuracyMeters: Float?
)

interface RuntimeLocationReader {
    fun readCurrentLocation(): RuntimeLocationReading?
}

interface ForegroundLocationPermissionChecker {
    fun foregroundLocationPermissionState(): LocationPermissionState
}

class AndroidForegroundLocationPermissionChecker(
    context: Context
) : ForegroundLocationPermissionChecker {
    private val appContext = context.applicationContext

    override fun foregroundLocationPermissionState(): LocationPermissionState {
        val fineGranted = hasPermission(Manifest.permission.ACCESS_FINE_LOCATION)
        if (fineGranted) {
            return LocationPermissionState.GrantedPrecise
        }

        val coarseGranted = hasPermission(Manifest.permission.ACCESS_COARSE_LOCATION)
        return if (coarseGranted) {
            LocationPermissionState.GrantedApproximate
        } else {
            LocationPermissionState.NotGranted
        }
    }

    private fun hasPermission(permission: String): Boolean {
        return ContextCompat.checkSelfPermission(
            appContext,
            permission
        ) == PackageManager.PERMISSION_GRANTED
    }
}

class RuntimeLocationContextProvider(
    private val locationSettings: AndroidLocationSettings,
    private val permissionChecker: ForegroundLocationPermissionChecker,
    private val locationReader: RuntimeLocationReader,
    private val clock: () -> Long = System::currentTimeMillis,
    private val idFactory: () -> String = { UUID.randomUUID().toString() }
) : RuntimeLocationSnapshotProvider {
    override fun captureSnapshot(deviceId: String): LocationContextSnapshotEntity? {
        if (!locationSettings.isLocationCaptureEnabled()) {
            return null
        }

        val permissionState = permissionChecker.foregroundLocationPermissionState()
        if (permissionState == LocationPermissionState.NotGranted) {
            return null
        }

        val reading = locationReader.readCurrentLocation() ?: return null
        val canStorePreciseCoordinates = locationSettings.isPreciseLatitudeLongitudeEnabled() &&
            permissionState == LocationPermissionState.GrantedPrecise
        val now = clock()

        return LocationContextSnapshotEntity(
            id = idFactory(),
            deviceId = deviceId,
            capturedAtUtcMillis = now,
            latitude = if (canStorePreciseCoordinates) reading.latitude else null,
            longitude = if (canStorePreciseCoordinates) reading.longitude else null,
            accuracyMeters = if (canStorePreciseCoordinates) reading.accuracyMeters else null,
            permissionState = permissionState,
            captureMode = LocationCaptureMode.AppUsageContext,
            createdAtUtcMillis = now
        )
    }
}
