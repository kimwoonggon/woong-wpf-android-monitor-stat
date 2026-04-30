package com.woong.monitorstack.snapshots

import android.app.Activity
import android.content.Context
import android.content.Intent
import android.view.View
import androidx.core.widget.NestedScrollView
import androidx.test.core.app.ActivityScenario
import androidx.test.core.app.ApplicationProvider
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.uiautomator.UiDevice
import com.woong.monitorstack.R
import com.woong.monitorstack.dashboard.DashboardActivity
import com.woong.monitorstack.sessions.SessionsActivity
import com.woong.monitorstack.settings.SettingsActivity
import com.woong.monitorstack.summary.DailySummaryActivity
import java.io.File
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

        captureActivity<DashboardActivity>(
            device = device,
            output = File(outputDir, "dashboard.png")
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
        ActivityScenario.launch(SettingsActivity::class.java).use {
            waitForScreen(device)
            captureScreen(device, File(outputDir, "05-settings-privacy-sync.png"))
            captureScreen(device, File(outputDir, "06-settings-location-permission.png"))
        }
    }

    private fun scrollDashboardTo(
        scenario: ActivityScenario<DashboardActivity>,
        targetViewId: Int
    ) {
        scenario.onActivity { activity ->
            val scrollView = activity.findViewById<NestedScrollView>(R.id.dashboardScroll)
            val target = activity.findViewById<View>(targetViewId)
            scrollView.scrollTo(0, target.top)
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
