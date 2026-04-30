package com.woong.monitorstack.dashboard

import androidx.test.ext.junit.rules.ActivityScenarioRule
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.scrollTo
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
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
        onView(withId(R.id.dashboardRoot)).check(matches(isDisplayed()))
        onView(withId(R.id.todayFilterButton)).check(matches(isDisplayed()))
        onView(withId(R.id.totalActiveCard)).perform(scrollTo()).check(matches(isDisplayed()))
        onView(withId(R.id.totalActiveText)).perform(scrollTo()).check(matches(isDisplayed()))
        onView(withId(R.id.topAppsCard)).perform(scrollTo()).check(matches(isDisplayed()))
        onView(withId(R.id.topAppText)).perform(scrollTo()).check(matches(isDisplayed()))
        onView(withId(R.id.idleCard)).perform(scrollTo()).check(matches(isDisplayed()))
        onView(withId(R.id.idleText)).perform(scrollTo()).check(matches(isDisplayed()))
        onView(withId(R.id.usageAccessGuidanceText))
            .perform(scrollTo())
            .check(matches(withText(R.string.usage_access_guidance)))
        onView(withId(R.id.usageAccessSettingsButton)).perform(scrollTo()).check(matches(isDisplayed()))
        onView(withId(R.id.activityLineChart)).perform(scrollTo()).check(matches(isDisplayed()))
        onView(withId(R.id.appUsageBarChart)).perform(scrollTo()).check(matches(isDisplayed()))
        onView(withId(R.id.emptySessionsText))
            .perform(scrollTo())
            .check(matches(withText(R.string.empty_sessions)))
        onView(withId(R.id.recentSessionsList)).perform(scrollTo()).check(matches(isDisplayed()))
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
        onView(withId(R.id.yesterdayFilterButton)).perform(scrollTo(), click())
        onView(withId(R.id.selectedPeriodText)).check(matches(withText(R.string.filter_yesterday)))

        onView(withId(R.id.recent7DaysFilterButton)).perform(scrollTo(), click())
        onView(withId(R.id.selectedPeriodText)).check(matches(withText(R.string.filter_recent_7_days)))

        onView(withId(R.id.todayFilterButton)).perform(scrollTo(), click())
        onView(withId(R.id.selectedPeriodText)).check(matches(withText(R.string.filter_today)))
    }
}
