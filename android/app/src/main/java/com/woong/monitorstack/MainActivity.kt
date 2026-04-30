package com.woong.monitorstack

import android.content.Context
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.fragment.app.Fragment
import com.woong.monitorstack.dashboard.DashboardFragment
import com.woong.monitorstack.databinding.ActivityMainBinding
import com.woong.monitorstack.sessions.SessionsFragment
import com.woong.monitorstack.settings.SettingsFragment
import com.woong.monitorstack.summary.ReportFragment
import com.woong.monitorstack.usage.AndroidUsageAccessPermissionReader
import com.woong.monitorstack.usage.PermissionOnboardingFragment
import com.woong.monitorstack.usage.UsageAccessPermissionChecker

class MainActivity : AppCompatActivity() {
    private lateinit var binding: ActivityMainBinding
    private lateinit var usageAccessGate: UsageAccessGate

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)
        setSupportActionBar(binding.topAppBar)
        usageAccessGate = usageAccessGateFactory(applicationContext)

        binding.bottomNavigation.setOnItemSelectedListener { item ->
            when (item.itemId) {
                R.id.navDashboard -> {
                    showDashboardOrPermissionOnboarding()
                    true
                }
                R.id.navSessions -> {
                    showScreen(SessionsFragment())
                    true
                }
                R.id.navReport -> {
                    showScreen(ReportFragment())
                    true
                }
                R.id.navSettings -> {
                    showScreen(SettingsFragment())
                    true
                }
                else -> false
            }
        }

        if (savedInstanceState == null) {
            binding.bottomNavigation.selectedItemId = R.id.navDashboard
        }
    }

    private fun showDashboardOrPermissionOnboarding() {
        if (usageAccessGate.hasUsageAccess(packageName)) {
            showScreen(DashboardFragment())
        } else {
            showScreen(PermissionOnboardingFragment())
        }
    }

    private fun showScreen(fragment: Fragment) {
        supportFragmentManager
            .beginTransaction()
            .replace(R.id.mainFragmentContainer, fragment)
            .commit()
    }

    interface UsageAccessGate {
        fun hasUsageAccess(packageName: String): Boolean
    }

    companion object {
        fun defaultUsageAccessGateFactory(): (Context) -> UsageAccessGate = { context ->
            val checker = UsageAccessPermissionChecker(
                AndroidUsageAccessPermissionReader(context.applicationContext)
            )

            object : UsageAccessGate {
                override fun hasUsageAccess(packageName: String): Boolean {
                    return checker.hasUsageAccess(packageName)
                }
            }
        }

        var usageAccessGateFactory: (Context) -> UsageAccessGate =
            defaultUsageAccessGateFactory()
    }
}
