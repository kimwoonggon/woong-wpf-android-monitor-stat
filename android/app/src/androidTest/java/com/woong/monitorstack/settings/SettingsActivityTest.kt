package com.woong.monitorstack.settings

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isEnabled
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withId
import androidx.test.espresso.matcher.ViewMatchers.withText
import androidx.test.ext.junit.rules.ActivityScenarioRule
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.woong.monitorstack.R
import org.hamcrest.CoreMatchers.not
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class SettingsActivityTest {
    @get:Rule
    val activityRule = ActivityScenarioRule(SettingsActivity::class.java)

    @Test
    fun settingsActivityDisplaysPrivacyAndSyncDefaults() {
        onView(withId(R.id.settingsScrollRoot)).check(matches(isDisplayed()))
        onView(withId(R.id.collectionVisibleText))
            .check(matches(withText(R.string.collection_visible_default)))
        onView(withId(R.id.usageAccessGuidanceText))
            .check(matches(withText(R.string.usage_access_guidance)))
        onView(withId(R.id.sensitiveDataBoundaryText))
            .check(matches(withText(R.string.sensitive_data_boundary)))
        onView(withId(R.id.openUsageAccessSettingsButton))
            .check(matches(withText(R.string.usage_access_settings)))
        onView(withId(R.id.syncOptInText))
            .check(matches(withText(R.string.sync_opt_in_default)))
        onView(withId(R.id.syncStatusText))
            .check(matches(withText(R.string.sync_local_only_status)))
    }

    @Test
    fun settingsActivityDisplaysLocationSectionWithSafeDefaults() {
        onView(withId(R.id.locationContextDefaultText))
            .check(matches(withText(R.string.location_context_default)))
        onView(withId(R.id.locationCoordinateBoundaryText))
            .check(matches(withText(R.string.location_coordinate_boundary)))
        onView(withId(R.id.preciseLocationOptInText))
            .check(matches(withText(R.string.precise_location_opt_in)))
        onView(withId(R.id.locationContextCheckBox))
            .check(matches(withText(R.string.store_optional_location_context)))
        onView(withId(R.id.preciseLatitudeLongitudeCheckBox))
            .check(matches(withText(R.string.store_precise_latitude_longitude)))
        onView(withId(R.id.requestLocationPermissionButton))
            .check(matches(withText(R.string.request_location_permission)))
            .check(matches(not(isEnabled())))
    }
}
