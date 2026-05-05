package com.woong.monitorstack.dashboard

import androidx.test.ext.junit.rules.ActivityScenarioRule
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.scrollTo
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.isSelected
import androidx.test.espresso.matcher.ViewMatchers.withId
import androidx.test.espresso.matcher.ViewMatchers.withText
import androidx.test.platform.app.InstrumentationRegistry
import com.woong.monitorstack.R
import com.woong.monitorstack.data.local.MonitorDatabase
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class DashboardActivityTest {
    @Before
    fun clearDashboardDatabase() {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        MonitorDatabase.getInstance(context).clearAllTables()
    }

    @get:Rule
    val activityRule = ActivityScenarioRule(DashboardActivity::class.java)

    @Test
    fun dashboardDisplaysSummaryFiltersGuidanceAndRecentSessions() {
        onView(withId(R.id.dashboardScrollRoot)).check(matches(isDisplayed()))
        onView(withId(R.id.todayFilterButton)).check(matches(isDisplayed()))
        onView(withId(R.id.activeFocusCard)).perform(scrollTo()).check(matches(isDisplayed()))
        onView(withId(R.id.activeFocusValueText)).perform(scrollTo()).check(matches(isDisplayed()))
        onView(withId(R.id.topAppsCard)).perform(scrollTo()).check(matches(isDisplayed()))
        onView(withId(R.id.idleGapCard)).perform(scrollTo()).check(matches(isDisplayed()))
        onView(withId(R.id.idleValueText)).perform(scrollTo()).check(matches(isDisplayed()))
        onView(withId(R.id.hourlyFocusChart)).perform(scrollTo()).check(matches(isDisplayed()))
        onView(withId(R.id.recentSessionsCard)).perform(scrollTo()).check(matches(isDisplayed()))
    }

    @Test
    fun dashboardDisplaysLocationContextCardForScreenshotCoverage() {
        onView(withId(R.id.locationContextCard)).perform(scrollTo()).check(matches(isDisplayed()))
        onView(withId(R.id.locationContextLabel)).check(matches(withText(R.string.location_context_label)))
        onView(withId(R.id.locationStatusText)).check(matches(isDisplayed()))
        onView(withId(R.id.locationLatitudeText)).check(matches(isDisplayed()))
        onView(withId(R.id.locationLongitudeText)).check(matches(isDisplayed()))
        onView(withId(R.id.locationAccuracyText)).check(matches(isDisplayed()))
        onView(withId(R.id.locationCapturedAtText)).check(matches(isDisplayed()))
    }

    @Test
    fun periodFilterClicksUpdateSelectedPeriodLabel() {
        onView(withId(R.id.oneHourFilterButton)).perform(scrollTo(), click())
        onView(withId(R.id.oneHourFilterButton)).check(matches(isSelected()))

        onView(withId(R.id.todayFilterButton)).perform(scrollTo(), click())
        onView(withId(R.id.todayFilterButton)).check(matches(isSelected()))
    }
}
