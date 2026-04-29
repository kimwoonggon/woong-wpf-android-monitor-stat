package com.woong.monitorstack.settings

import android.content.Context

interface AndroidLocationSettings {
    fun isLocationCaptureEnabled(): Boolean
    fun isPreciseLatitudeLongitudeEnabled(): Boolean
    fun isApproximateLocationPreferred(): Boolean
}

class SharedPreferencesAndroidLocationSettings(
    context: Context
) : AndroidLocationSettings {
    private val preferences = context.applicationContext.getSharedPreferences(
        PreferenceName,
        Context.MODE_PRIVATE
    )

    override fun isLocationCaptureEnabled(): Boolean {
        return preferences.getBoolean(KeyLocationCaptureEnabled, false)
    }

    override fun isPreciseLatitudeLongitudeEnabled(): Boolean {
        return preferences.getBoolean(KeyPreciseLatitudeLongitudeEnabled, false)
    }

    override fun isApproximateLocationPreferred(): Boolean {
        return !isPreciseLatitudeLongitudeEnabled()
    }

    fun setLocationCaptureEnabled(enabled: Boolean) {
        preferences.edit()
            .putBoolean(KeyLocationCaptureEnabled, enabled)
            .apply {
                if (!enabled) {
                    putBoolean(KeyPreciseLatitudeLongitudeEnabled, false)
                }
            }
            .apply()
    }

    fun setPreciseLatitudeLongitudeEnabled(enabled: Boolean) {
        val canEnablePreciseCoordinates = enabled && isLocationCaptureEnabled()
        preferences.edit()
            .putBoolean(KeyPreciseLatitudeLongitudeEnabled, canEnablePreciseCoordinates)
            .apply()
    }

    companion object {
        const val PreferenceName = "woong_monitor_settings"
        private const val KeyLocationCaptureEnabled = "location_capture_enabled"
        private const val KeyPreciseLatitudeLongitudeEnabled = "precise_latitude_longitude_enabled"
    }
}
