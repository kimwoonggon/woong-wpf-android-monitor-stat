package com.woong.monitorstack.sessions

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
class SessionsActivityTest {
    @get:Rule
    val activityRule = ActivityScenarioRule(SessionsActivity::class.java)

    @Test
    fun sessionsActivityDisplaysListSurfaceAndEmptyState() {
        onView(withId(R.id.sessionsRoot)).check(matches(isDisplayed()))
        onView(withId(R.id.sessionsList)).check(matches(isDisplayed()))
        onView(withId(R.id.emptySessionsText)).check(matches(withText(R.string.empty_sessions)))
    }
}
