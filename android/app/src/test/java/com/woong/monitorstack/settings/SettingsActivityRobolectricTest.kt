package com.woong.monitorstack.settings

import android.content.Context
import android.content.Intent
import android.widget.Button
import android.widget.CheckBox
import android.widget.TextView
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.R
import org.junit.Assert.assertEquals
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.Robolectric
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class SettingsActivityRobolectricTest {
    @Test
    fun settingsActivityDisplaysClearUsageAccessGuidanceAndSyncOffStatus() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        clearSettingsPreferences(context)
        val activity = Robolectric.buildActivity(SettingsActivity::class.java)
            .setup()
            .get()

        assertEquals(
            activity.getString(R.string.settings_title),
            activity.findViewById<TextView>(R.id.settingsTitle).text.toString()
        )
        assertEquals(
            "Usage Access is needed to calculate app usage statistics.",
            activity.findViewById<TextView>(R.id.usageAccessGuidanceText).text.toString()
        )
        assertEquals(
            "This app does not collect messages, passwords, form input, or global touch coordinates.",
            activity.findViewById<TextView>(R.id.sensitiveDataBoundaryText).text.toString()
        )
        assertEquals(
            "Open Usage Access settings",
            activity.findViewById<Button>(R.id.openUsageAccessSettingsButton).text.toString()
        )
        assertEquals(
            "Sync is off. Data stays on this Android device.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )
        assertEquals(
            "Morning summary notifications require notification permission on Android 13+.",
            activity.findViewById<TextView>(
                R.id.notificationPermissionGuidanceText
            ).text.toString()
        )
        assertEquals(
            "Allow notifications",
            activity.findViewById<Button>(R.id.requestNotificationPermissionButton).text.toString()
        )
        assertEquals(
            "Location context is off by default.",
            activity.findViewById<TextView>(R.id.locationContextDefaultText).text.toString()
        )
        assertEquals(
            "Latitude/longitude are not stored unless location context is enabled.",
            activity.findViewById<TextView>(R.id.locationCoordinateBoundaryText).text.toString()
        )
        assertEquals(
            "Precise latitude/longitude requires a separate explicit opt-in.",
            activity.findViewById<TextView>(R.id.preciseLocationOptInText).text.toString()
        )
    }

    @Test
    fun settingsActivityLocationControlsDefaultToSafeOptInState() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        clearSettingsPreferences(context)

        val activity = Robolectric.buildActivity(SettingsActivity::class.java)
            .setup()
            .get()

        val locationContext = activity.findViewById<CheckBox>(R.id.locationContextCheckBox)
        val preciseLatitudeLongitude = activity.findViewById<CheckBox>(
            R.id.preciseLatitudeLongitudeCheckBox
        )

        assertEquals("Store optional location context", locationContext.text.toString())
        assertEquals("Store precise latitude/longitude", preciseLatitudeLongitude.text.toString())
        assertEquals(false, locationContext.isChecked)
        assertEquals(false, preciseLatitudeLongitude.isChecked)
        assertEquals(false, preciseLatitudeLongitude.isEnabled)
    }

    @Test
    fun locationPermissionRequestStaysDisabledUntilLocationContextOptIn() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        clearSettingsPreferences(context)
        val activity = Robolectric.buildActivity(SettingsActivity::class.java)
            .setup()
            .get()

        val locationContext = activity.findViewById<CheckBox>(R.id.locationContextCheckBox)
        val requestLocationPermission = activity.findViewById<Button>(
            R.id.requestLocationPermissionButton
        )

        assertEquals("Allow location permission", requestLocationPermission.text.toString())
        assertEquals(false, requestLocationPermission.isEnabled)

        locationContext.performClick()

        assertEquals(true, requestLocationPermission.isEnabled)
    }

    @Test
    fun settingsActivityShowsCanonicalSyncStatusEvenWhenLegacyFailureExtrasArePresent() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        clearSettingsPreferences(context)
        val intent = Intent(context, SettingsActivity::class.java)
            .putExtra(SettingsActivity.EXTRA_SYNC_FAILED_COUNT, 2)
            .putExtra(SettingsActivity.EXTRA_SYNC_FAILURE_MESSAGE, "server unavailable")

        val activity = Robolectric.buildActivity(SettingsActivity::class.java, intent)
            .setup()
            .get()

        val syncStatus = activity.findViewById<TextView>(R.id.syncStatusText)

        assertEquals(
            "Sync is off. Data stays on this Android device.",
            syncStatus.text.toString()
        )
    }

    private fun clearSettingsPreferences(context: Context) {
        context.getSharedPreferences(
            SharedPreferencesAndroidLocationSettings.PreferenceName,
            Context.MODE_PRIVATE
        ).edit().clear().commit()
        context.getSharedPreferences(
            SharedPreferencesAndroidSyncSettings.PreferenceName,
            Context.MODE_PRIVATE
        ).edit().clear().commit()
    }
}
