package com.woong.monitorstack.dashboard

enum class LocationMapMode {
    LocalPreview,
    GoogleMaps
}

class GoogleMapsAvailabilityPolicy(
    private val apiKey: String
) {
    fun mode(): LocationMapMode {
        return if (apiKey.isBlank()) {
            LocationMapMode.LocalPreview
        } else {
            LocationMapMode.GoogleMaps
        }
    }
}
