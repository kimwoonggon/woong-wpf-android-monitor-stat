package com.woong.monitorstack.settings

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class SharedPreferencesAndroidLocationSettingsTest {
    @Test
    fun locationContextDefaultsOffAndPreciseCoordinatesRequireSeparateOptIn() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        context.getSharedPreferences(
            SharedPreferencesAndroidLocationSettings.PreferenceName,
            Context.MODE_PRIVATE
        ).edit().clear().commit()

        val settings = SharedPreferencesAndroidLocationSettings(context)

        assertFalse(settings.isLocationCaptureEnabled())
        assertFalse(settings.isPreciseLatitudeLongitudeEnabled())
        assertTrue(settings.isApproximateLocationPreferred())
    }

    @Test
    fun preciseLatitudeLongitudeCanOnlyBeEnabledAfterLocationCaptureOptIn() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        context.getSharedPreferences(
            SharedPreferencesAndroidLocationSettings.PreferenceName,
            Context.MODE_PRIVATE
        ).edit().clear().commit()
        val settings = SharedPreferencesAndroidLocationSettings(context)

        settings.setPreciseLatitudeLongitudeEnabled(true)

        assertFalse(settings.isPreciseLatitudeLongitudeEnabled())

        settings.setLocationCaptureEnabled(true)
        settings.setPreciseLatitudeLongitudeEnabled(true)

        assertTrue(settings.isPreciseLatitudeLongitudeEnabled())
        assertFalse(settings.isApproximateLocationPreferred())

        settings.setLocationCaptureEnabled(false)

        assertFalse(settings.isLocationCaptureEnabled())
        assertFalse(settings.isPreciseLatitudeLongitudeEnabled())
        assertTrue(settings.isApproximateLocationPreferred())
    }
}
