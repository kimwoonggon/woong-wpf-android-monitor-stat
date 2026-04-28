package com.woong.monitorstack.dashboard

import androidx.test.ext.junit.rules.ActivityScenarioRule
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withId
import com.woong.monitorstack.R
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class DashboardActivityTest {
    @get:Rule
    val activityRule = ActivityScenarioRule(DashboardActivity::class.java)

    @Test
    fun dashboardDisplaysSummaryFiltersGuidanceAndRecentSessions() {
        onView(withId(R.id.dashboardRoot)).check(matches(isDisplayed()))
        onView(withId(R.id.todayFilterButton)).check(matches(isDisplayed()))
        onView(withId(R.id.totalActiveCard)).check(matches(isDisplayed()))
        onView(withId(R.id.topAppCard)).check(matches(isDisplayed()))
        onView(withId(R.id.idleCard)).check(matches(isDisplayed()))
        onView(withId(R.id.usageAccessSettingsButton)).check(matches(isDisplayed()))
        onView(withId(R.id.recentSessionsList)).check(matches(isDisplayed()))
    }
}
