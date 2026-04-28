package com.woong.monitorstack.settings

import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withId
import androidx.test.espresso.matcher.ViewMatchers.withText
import androidx.test.ext.junit.rules.ActivityScenarioRule
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.woong.monitorstack.R
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class SettingsActivityTest {
    @get:Rule
    val activityRule = ActivityScenarioRule(SettingsActivity::class.java)

    @Test
    fun settingsActivityDisplaysPrivacyAndSyncDefaults() {
        onView(withId(R.id.settingsRoot)).check(matches(isDisplayed()))
        onView(withId(R.id.collectionVisibleText))
            .check(matches(withText(R.string.collection_visible_default)))
        onView(withId(R.id.syncOptInText))
            .check(matches(withText(R.string.sync_opt_in_default)))
    }
}
