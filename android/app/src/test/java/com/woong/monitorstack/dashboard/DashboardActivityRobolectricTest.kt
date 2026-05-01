package com.woong.monitorstack.dashboard

import android.view.View
import android.widget.TextView
import androidx.test.core.app.ActivityScenario
import com.woong.monitorstack.R
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class DashboardActivityRobolectricTest {
    @Test
    fun dashboardActivityHostsCanonicalDashboardFragmentSurface() {
        ActivityScenario.launch(DashboardActivity::class.java).use { scenario ->
            scenario.onActivity { activity ->
                activity.supportFragmentManager.executePendingTransactions()

                val fragment = activity.supportFragmentManager.findFragmentById(
                    R.id.mainFragmentContainer
                )
                assertTrue(fragment is DashboardFragment)
                assertNotNull(activity.findViewById<View>(R.id.dashboardScrollRoot))
                assertEquals(
                    "Obsolete legacy Activity id must not remain in packaged resources: dashboardRoot",
                    0,
                    activity.resources.getIdentifier("dashboardRoot", "id", activity.packageName)
                )
                assertEquals(
                    "Current Focus",
                    activity.findViewById<TextView>(R.id.currentFocusTitle).text.toString()
                )
                assertNotNull(activity.findViewById<View>(R.id.hourlyFocusChartCard))
                assertNotNull(activity.findViewById<View>(R.id.topAppsCard))
            }
        }
    }

    @Test
    fun dashboardActivityCanonicalFragmentShowsLocationStatusCardWithSafeDefaults() {
        ActivityScenario.launch(DashboardActivity::class.java).use { scenario ->
            scenario.onActivity { activity ->
                activity.supportFragmentManager.executePendingTransactions()

                assertNotNull(activity.findViewById<View>(R.id.locationContextCard))
                assertEquals(
                    "Location context",
                    activity.findViewById<TextView>(R.id.locationContextLabel).text.toString()
                )
                assertEquals(
                    "Location capture off",
                    activity.findViewById<TextView>(R.id.locationStatusText).text.toString()
                )
                assertEquals(
                    activity.getString(R.string.location_latitude_value, "Latitude not stored"),
                    activity.findViewById<TextView>(R.id.locationLatitudeText).text.toString()
                )
                assertEquals(
                    activity.getString(R.string.location_longitude_value, "Longitude not stored"),
                    activity.findViewById<TextView>(R.id.locationLongitudeText).text.toString()
                )
                assertEquals(
                    activity.getString(R.string.location_accuracy_value, "Accuracy unavailable"),
                    activity.findViewById<TextView>(R.id.locationAccuracyText).text.toString()
                )
                assertEquals(
                    activity.getString(R.string.location_captured_at_value, "No location captured"),
                    activity.findViewById<TextView>(R.id.locationCapturedAtText).text.toString()
                )
            }
        }
    }
}
