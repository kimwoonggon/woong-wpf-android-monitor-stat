package com.woong.monitorstack

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
}
