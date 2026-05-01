package com.woong.monitorstack

import android.content.Context
import android.os.Bundle
import android.view.View
import android.view.ViewGroup
import androidx.appcompat.app.AppCompatActivity
import androidx.fragment.app.Fragment
import androidx.core.view.ViewCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.updatePadding
import com.woong.monitorstack.dashboard.DashboardFragment
import com.woong.monitorstack.databinding.ActivityMainBinding
import com.woong.monitorstack.layout.SystemInsetsLayoutCalculator
import com.woong.monitorstack.sessions.SessionsFragment
import com.woong.monitorstack.settings.SettingsFragment
import com.woong.monitorstack.summary.ReportFragment
import com.woong.monitorstack.usage.AndroidUsageAccessPermissionReader
import com.woong.monitorstack.usage.AndroidUsageCollectionScheduler
import com.woong.monitorstack.usage.AndroidRecentUsageCollector
import com.woong.monitorstack.usage.PermissionOnboardingFragment
import com.woong.monitorstack.usage.RunnerBackedAndroidRecentUsageCollector
import com.woong.monitorstack.usage.UsageAccessPermissionChecker
import com.woong.monitorstack.usage.UsageCollectionScheduleResult

class MainActivity : AppCompatActivity() {
    private lateinit var binding: ActivityMainBinding
    private lateinit var usageAccessGate: UsageAccessGate
    private lateinit var usageCollectionReconciler: UsageCollectionReconciler
    private lateinit var usageImmediateCollector: AndroidRecentUsageCollector
    private var completedInitialResume = false
    private var shellChromeVisible = true
    private var fragmentTopMarginWithChrome = 0
    private var fragmentBottomMarginWithChrome = 0

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)
        rememberFragmentContainerChromeMargins()
        setSupportActionBar(binding.topAppBar)
        applySystemBarInsets()
        usageAccessGate = usageAccessGateFactory(applicationContext)
        usageCollectionReconciler = usageCollectionReconcilerFactory(applicationContext)
        usageImmediateCollector = usageImmediateCollectorFactory(applicationContext)

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
            showSplashThenRoute()
        }
    }

    override fun onResume() {
        super.onResume()

        if (completedInitialResume && ::binding.isInitialized) {
            val scheduleResult = usageCollectionReconciler.reconcile(packageName)
            if (binding.bottomNavigation.selectedItemId == R.id.navDashboard) {
                showDashboardOrPermissionOnboarding(scheduleResult)
            }
        }
        completedInitialResume = true
    }

    private fun showDashboardOrPermissionOnboarding() {
        val scheduleResult = usageCollectionReconciler.reconcile(packageName)
        showDashboardOrPermissionOnboarding(scheduleResult)
    }

    private fun showDashboardOrPermissionOnboarding(scheduleResult: UsageCollectionScheduleResult) {
        if (usageAccessGate.hasUsageAccess(packageName)) {
            setShellChromeVisible(true)
            showScreen(DashboardFragment())
            collectRecentUsageThenRefreshDashboard(scheduleResult)
        } else {
            setShellChromeVisible(false)
            showScreen(
                PermissionOnboardingFragment.newInstance(
                    collectionStatusText(scheduleResult)
                )
            )
        }
    }

    private fun collectRecentUsageThenRefreshDashboard(scheduleResult: UsageCollectionScheduleResult) {
        if (scheduleResult != UsageCollectionScheduleResult.Scheduled) {
            return
        }

        Thread {
            try {
                usageImmediateCollector.collectRecentUsage()
            } catch (_: SecurityException) {
                return@Thread
            }

            runOnUiThread {
                (supportFragmentManager.findFragmentById(R.id.mainFragmentContainer) as? DashboardFragment)
                    ?.refreshFromDatabase()
            }
        }.start()
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

    private fun showSplashThenRoute() {
        setShellChromeVisible(false)
        showScreen(SplashFragment())
        binding.mainFragmentContainer.postDelayed(
            {
                if (!isFinishing && !isDestroyed && !supportFragmentManager.isDestroyed) {
                    showDashboardOrPermissionOnboarding()
                }
            },
            splashDelayMillis
        )
    }

    private fun setShellChromeVisible(visible: Boolean) {
        shellChromeVisible = visible
        val visibility = if (visible) View.VISIBLE else View.GONE
        binding.topAppBar.visibility = visibility
        binding.bottomNavigation.visibility = visibility
        applyFragmentContainerChromeMargins()
    }

    private fun rememberFragmentContainerChromeMargins() {
        val layoutParams = binding.mainFragmentContainer.layoutParams
        if (layoutParams is ViewGroup.MarginLayoutParams) {
            fragmentTopMarginWithChrome = layoutParams.topMargin
            fragmentBottomMarginWithChrome = layoutParams.bottomMargin
        }
    }

    private fun applyFragmentContainerChromeMargins() {
        binding.mainFragmentContainer.layoutParams =
            binding.mainFragmentContainer.layoutParams.apply {
                if (this is ViewGroup.MarginLayoutParams) {
                    topMargin = if (shellChromeVisible) fragmentTopMarginWithChrome else 0
                    bottomMargin = if (shellChromeVisible) fragmentBottomMarginWithChrome else 0
                }
            }
    }

    private fun applySystemBarInsets() {
        val baseBottomNavigationHeightPx = resources.getDimensionPixelSize(
            R.dimen.bottom_navigation_base_height
        )
        val layoutCalculator = SystemInsetsLayoutCalculator(baseBottomNavigationHeightPx)

        ViewCompat.setOnApplyWindowInsetsListener(binding.mainRoot) { _, insets ->
            val bottomInset = insets.getInsets(WindowInsetsCompat.Type.navigationBars()).bottom
            val layout = layoutCalculator.calculate(bottomInset)

            binding.bottomNavigation.layoutParams = binding.bottomNavigation.layoutParams.apply {
                height = layout.bottomNavigationHeightPx
            }
            binding.bottomNavigation.updatePadding(
                bottom = layout.bottomNavigationPaddingBottomPx
            )
            fragmentBottomMarginWithChrome = layout.fragmentBottomMarginPx
            applyFragmentContainerChromeMargins()

            insets
        }
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

        fun defaultUsageImmediateCollectorFactory(): (Context) -> AndroidRecentUsageCollector = {
            context ->
            RunnerBackedAndroidRecentUsageCollector.create(context.applicationContext)
        }

        var usageAccessGateFactory: (Context) -> UsageAccessGate =
            defaultUsageAccessGateFactory()

        var usageCollectionReconcilerFactory: (Context) -> UsageCollectionReconciler =
            defaultUsageCollectionReconcilerFactory()

        var usageImmediateCollectorFactory: (Context) -> AndroidRecentUsageCollector =
            defaultUsageImmediateCollectorFactory()

        const val DefaultSplashDelayMillis = 700L
        var splashDelayMillis: Long = DefaultSplashDelayMillis
    }
}
