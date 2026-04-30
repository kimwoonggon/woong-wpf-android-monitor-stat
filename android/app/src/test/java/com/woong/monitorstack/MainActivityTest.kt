package com.woong.monitorstack

import com.woong.monitorstack.dashboard.DashboardActivity
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
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
    fun launcherImmediatelyOpensDashboard() {
        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()

        val startedIntent = shadowOf(activity).nextStartedActivity

        assertEquals(
            DashboardActivity::class.java.name,
            startedIntent.component?.className
        )
        assertTrue(activity.isFinishing)
    }
}
