package com.woong.monitorstack.location

import com.woong.monitorstack.data.local.LocationCaptureMode
import com.woong.monitorstack.data.local.LocationPermissionState
import com.woong.monitorstack.settings.AndroidLocationSettings
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class RuntimeLocationContextProviderTest {
    @Test
    fun captureReturnsNoSnapshotWhenLocationContextIsDisabled() {
        val provider = RuntimeLocationContextProvider(
            locationSettings = FakeLocationSettings(locationEnabled = false),
            permissionChecker = FakeForegroundLocationPermissionChecker(
                LocationPermissionState.GrantedPrecise
            ),
            locationReader = FakeRuntimeLocationReader(
                RuntimeLocationReading(
                    latitude = 37.5665,
                    longitude = 126.9780,
                    accuracyMeters = 8.5f
                )
            )
        )

        val snapshot = provider.captureSnapshot(deviceId = "android-device-1")

        assertNull(snapshot)
    }

    @Test
    fun captureReturnsNoSnapshotWhenForegroundLocationPermissionIsMissing() {
        val provider = RuntimeLocationContextProvider(
            locationSettings = FakeLocationSettings(locationEnabled = true),
            permissionChecker = FakeForegroundLocationPermissionChecker(
                LocationPermissionState.NotGranted
            ),
            locationReader = FakeRuntimeLocationReader(
                RuntimeLocationReading(
                    latitude = 37.5665,
                    longitude = 126.9780,
                    accuracyMeters = 8.5f
                )
            )
        )

        val snapshot = provider.captureSnapshot(deviceId = "android-device-1")

        assertNull(snapshot)
    }

    @Test
    fun captureInApproximateModeKeepsPreciseCoordinatesNull() {
        val provider = RuntimeLocationContextProvider(
            locationSettings = FakeLocationSettings(
                locationEnabled = true,
                preciseEnabled = false
            ),
            permissionChecker = FakeForegroundLocationPermissionChecker(
                LocationPermissionState.GrantedApproximate
            ),
            locationReader = FakeRuntimeLocationReader(
                RuntimeLocationReading(
                    latitude = 37.5665,
                    longitude = 126.9780,
                    accuracyMeters = 125.0f
                )
            ),
            clock = { 1_800_000L },
            idFactory = { "location-context-1" }
        )

        val snapshot = provider.captureSnapshot(deviceId = "android-device-1")

        requireNotNull(snapshot)
        assertEquals("location-context-1", snapshot.id)
        assertEquals("android-device-1", snapshot.deviceId)
        assertEquals(1_800_000L, snapshot.capturedAtUtcMillis)
        assertNull(snapshot.latitude)
        assertNull(snapshot.longitude)
        assertNull(snapshot.accuracyMeters)
        assertEquals(LocationPermissionState.GrantedApproximate, snapshot.permissionState)
        assertEquals(LocationCaptureMode.AppUsageContext, snapshot.captureMode)
    }

    @Test
    fun captureIncludesPreciseCoordinatesOnlyAfterSeparatePreciseOptIn() {
        val provider = RuntimeLocationContextProvider(
            locationSettings = FakeLocationSettings(
                locationEnabled = true,
                preciseEnabled = true
            ),
            permissionChecker = FakeForegroundLocationPermissionChecker(
                LocationPermissionState.GrantedPrecise
            ),
            locationReader = FakeRuntimeLocationReader(
                RuntimeLocationReading(
                    latitude = 37.5665,
                    longitude = 126.9780,
                    accuracyMeters = 8.5f
                )
            ),
            clock = { 1_900_000L },
            idFactory = { "location-context-2" }
        )

        val snapshot = provider.captureSnapshot(deviceId = "android-device-1")

        requireNotNull(snapshot)
        assertEquals(37.5665, snapshot.latitude ?: 0.0, 0.0001)
        assertEquals(126.9780, snapshot.longitude ?: 0.0, 0.0001)
        assertEquals(8.5f, snapshot.accuracyMeters ?: 0f, 0.0001f)
        assertEquals(LocationPermissionState.GrantedPrecise, snapshot.permissionState)
        assertEquals(1_900_000L, snapshot.createdAtUtcMillis)
    }

    @Test
    fun captureKeepsCoordinatesNullWhenPreciseOptInHasOnlyApproximatePermission() {
        val provider = RuntimeLocationContextProvider(
            locationSettings = FakeLocationSettings(
                locationEnabled = true,
                preciseEnabled = true
            ),
            permissionChecker = FakeForegroundLocationPermissionChecker(
                LocationPermissionState.GrantedApproximate
            ),
            locationReader = FakeRuntimeLocationReader(
                RuntimeLocationReading(
                    latitude = 37.5665,
                    longitude = 126.9780,
                    accuracyMeters = 125.0f
                )
            )
        )

        val snapshot = provider.captureSnapshot(deviceId = "android-device-1")

        requireNotNull(snapshot)
        assertNull(snapshot.latitude)
        assertNull(snapshot.longitude)
        assertNull(snapshot.accuracyMeters)
        assertEquals(LocationPermissionState.GrantedApproximate, snapshot.permissionState)
    }

    @Test
    fun captureStoresOnlyLocalSnapshotAndDoesNotRequireSyncOptIn() {
        val provider = RuntimeLocationContextProvider(
            locationSettings = FakeLocationSettings(locationEnabled = true),
            permissionChecker = FakeForegroundLocationPermissionChecker(
                LocationPermissionState.GrantedApproximate
            ),
            locationReader = FakeRuntimeLocationReader(
                RuntimeLocationReading(
                    latitude = 37.5665,
                    longitude = 126.9780,
                    accuracyMeters = 125.0f
                )
            ),
            clock = { 2_000_000L },
            idFactory = { "local-location-context" }
        )

        val snapshot = provider.captureSnapshot(deviceId = "android-device-1")

        requireNotNull(snapshot)
        assertEquals("local-location-context", snapshot.id)
        assertEquals(LocationCaptureMode.AppUsageContext, snapshot.captureMode)
    }

    private class FakeLocationSettings(
        private val locationEnabled: Boolean,
        private val preciseEnabled: Boolean = false
    ) : AndroidLocationSettings {
        override fun isLocationCaptureEnabled(): Boolean = locationEnabled
        override fun isPreciseLatitudeLongitudeEnabled(): Boolean = preciseEnabled
        override fun isApproximateLocationPreferred(): Boolean = !preciseEnabled
    }

    private class FakeForegroundLocationPermissionChecker(
        private val permissionState: LocationPermissionState
    ) : ForegroundLocationPermissionChecker {
        override fun foregroundLocationPermissionState(): LocationPermissionState = permissionState
    }

    private class FakeRuntimeLocationReader(
        private val reading: RuntimeLocationReading?
    ) : RuntimeLocationReader {
        override fun readCurrentLocation(): RuntimeLocationReading? = reading
    }
}
