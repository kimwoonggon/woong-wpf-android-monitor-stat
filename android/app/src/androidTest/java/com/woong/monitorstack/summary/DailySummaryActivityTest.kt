package com.woong.monitorstack.summary

import android.content.Context
import android.content.Intent
import androidx.test.core.app.ActivityScenario
import androidx.test.core.app.ApplicationProvider
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withId
import androidx.test.espresso.matcher.ViewMatchers.withText
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.woong.monitorstack.R
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class DailySummaryActivityTest {
    @Test
    fun dailySummaryActivityDisplaysPreviousDaySummaryValues() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val intent = Intent(context, DailySummaryActivity::class.java)
            .putExtra(DailySummaryActivity.EXTRA_SUMMARY_DATE, "2026-04-27")
            .putExtra(DailySummaryActivity.EXTRA_ACTIVE_MS, 900_000L)
            .putExtra(DailySummaryActivity.EXTRA_IDLE_MS, 120_000L)
            .putExtra(DailySummaryActivity.EXTRA_WEB_MS, 240_000L)
            .putExtra(DailySummaryActivity.EXTRA_TOP_APP, "com.android.chrome")
            .putExtra(DailySummaryActivity.EXTRA_TOP_DOMAIN, "example.com")

        ActivityScenario.launch<DailySummaryActivity>(intent).use {
            onView(withId(R.id.dailySummaryRoot)).check(matches(isDisplayed()))
            onView(withId(R.id.dailySummaryTitle))
                .check(matches(withText(R.string.daily_summary_title)))
            onView(withId(R.id.dailySummaryDateText)).check(matches(withText("2026-04-27")))
            onView(withId(R.id.dailySummaryActiveText)).check(matches(withText("15m")))
            onView(withId(R.id.dailySummaryIdleText)).check(matches(withText("2m")))
            onView(withId(R.id.dailySummaryWebText)).check(matches(withText("4m")))
            onView(withId(R.id.dailySummaryTopAppText))
                .check(matches(withText("com.android.chrome")))
            onView(withId(R.id.dailySummaryTopDomainText))
                .check(matches(withText("example.com")))
        }
    }
}
