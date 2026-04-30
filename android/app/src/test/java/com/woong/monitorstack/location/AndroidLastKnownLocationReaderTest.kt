package com.woong.monitorstack.location

import android.location.Location
import android.location.LocationManager
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class AndroidLastKnownLocationReaderTest {
    @Test
    fun readCurrentLocationReturnsFreshestLastKnownLocation() {
        val networkLocation = Location(LocationManager.NETWORK_PROVIDER).apply {
            latitude = 37.5000
            longitude = 126.9000
            accuracy = 90.0f
            time = 1_000L
        }
        val gpsLocation = Location(LocationManager.GPS_PROVIDER).apply {
            latitude = 37.5665
            longitude = 126.9780
            accuracy = 8.5f
            time = 2_000L
        }
        val reader = AndroidLastKnownLocationReader(
            locationSource = FakeLastKnownLocationSource(
                LocationManager.NETWORK_PROVIDER to networkLocation,
                LocationManager.GPS_PROVIDER to gpsLocation
            )
        )

        val reading = reader.readCurrentLocation()

        requireNotNull(reading)
        assertEquals(37.5665, reading.latitude, 0.0001)
        assertEquals(126.9780, reading.longitude, 0.0001)
        assertEquals(8.5f, reading.accuracyMeters ?: 0f, 0.0001f)
    }

    @Test
    fun readCurrentLocationReturnsNullWhenAllProvidersAreUnavailable() {
        val reader = AndroidLastKnownLocationReader(
            locationSource = FakeLastKnownLocationSource()
        )

        val reading = reader.readCurrentLocation()

        assertNull(reading)
    }

    @Test
    fun readCurrentLocationSkipsProviderFailuresWithoutCrashing() {
        val passiveLocation = Location(LocationManager.PASSIVE_PROVIDER).apply {
            latitude = 37.5550
            longitude = 127.0100
            time = 3_000L
        }
        val reader = AndroidLastKnownLocationReader(
            locationSource = ThrowingThenFakeLastKnownLocationSource(passiveLocation)
        )

        val reading = reader.readCurrentLocation()

        requireNotNull(reading)
        assertEquals(37.5550, reading.latitude, 0.0001)
        assertEquals(127.0100, reading.longitude, 0.0001)
        assertNull(reading.accuracyMeters)
    }

    private class FakeLastKnownLocationSource(
        vararg locations: Pair<String, Location>
    ) : LastKnownLocationSource {
        private val locationsByProvider = locations.toMap()

        override fun getLastKnownLocation(provider: String): Location? {
            return locationsByProvider[provider]
        }
    }

    private class ThrowingThenFakeLastKnownLocationSource(
        private val passiveLocation: Location
    ) : LastKnownLocationSource {
        override fun getLastKnownLocation(provider: String): Location? {
            if (provider != LocationManager.PASSIVE_PROVIDER) {
                throw SecurityException("provider unavailable")
            }

            return passiveLocation
        }
    }
}
