package com.woong.monitorstack.usage

import android.content.Context
import androidx.test.core.app.ActivityScenario
import androidx.test.core.app.ApplicationProvider
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.uiautomator.UiDevice
import com.google.android.material.bottomnavigation.BottomNavigationView
import com.woong.monitorstack.MainActivity
import com.woong.monitorstack.R
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.sync.AndroidOutboxSyncProcessor
import java.io.File
import kotlinx.coroutines.runBlocking
import org.json.JSONArray
import org.json.JSONObject
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class AppSwitchQaEvidenceTest {
    @Test
    fun prepareCleanRoomForAppSwitchQa() {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val outputDir = outputDir(context)
        if (outputDir.exists()) {
            outputDir.deleteRecursively()
        }
        outputDir.mkdirs()

        MonitorDatabase.getInstance(context).clearAllTables()
        writeJson(
            File(outputDir, "room-assertions.json"),
            JSONObject()
                .put("status", "PREPARED")
                .put("privacy", PrivacyBoundary)
        )
    }

    @Test
    fun collectUsageStatsAfterChromeReturnPersistsFocusSessionAndOutbox() = runBlocking {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val arguments = InstrumentationRegistry.getArguments()
        val chromePackageName = arguments.getString("chromePackageName") ?: "com.android.chrome"
        val toUtcMillis = arguments.getString("toUtcMillis")?.toLongOrNull()
            ?: System.currentTimeMillis()
        val fromUtcMillis = arguments.getString("fromUtcMillis")?.toLongOrNull()
            ?: toUtcMillis - DefaultCollectionLookbackMs
        val database = MonitorDatabase.getInstance(context)

        AndroidUsageCollectionRunner.create(context).collect(fromUtcMillis, toUtcMillis)

        val chromeSessions = database.focusSessionDao().queryByPackage(chromePackageName, limit = 20)
        val chromeOutboxItems = database.syncOutboxDao()
            .queryPending(limit = 100)
            .filter { item ->
                item.aggregateType == AndroidOutboxSyncProcessor.FocusSessionAggregateType &&
                    item.payloadJson.contains("\"platformAppKey\":\"$chromePackageName\"")
            }
        val hasValidChromeSession = chromeSessions.any { session ->
            session.source == "android_usage_stats" &&
                session.durationMs > 0L &&
                session.startedAtUtcMillis < session.endedAtUtcMillis &&
                session.localDate.isNotBlank() &&
                session.timezoneId.isNotBlank()
        }

        val assertions = JSONObject()
            .put("status", if (hasValidChromeSession && chromeOutboxItems.isNotEmpty()) "PASS" else "FAIL")
            .put("privacy", PrivacyBoundary)
            .put("chromePackageName", chromePackageName)
            .put("fromUtcMillis", fromUtcMillis)
            .put("toUtcMillis", toUtcMillis)
            .put("focusSessionChromeRows", chromeSessions.size)
            .put("syncOutboxChromeRows", chromeOutboxItems.size)
            .put(
                "chromeFocusSessions",
                JSONArray(
                    chromeSessions.map { session ->
                        JSONObject()
                            .put("clientSessionId", session.clientSessionId)
                            .put("packageName", session.packageName)
                            .put("startedAtUtcMillis", session.startedAtUtcMillis)
                            .put("endedAtUtcMillis", session.endedAtUtcMillis)
                            .put("durationMs", session.durationMs)
                            .put("localDate", session.localDate)
                            .put("timezoneId", session.timezoneId)
                            .put("source", session.source)
                    }
                )
            )

        writeJson(File(outputDir(context), "room-assertions.json"), assertions)

        assertTrue(
            "Expected at least one valid Chrome UsageStats focus_session row.",
            hasValidChromeSession
        )
        assertTrue(
            "Expected at least one pending focus_session sync_outbox item for Chrome.",
            chromeOutboxItems.isNotEmpty()
        )
    }

    @Test
    fun captureWoongDashboardAndSessionsOnlyAfterReturn() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        val device = UiDevice.getInstance(instrumentation)
        val outputDir = outputDir(context)
        outputDir.mkdirs()

        withMainActivityTestGates {
            ActivityScenario.launch(MainActivity::class.java).use { scenario ->
                waitForScreen(device)
                captureScreen(
                    device = device,
                    outputDir = outputDir,
                    baseName = DashboardAfterAppSwitchBaseName
                )

                scenario.onActivity { activity ->
                    activity.findViewById<BottomNavigationView>(R.id.bottomNavigation)
                        .selectedItemId = R.id.navSessions
                    activity.supportFragmentManager.executePendingTransactions()
                }

                waitForScreen(device)
                captureScreen(
                    device = device,
                    outputDir = outputDir,
                    baseName = SessionsAfterAppSwitchBaseName
                )
            }
        }
    }

    private fun captureScreen(device: UiDevice, outputDir: File, baseName: String) {
        val screenshot = File(outputDir, "$baseName.png")
        val hierarchy = File(outputDir, "$baseName.xml")

        assertTrue("Expected screenshot capture to succeed for ${screenshot.name}", device.takeScreenshot(screenshot))
        device.dumpWindowHierarchy(hierarchy)
        assertTrue("Expected screenshot file to exist: $screenshot", screenshot.isFile)
        assertTrue("Expected UI hierarchy file to exist: $hierarchy", hierarchy.isFile)
    }

    private fun withMainActivityTestGates(block: () -> Unit) {
        val originalUsageAccessGateFactory = MainActivity.usageAccessGateFactory
        val originalUsageCollectionReconcilerFactory = MainActivity.usageCollectionReconcilerFactory
        val originalUsageImmediateCollectorFactory = MainActivity.usageImmediateCollectorFactory
        val originalSplashDelayMillis = MainActivity.splashDelayMillis

        MainActivity.usageAccessGateFactory = {
            object : MainActivity.UsageAccessGate {
                override fun hasUsageAccess(packageName: String): Boolean = true
            }
        }
        MainActivity.usageCollectionReconcilerFactory = {
            object : MainActivity.UsageCollectionReconciler {
                override fun reconcile(packageName: String): UsageCollectionScheduleResult =
                    UsageCollectionScheduleResult.Scheduled
            }
        }
        MainActivity.usageImmediateCollectorFactory = {
            object : AndroidRecentUsageCollector {
                override fun collectRecentUsage(): Int = 0
            }
        }
        MainActivity.splashDelayMillis = 0L

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

    private fun waitForScreen(device: UiDevice) {
        InstrumentationRegistry.getInstrumentation().waitForIdleSync()
        device.waitForIdle()
        Thread.sleep(500)
    }

    private fun outputDir(context: Context): File {
        return File(requireNotNull(context.getExternalFilesDir(null)), "app-switch-qa")
    }

    private fun writeJson(file: File, value: JSONObject) {
        file.parentFile?.mkdirs()
        file.writeText(value.toString(2))
    }

    companion object {
        private const val DefaultCollectionLookbackMs = 15 * 60 * 1_000L
        private const val DashboardAfterAppSwitchBaseName = "dashboard-after-app-switch"
        private const val SessionsAfterAppSwitchBaseName = "sessions-after-app-switch"
        private const val DashboardAfterAppSwitchScreenshot = "dashboard-after-app-switch.png"
        private const val SessionsAfterAppSwitchScreenshot = "sessions-after-app-switch.png"
        private const val PrivacyBoundary =
            "No Chrome screenshots, no Chrome UI hierarchy, no typed text, no form contents, no browser/page contents."
    }
}
