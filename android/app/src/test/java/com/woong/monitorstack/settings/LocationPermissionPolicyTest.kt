package com.woong.monitorstack.settings

import android.Manifest
import org.junit.Assert.assertArrayEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class LocationPermissionPolicyTest {
    @Test
    fun shouldRequestForegroundLocationOnlyAfterLocationContextOptIn() {
        assertFalse(
            LocationPermissionPolicy.shouldRequestForegroundLocationPermission(
                locationCaptureEnabled = false,
                preciseLatitudeLongitudeEnabled = false,
                coarseGranted = false,
                fineGranted = false
            )
        )
        assertTrue(
            LocationPermissionPolicy.shouldRequestForegroundLocationPermission(
                locationCaptureEnabled = true,
                preciseLatitudeLongitudeEnabled = false,
                coarseGranted = false,
                fineGranted = false
            )
        )
        assertFalse(
            LocationPermissionPolicy.shouldRequestForegroundLocationPermission(
                locationCaptureEnabled = true,
                preciseLatitudeLongitudeEnabled = false,
                coarseGranted = true,
                fineGranted = false
            )
        )
        assertTrue(
            LocationPermissionPolicy.shouldRequestForegroundLocationPermission(
                locationCaptureEnabled = true,
                preciseLatitudeLongitudeEnabled = true,
                coarseGranted = true,
                fineGranted = false
            )
        )
    }

    @Test
    fun requiredPermissionsUseCoarseByDefaultAndFineOnlyAfterPreciseOptIn() {
        assertArrayEquals(
            arrayOf(Manifest.permission.ACCESS_COARSE_LOCATION),
            LocationPermissionPolicy.requiredForegroundPermissions(
                preciseLatitudeLongitudeEnabled = false
            )
        )
        assertArrayEquals(
            arrayOf(
                Manifest.permission.ACCESS_COARSE_LOCATION,
                Manifest.permission.ACCESS_FINE_LOCATION
            ),
            LocationPermissionPolicy.requiredForegroundPermissions(
                preciseLatitudeLongitudeEnabled = true
            )
        )
    }
}
