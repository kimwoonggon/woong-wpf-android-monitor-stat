package com.woong.monitorstack.location

import android.content.Context
import android.location.Location
import android.location.LocationManager

fun interface LastKnownLocationSource {
    fun getLastKnownLocation(provider: String): Location?
}

class AndroidLastKnownLocationReader(
    private val locationSource: LastKnownLocationSource,
    private val providers: List<String> = DefaultProviders
) : RuntimeLocationReader {
    override fun readCurrentLocation(): RuntimeLocationReading? {
        val location = providers
            .mapNotNull { provider -> readProvider(provider) }
            .maxByOrNull { it.time }
            ?: return null

        return RuntimeLocationReading(
            latitude = location.latitude,
            longitude = location.longitude,
            accuracyMeters = if (location.hasAccuracy()) location.accuracy else null
        )
    }

    private fun readProvider(provider: String): Location? {
        return try {
            locationSource.getLastKnownLocation(provider)
        } catch (_: SecurityException) {
            null
        } catch (_: IllegalArgumentException) {
            null
        }
    }

    companion object {
        private val DefaultProviders = listOf(
            LocationManager.GPS_PROVIDER,
            LocationManager.NETWORK_PROVIDER,
            LocationManager.PASSIVE_PROVIDER
        )

        fun create(context: Context): AndroidLastKnownLocationReader {
            val locationManager = context.applicationContext
                .getSystemService(Context.LOCATION_SERVICE) as LocationManager

            return AndroidLastKnownLocationReader(
                locationSource = LastKnownLocationSource { provider ->
                    locationManager.getLastKnownLocation(provider)
                }
            )
        }
    }
}
