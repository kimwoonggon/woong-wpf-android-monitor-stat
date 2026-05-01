package com.woong.monitorstack.snapshots

import android.app.Activity
import android.content.Context
import android.content.Intent
import android.graphics.Rect
import android.view.View
import android.widget.EditText
import androidx.core.widget.NestedScrollView
import androidx.test.core.app.ActivityScenario
import androidx.test.core.app.ApplicationProvider
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.uiautomator.UiDevice
import com.woong.monitorstack.MainActivity
import com.woong.monitorstack.R
import com.woong.monitorstack.dashboard.DashboardActivity
import com.woong.monitorstack.sessions.AppDetailFragment
import com.woong.monitorstack.sessions.SessionsActivity
import com.woong.monitorstack.settings.SettingsActivity
import com.woong.monitorstack.summary.DailySummaryActivity
import com.woong.monitorstack.usage.AndroidRecentUsageCollector
import java.io.File
import java.time.LocalDate
import java.time.ZoneId
import com.woong.monitorstack.usage.UsageCollectionScheduleResult
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class SnapshotCaptureTest {
    @Test
    fun captureDashboardSettingsSessionsAndDailySummaryScreens() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        val device = UiDevice.getInstance(instrumentation)
        val outputDir = File(requireNotNull(context.getExternalFilesDir(null)), "ui-snapshots")
        if (outputDir.exists()) {
            outputDir.deleteRecursively()
        }
        outputDir.mkdirs()

        captureCanonicalFigmaScreens(
            device = device,
            outputDir = outputDir
        )
        captureActivity<DashboardActivity>(
            device = device,
            output = File(outputDir, "dashboard.png")
        )
        captureMainActivityWithUsageGate(
            device = device,
            output = File(outputDir, "09-main-shell.png"),
            hasUsageAccess = true,
            scheduleResult = UsageCollectionScheduleResult.Scheduled,
            splashDelayMillis = 0L
        )
        captureMainActivityWithUsageGate(
            device = device,
            output = File(outputDir, "13-permission-onboarding.png"),
            hasUsageAccess = false,
            scheduleResult = UsageCollectionScheduleResult.UsageAccessMissing,
            splashDelayMillis = 0L
        )
        captureMainShellSessions(
            device = device,
            output = File(outputDir, "10-main-shell-sessions.png")
        )
        captureMainShellDashboardOneHour(
            device = device,
            output = File(outputDir, "16-dashboard-1h-selected.png")
        )
        captureMainShellSessionsSixHour(
            device = device,
            output = File(outputDir, "17-sessions-6h-selected.png")
        )
        captureMainShellReport(
            device = device,
            output = File(outputDir, "12-main-shell-report.png")
        )
        captureMainShellReportCustomRange(
            device = device,
            output = File(outputDir, "15-report-custom-range.png")
        )
        captureMainShellSettings(
            device = device,
            output = File(outputDir, "11-main-shell-settings.png")
        )
        captureMainShellAppDetail(
            device = device,
            output = File(outputDir, "14-app-detail.png")
        )
        captureDashboardFeatureScreens(
            device = device,
            outputDir = outputDir
        )
        captureActivity<SettingsActivity>(
            device = device,
            output = File(outputDir, "settings.png")
        )
        captureSettingsFeatureScreens(
            device = device,
            outputDir = outputDir
        )
        captureActivity<SessionsActivity>(
            device = device,
            output = File(outputDir, "sessions.png")
        )
        captureActivity<SessionsActivity>(
            device = device,
            output = File(outputDir, "07-sessions-list.png")
        )

        val dailySummaryIntent = Intent(context, DailySummaryActivity::class.java)
            .putExtra(DailySummaryActivity.EXTRA_SUMMARY_DATE, "2026-04-27")
            .putExtra(DailySummaryActivity.EXTRA_ACTIVE_MS, 900_000L)
            .putExtra(DailySummaryActivity.EXTRA_IDLE_MS, 120_000L)
            .putExtra(DailySummaryActivity.EXTRA_WEB_MS, 240_000L)
            .putExtra(DailySummaryActivity.EXTRA_TOP_APP, "com.android.chrome")
            .putExtra(DailySummaryActivity.EXTRA_TOP_DOMAIN, "example.com")
        captureIntent(
            device = device,
            intent = dailySummaryIntent,
            output = File(outputDir, "daily-summary.png")
        )
        captureIntent(
            device = device,
            intent = dailySummaryIntent,
            output = File(outputDir, "08-daily-summary.png")
        )
    }

    private fun captureCanonicalFigmaScreens(
        device: UiDevice,
        outputDir: File
    ) {
        captureMainActivityWithUsageGate(
            device = device,
            output = File(outputDir, "figma-01-splash.png"),
            hasUsageAccess = true,
            scheduleResult = UsageCollectionScheduleResult.Scheduled,
            splashDelayMillis = 10_000L
        )
        captureMainActivityWithUsageGate(
            device = device,
            output = File(outputDir, "figma-02-permission.png"),
            hasUsageAccess = false,
            scheduleResult = UsageCollectionScheduleResult.UsageAccessMissing,
            splashDelayMillis = 0L
        )
        captureMainActivityWithUsageGate(
            device = device,
            output = File(outputDir, "figma-03-dashboard.png"),
            hasUsageAccess = true,
            scheduleResult = UsageCollectionScheduleResult.Scheduled,
            splashDelayMillis = 0L
        )
        captureMainShellTabWithUsageGate(
            device = device,
            output = File(outputDir, "figma-04-sessions.png"),
            selectedItemId = R.id.navSessions
        )
        captureMainShellAppDetail(
            device = device,
            output = File(outputDir, "figma-05-app-detail.png")
        )
        captureMainShellTabWithUsageGate(
            device = device,
            output = File(outputDir, "figma-06-report.png"),
            selectedItemId = R.id.navReport
        )
        captureMainShellTabWithUsageGate(
            device = device,
            output = File(outputDir, "figma-07-settings.png"),
            selectedItemId = R.id.navSettings
        )
    }

    private inline fun <reified T : Activity> captureActivity(
        device: UiDevice,
        output: File
    ) {
        ActivityScenario.launch(T::class.java).use {
            waitForScreen(device)
            assertTrue("Expected screenshot capture to succeed for ${output.name}", device.takeScreenshot(output))
            assertTrue("Expected screenshot file to exist: $output", output.isFile)
        }
    }

    private fun captureMainActivityWithUsageGate(
        device: UiDevice,
        output: File,
        hasUsageAccess: Boolean,
        scheduleResult: UsageCollectionScheduleResult,
        splashDelayMillis: Long
    ) {
        withMainActivityTestGates(
            hasUsageAccess = hasUsageAccess,
            scheduleResult = scheduleResult,
            splashDelayMillis = splashDelayMillis
        ) {
            ActivityScenario.launch(MainActivity::class.java).use {
                waitForScreen(device)
                captureScreen(device, output)
            }
        }
    }

    private fun captureMainShellTabWithUsageGate(
        device: UiDevice,
        output: File,
        selectedItemId: Int
    ) {
        withMainActivityTestGates(
            hasUsageAccess = true,
            scheduleResult = UsageCollectionScheduleResult.Scheduled,
            splashDelayMillis = 0L
        ) {
            ActivityScenario.launch(MainActivity::class.java).use { scenario ->
                waitForScreen(device)
                selectMainShellTab(scenario, selectedItemId)
                waitForScreen(device)
                captureScreen(device, output)
            }
        }
    }

    private fun selectMainShellTab(
        scenario: ActivityScenario<MainActivity>,
        selectedItemId: Int
    ) {
        scenario.onActivity { activity ->
            activity.findViewById<com.google.android.material.bottomnavigation.BottomNavigationView>(
                R.id.bottomNavigation
            ).selectedItemId = selectedItemId
            activity.supportFragmentManager.executePendingTransactions()
        }
    }

    private fun withMainActivityTestGates(
        hasUsageAccess: Boolean,
        scheduleResult: UsageCollectionScheduleResult,
        splashDelayMillis: Long,
        block: () -> Unit
    ) {
        val originalUsageAccessGateFactory = MainActivity.usageAccessGateFactory
        val originalUsageCollectionReconcilerFactory = MainActivity.usageCollectionReconcilerFactory
        val originalUsageImmediateCollectorFactory = MainActivity.usageImmediateCollectorFactory
        val originalSplashDelayMillis = MainActivity.splashDelayMillis

        MainActivity.usageAccessGateFactory = {
            object : MainActivity.UsageAccessGate {
                override fun hasUsageAccess(packageName: String): Boolean = hasUsageAccess
            }
        }
        MainActivity.usageCollectionReconcilerFactory = {
            object : MainActivity.UsageCollectionReconciler {
                override fun reconcile(packageName: String): UsageCollectionScheduleResult =
                    scheduleResult
            }
        }
        MainActivity.usageImmediateCollectorFactory = {
            object : AndroidRecentUsageCollector {
                override fun collectRecentUsage(): Int = 0
            }
        }
        MainActivity.splashDelayMillis = splashDelayMillis

        try {
            block()
        } finally {
            MainActivity.usageAccessGateFactory = originalUsageAccessGateFactory
            MainActivity.usageCollectionReconcilerFactory =
                originalUsageCollectionReconcilerFactory
            MainActivity.usageImmediateCollectorFactory =
                originalUsageImmediateCollectorFactory
            MainActivity.splashDelayMillis = originalSplashDelayMillis
        }
    }

    private fun captureDashboardFeatureScreens(
        device: UiDevice,
        outputDir: File
    ) {
        ActivityScenario.launch(DashboardActivity::class.java).use { scenario ->
            waitForScreen(device)
            captureScreen(device, File(outputDir, "01-dashboard-overview.png"))
            scrollDashboardTo(scenario, R.id.locationContextCard)
            waitForScreen(device)
            captureScreen(device, File(outputDir, "02-dashboard-summary-location.png"))
            scrollDashboardTo(scenario, R.id.chartsPanel)
            waitForScreen(device)
            captureScreen(device, File(outputDir, "03-dashboard-charts.png"))
            scrollDashboardTo(scenario, R.id.recentSessionsTitle)
            waitForScreen(device)
            captureScreen(device, File(outputDir, "04-dashboard-recent-sessions.png"))
        }
    }

    private fun captureSettingsFeatureScreens(
        device: UiDevice,
        outputDir: File
    ) {
        ActivityScenario.launch(SettingsActivity::class.java).use { scenario ->
            waitForScreen(device)
            captureScreen(device, File(outputDir, "05-settings-privacy-sync.png"))
            scrollSettingsTo(scenario, R.id.locationSettingsCard)
            waitForScreen(device)
            captureScreen(device, File(outputDir, "06-settings-location-permission.png"))
        }
    }

    private fun captureMainShellSessions(
        device: UiDevice,
        output: File
    ) {
        withMainActivityTestGates(
            hasUsageAccess = true,
            scheduleResult = UsageCollectionScheduleResult.Scheduled,
            splashDelayMillis = 0L
        ) {
            ActivityScenario.launch(MainActivity::class.java).use { scenario ->
                waitForScreen(device)
                selectMainShellTab(scenario, R.id.navSessions)
                waitForScreen(device)
                captureScreen(device, output)
            }
        }
    }

    private fun captureMainShellDashboardOneHour(
        device: UiDevice,
        output: File
    ) {
        withMainActivityTestGates(
            hasUsageAccess = true,
            scheduleResult = UsageCollectionScheduleResult.Scheduled,
            splashDelayMillis = 0L
        ) {
            ActivityScenario.launch(MainActivity::class.java).use { scenario ->
                waitForScreen(device)
                scenario.onActivity { activity ->
                    activity.findViewById<View>(R.id.oneHourFilterButton).performClick()
                }
                waitForScreen(device)
                captureScreen(device, output)
            }
        }
    }

    private fun captureMainShellSessionsSixHour(
        device: UiDevice,
        output: File
    ) {
        withMainActivityTestGates(
            hasUsageAccess = true,
            scheduleResult = UsageCollectionScheduleResult.Scheduled,
            splashDelayMillis = 0L
        ) {
            ActivityScenario.launch(MainActivity::class.java).use { scenario ->
                waitForScreen(device)
                selectMainShellTab(scenario, R.id.navSessions)
                waitForScreen(device)
                scenario.onActivity { activity ->
                    activity.supportFragmentManager.executePendingTransactions()
                    activity.findViewById<View>(R.id.sessionsSixHourButton).performClick()
                }
                waitForScreen(device)
                captureScreen(device, output)
            }
        }
    }

    private fun captureMainShellSettings(
        device: UiDevice,
        output: File
    ) {
        withMainActivityTestGates(
            hasUsageAccess = true,
            scheduleResult = UsageCollectionScheduleResult.Scheduled,
            splashDelayMillis = 0L
        ) {
            ActivityScenario.launch(MainActivity::class.java).use { scenario ->
                waitForScreen(device)
                selectMainShellTab(scenario, R.id.navSettings)
                waitForScreen(device)
                captureScreen(device, output)
            }
        }
    }

    private fun captureMainShellReport(
        device: UiDevice,
        output: File
    ) {
        withMainActivityTestGates(
            hasUsageAccess = true,
            scheduleResult = UsageCollectionScheduleResult.Scheduled,
            splashDelayMillis = 0L
        ) {
            ActivityScenario.launch(MainActivity::class.java).use { scenario ->
                waitForScreen(device)
                selectMainShellTab(scenario, R.id.navReport)
                waitForScreen(device)
                captureScreen(device, output)
            }
        }
    }

    private fun captureMainShellReportCustomRange(
        device: UiDevice,
        output: File
    ) {
        withMainActivityTestGates(
            hasUsageAccess = true,
            scheduleResult = UsageCollectionScheduleResult.Scheduled,
            splashDelayMillis = 0L
        ) {
            ActivityScenario.launch(MainActivity::class.java).use { scenario ->
                waitForScreen(device)
                selectMainShellTab(scenario, R.id.navReport)
                waitForScreen(device)
                scenario.onActivity { activity ->
                    val today = LocalDate.now(ZoneId.systemDefault()).toString()
                    activity.supportFragmentManager.executePendingTransactions()
                    activity.findViewById<View>(R.id.reportCustomButton).performClick()
                    activity.findViewById<EditText>(R.id.reportCustomStartDateEditText).setText(today)
                    activity.findViewById<EditText>(R.id.reportCustomEndDateEditText).setText(today)
                    activity.findViewById<View>(R.id.reportApplyCustomRangeButton).performClick()
                }
                waitForScreen(device)
                captureScreen(device, output)
            }
        }
    }

    private fun captureMainShellAppDetail(
        device: UiDevice,
        output: File
    ) {
        withMainActivityTestGates(
            hasUsageAccess = true,
            scheduleResult = UsageCollectionScheduleResult.Scheduled,
            splashDelayMillis = 0L
        ) {
            ActivityScenario.launch(MainActivity::class.java).use { scenario ->
                waitForScreen(device)
                scenario.onActivity { activity ->
                    activity.findViewById<View>(R.id.topAppBar).visibility = View.VISIBLE
                    activity.findViewById<View>(R.id.bottomNavigation).visibility = View.VISIBLE
                    activity.supportFragmentManager
                        .beginTransaction()
                        .replace(
                            R.id.mainFragmentContainer,
                            AppDetailFragment.newInstance("com.android.chrome")
                        )
                        .commitNow()
                }
                waitForScreen(device)
                captureScreen(device, output)
            }
        }
    }

    private fun scrollDashboardTo(
        scenario: ActivityScenario<DashboardActivity>,
        targetViewId: Int
    ) {
        scenario.onActivity { activity ->
            val scrollView = activity.findViewById<NestedScrollView>(R.id.dashboardScroll)
            val target = activity.findViewById<View>(targetViewId)
            val targetRect = Rect()
            target.getDrawingRect(targetRect)
            scrollView.offsetDescendantRectToMyCoords(target, targetRect)
            scrollView.scrollTo(0, targetRect.top.coerceAtLeast(0))
        }
    }

    private fun scrollSettingsTo(
        scenario: ActivityScenario<SettingsActivity>,
        targetViewId: Int
    ) {
        scenario.onActivity { activity ->
            val scrollView = activity.findViewById<NestedScrollView>(R.id.settingsRoot)
            val target = activity.findViewById<View>(targetViewId)
            val targetRect = Rect()
            target.getDrawingRect(targetRect)
            scrollView.offsetDescendantRectToMyCoords(target, targetRect)
            scrollView.scrollTo(0, targetRect.top.coerceAtLeast(0))
        }
    }

    private fun captureIntent(
        device: UiDevice,
        intent: Intent,
        output: File
    ) {
        ActivityScenario.launch<DailySummaryActivity>(intent).use {
            waitForScreen(device)
            assertTrue("Expected screenshot capture to succeed for ${output.name}", device.takeScreenshot(output))
            assertTrue("Expected screenshot file to exist: $output", output.isFile)
        }
    }

    private fun captureScreen(device: UiDevice, output: File) {
        assertTrue("Expected screenshot capture to succeed for ${output.name}", device.takeScreenshot(output))
        assertTrue("Expected screenshot file to exist: $output", output.isFile)
    }

    private fun waitForScreen(device: UiDevice) {
        InstrumentationRegistry.getInstrumentation().waitForIdleSync()
        device.waitForIdle()
        Thread.sleep(500)
    }
}
