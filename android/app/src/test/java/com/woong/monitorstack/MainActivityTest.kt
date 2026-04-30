package com.woong.monitorstack

import android.content.Context
import android.widget.CheckBox
import android.widget.TextView
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.settings.SharedPreferencesAndroidLocationSettings
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.Robolectric
import org.robolectric.RobolectricTestRunner
import org.robolectric.Shadows.shadowOf
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class MainActivityTest {
    @Test
    fun launcherShowsMainShellWithoutRedirectingToAnotherActivity() {
        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()

        assertNull(shadowOf(activity).nextStartedActivity)
        assertNotNull(activity.findViewById(R.id.topAppBar))
        assertNotNull(activity.findViewById(R.id.mainFragmentContainer))
        assertNotNull(activity.findViewById(R.id.bottomNavigation))
        assertEquals(
            R.id.navDashboard,
            activity.findViewById<com.google.android.material.bottomnavigation.BottomNavigationView>(
                R.id.bottomNavigation
            ).selectedItemId
        )
    }

    @Test
    fun settingsTabShowsRuntimePrivacySyncAndLocationControls() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        context.getSharedPreferences(
            SharedPreferencesAndroidLocationSettings.PreferenceName,
            Context.MODE_PRIVATE
        ).edit().clear().commit()
        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()

        activity.findViewById<com.google.android.material.bottomnavigation.BottomNavigationView>(
            R.id.bottomNavigation
        ).selectedItemId = R.id.navSettings
        activity.supportFragmentManager.executePendingTransactions()

        assertNotNull(activity.findViewById(R.id.openUsageAccessSettingsButton))
        assertEquals(
            "This app does not collect messages, passwords, form input, or global touch coordinates.",
            activity.findViewById<TextView>(R.id.sensitiveDataBoundaryText).text.toString()
        )
        assertEquals(
            "Sync is off. Data stays on this Android device.",
            activity.findViewById<TextView>(R.id.syncStatusText).text.toString()
        )

        val locationContext = activity.findViewById<CheckBox>(R.id.locationContextCheckBox)
        val preciseLatitudeLongitude = activity.findViewById<CheckBox>(
            R.id.preciseLatitudeLongitudeCheckBox
        )

        assertEquals(false, locationContext.isChecked)
        assertEquals(false, preciseLatitudeLongitude.isChecked)
        assertEquals(false, preciseLatitudeLongitude.isEnabled)
    }
}
