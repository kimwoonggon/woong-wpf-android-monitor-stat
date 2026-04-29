package com.woong.monitorstack.settings

import android.Manifest
import android.content.pm.PackageManager
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat

object LocationPermissionPolicy {
    fun shouldRequestForegroundLocationPermission(
        locationCaptureEnabled: Boolean,
        preciseLatitudeLongitudeEnabled: Boolean,
        coarseGranted: Boolean,
        fineGranted: Boolean
    ): Boolean {
        if (!locationCaptureEnabled) {
            return false
        }

        return if (preciseLatitudeLongitudeEnabled) {
            !fineGranted
        } else {
            !coarseGranted
        }
    }

    fun requiredForegroundPermissions(
        preciseLatitudeLongitudeEnabled: Boolean
    ): Array<String> {
        return if (preciseLatitudeLongitudeEnabled) {
            arrayOf(
                Manifest.permission.ACCESS_COARSE_LOCATION,
                Manifest.permission.ACCESS_FINE_LOCATION
            )
        } else {
            arrayOf(Manifest.permission.ACCESS_COARSE_LOCATION)
        }
    }
}

class LocationPermissionController(
    private val activity: AppCompatActivity
) {
    fun requestIfNeeded(settings: AndroidLocationSettings) {
        val coarseGranted = ContextCompat.checkSelfPermission(
            activity,
            Manifest.permission.ACCESS_COARSE_LOCATION
        ) == PackageManager.PERMISSION_GRANTED
        val fineGranted = ContextCompat.checkSelfPermission(
            activity,
            Manifest.permission.ACCESS_FINE_LOCATION
        ) == PackageManager.PERMISSION_GRANTED
        val shouldRequest = LocationPermissionPolicy.shouldRequestForegroundLocationPermission(
            locationCaptureEnabled = settings.isLocationCaptureEnabled(),
            preciseLatitudeLongitudeEnabled = settings.isPreciseLatitudeLongitudeEnabled(),
            coarseGranted = coarseGranted,
            fineGranted = fineGranted
        )

        if (shouldRequest) {
            ActivityCompat.requestPermissions(
                activity,
                LocationPermissionPolicy.requiredForegroundPermissions(
                    settings.isPreciseLatitudeLongitudeEnabled()
                ),
                RequestCode
            )
        }
    }

    companion object {
        const val RequestCode = 310
    }
}
