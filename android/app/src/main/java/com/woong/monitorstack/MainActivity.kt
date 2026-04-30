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
import com.woong.monitorstack.usage.AndroidUsageCollectionScheduler
import com.woong.monitorstack.usage.PermissionOnboardingFragment
import com.woong.monitorstack.usage.UsageAccessPermissionChecker
import com.woong.monitorstack.usage.UsageCollectionScheduleResult

class MainActivity : AppCompatActivity() {
    private lateinit var binding: ActivityMainBinding
    private lateinit var usageAccessGate: UsageAccessGate
    private lateinit var usageCollectionReconciler: UsageCollectionReconciler
    private var completedInitialResume = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)
        setSupportActionBar(binding.topAppBar)
        usageAccessGate = usageAccessGateFactory(applicationContext)
        usageCollectionReconciler = usageCollectionReconcilerFactory(applicationContext)

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

    override fun onResume() {
        super.onResume()

        if (
            completedInitialResume &&
            ::binding.isInitialized &&
            binding.bottomNavigation.selectedItemId == R.id.navDashboard
        ) {
            showDashboardOrPermissionOnboarding()
        }
        completedInitialResume = true
    }

    private fun showDashboardOrPermissionOnboarding() {
        val scheduleResult = usageCollectionReconciler.reconcile(packageName)
        if (usageAccessGate.hasUsageAccess(packageName)) {
            showScreen(DashboardFragment())
        } else {
            showScreen(
                PermissionOnboardingFragment.newInstance(
                    collectionStatusText(scheduleResult)
                )
            )
        }
    }

    private fun collectionStatusText(result: UsageCollectionScheduleResult): String {
        return when (result) {
            UsageCollectionScheduleResult.CollectionDisabled ->
                getString(R.string.usage_collection_disabled)
            UsageCollectionScheduleResult.Scheduled ->
                getString(R.string.usage_collection_scheduled)
            UsageCollectionScheduleResult.UsageAccessMissing ->
                getString(R.string.usage_collection_paused_until_permission)
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

    interface UsageCollectionReconciler {
        fun reconcile(packageName: String): UsageCollectionScheduleResult
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

        fun defaultUsageCollectionReconcilerFactory(): (Context) -> UsageCollectionReconciler = {
            context ->
            val scheduler = AndroidUsageCollectionScheduler.create(context.applicationContext)

            object : UsageCollectionReconciler {
                override fun reconcile(packageName: String): UsageCollectionScheduleResult {
                    return scheduler.reconcile(packageName)
                }
            }
        }

        var usageAccessGateFactory: (Context) -> UsageAccessGate =
            defaultUsageAccessGateFactory()

        var usageCollectionReconcilerFactory: (Context) -> UsageCollectionReconciler =
            defaultUsageCollectionReconcilerFactory()
    }
}
