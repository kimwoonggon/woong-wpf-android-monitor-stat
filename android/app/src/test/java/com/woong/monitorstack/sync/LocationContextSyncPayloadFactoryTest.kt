package com.woong.monitorstack.sync

import com.woong.monitorstack.data.local.LocationCaptureMode
import com.woong.monitorstack.data.local.LocationContextSnapshotEntity
import com.woong.monitorstack.data.local.LocationPermissionState
import com.woong.monitorstack.settings.AndroidLocationSettings
import com.woong.monitorstack.settings.AndroidSyncSettings
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

class LocationContextSyncPayloadFactoryTest {
    @Test
    fun buildPayloadReturnsEmptyWhenSyncIsOffEvenIfLocationIsEnabled() {
        val factory = LocationContextSyncPayloadFactory(
            syncSettings = FakeSyncSettings(isEnabled = false),
            locationSettings = FakeLocationSettings(isLocationEnabled = true)
        )

        val payload = factory.buildPayload("android-device-1", listOf(locationSnapshot()))

        assertEquals("android-device-1", payload.deviceId)
        assertTrue(payload.contexts.isEmpty())
    }

    @Test
    fun buildPayloadReturnsEmptyWhenLocationOptInIsOffEvenIfSyncIsEnabled() {
        val factory = LocationContextSyncPayloadFactory(
            syncSettings = FakeSyncSettings(isEnabled = true),
            locationSettings = FakeLocationSettings(isLocationEnabled = false)
        )

        val payload = factory.buildPayload("android-device-1", listOf(locationSnapshot()))

        assertEquals("android-device-1", payload.deviceId)
        assertTrue(payload.contexts.isEmpty())
    }

    @Test
    fun buildPayloadIncludesNullableCoordinatesOnlyWhenSyncAndLocationAreEnabled() {
        val factory = LocationContextSyncPayloadFactory(
            syncSettings = FakeSyncSettings(isEnabled = true),
            locationSettings = FakeLocationSettings(isLocationEnabled = true),
            timezoneId = "Asia/Seoul"
        )

        val payload = factory.buildPayload(
            deviceId = "android-device-1",
            snapshots =
            listOf(
                locationSnapshot(
                    id = "location-with-coordinates",
                    latitude = 37.5665,
                    longitude = 126.9780,
                    accuracyMeters = 35.5f
                ),
                locationSnapshot(
                    id = "location-without-coordinates",
                    latitude = null,
                    longitude = null,
                    accuracyMeters = null
                )
            )
        )

        assertEquals("android-device-1", payload.deviceId)
        assertEquals("location-with-coordinates", payload.contexts[0].clientContextId)
        assertEquals("2026-04-28T00:00:00Z", payload.contexts[0].capturedAtUtc)
        assertEquals("2026-04-28", payload.contexts[0].localDate)
        assertEquals("Asia/Seoul", payload.contexts[0].timezoneId)
        assertEquals(37.5665, payload.contexts[0].latitude ?: 0.0, 0.0001)
        assertEquals(126.9780, payload.contexts[0].longitude ?: 0.0, 0.0001)
        assertEquals(35.5f, payload.contexts[0].accuracyMeters ?: 0f, 0.0001f)
        assertEquals("AppUsageContext", payload.contexts[0].captureMode)
        assertEquals("GrantedApproximate", payload.contexts[0].permissionState)
        assertEquals("android_location_context", payload.contexts[0].source)
        assertEquals(null, payload.contexts[1].latitude)
        assertEquals(null, payload.contexts[1].longitude)
        assertEquals(null, payload.contexts[1].accuracyMeters)
    }

    private fun locationSnapshot(
        id: String = "location-1",
        latitude: Double? = 37.5665,
        longitude: Double? = 126.9780,
        accuracyMeters: Float? = 35.5f
    ): LocationContextSnapshotEntity {
        return LocationContextSnapshotEntity(
            id = id,
            deviceId = "android-device-1",
            capturedAtUtcMillis = 1_777_334_400_000,
            latitude = latitude,
            longitude = longitude,
            accuracyMeters = accuracyMeters,
            permissionState = LocationPermissionState.GrantedApproximate,
            captureMode = LocationCaptureMode.AppUsageContext,
            createdAtUtcMillis = 1_777_680_000_500
        )
    }

    private class FakeSyncSettings(
        private val isEnabled: Boolean
    ) : AndroidSyncSettings {
        override fun isSyncEnabled(): Boolean = isEnabled
    }

    private class FakeLocationSettings(
        private val isLocationEnabled: Boolean
    ) : AndroidLocationSettings {
        override fun isLocationCaptureEnabled(): Boolean = isLocationEnabled
        override fun isPreciseLatitudeLongitudeEnabled(): Boolean = false
        override fun isApproximateLocationPreferred(): Boolean = true
    }
}
