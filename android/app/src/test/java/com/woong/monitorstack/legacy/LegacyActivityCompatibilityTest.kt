package com.woong.monitorstack.legacy

import android.view.View
import androidx.appcompat.app.AppCompatActivity
import androidx.fragment.app.Fragment
import androidx.test.core.app.ActivityScenario
import com.woong.monitorstack.R
import com.woong.monitorstack.dashboard.DashboardActivity
import com.woong.monitorstack.dashboard.DashboardFragment
import com.woong.monitorstack.sessions.SessionsActivity
import com.woong.monitorstack.sessions.SessionsFragment
import com.woong.monitorstack.settings.SettingsActivity
import com.woong.monitorstack.settings.SettingsFragment
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class LegacyActivityCompatibilityTest {
    @Test
    fun dashboardActivityHostsCanonicalDashboardFragmentInsteadOfLegacyLayout() {
        ActivityScenario.launch(DashboardActivity::class.java).use { scenario ->
            scenario.onActivity { activity ->
                assertHostedFragment<DashboardFragment>(activity)
                assertNotNull(activity.findViewById<View>(R.id.dashboardScrollRoot))
                assertObsoleteIdRemoved(activity, "dashboardRoot")
            }
        }
    }

    @Test
    fun sessionsActivityHostsCanonicalSessionsFragmentInsteadOfLegacyLayout() {
        ActivityScenario.launch(SessionsActivity::class.java).use { scenario ->
            scenario.onActivity { activity ->
                assertHostedFragment<SessionsFragment>(activity)
                assertNotNull(activity.findViewById<View>(R.id.sessionsRecyclerView))
                assertObsoleteIdRemoved(activity, "sessionsList")
            }
        }
    }

    @Test
    fun settingsActivityHostsCanonicalSettingsFragmentInsteadOfLegacyLayout() {
        ActivityScenario.launch(SettingsActivity::class.java).use { scenario ->
            scenario.onActivity { activity ->
                assertHostedFragment<SettingsFragment>(activity)
                assertNotNull(activity.findViewById<View>(R.id.settingsScrollRoot))
                assertObsoleteIdRemoved(activity, "settingsRoot")
            }
        }
    }

    private inline fun <reified T : Fragment> assertHostedFragment(activity: AppCompatActivity) {
        activity.supportFragmentManager.executePendingTransactions()

        val fragment = activity.supportFragmentManager.findFragmentById(R.id.mainFragmentContainer)
        assertTrue("Expected ${T::class.java.simpleName}, got $fragment", fragment is T)
    }

    private fun assertObsoleteIdRemoved(activity: AppCompatActivity, idName: String) {
        assertEquals(
            "Obsolete legacy Activity id must not remain in packaged resources: $idName",
            0,
            activity.resources.getIdentifier(idName, "id", activity.packageName)
        )
    }
}
