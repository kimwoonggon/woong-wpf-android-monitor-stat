package com.woong.monitorstack.usage

import android.content.Context
import android.widget.TextView
import androidx.test.core.app.ActivityScenario
import androidx.test.core.app.ApplicationProvider
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.uiautomator.UiDevice
import com.google.android.material.bottomnavigation.BottomNavigationView
import com.woong.monitorstack.MainActivity
import com.woong.monitorstack.R
import com.woong.monitorstack.data.local.CurrentAppStateEntity
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.data.local.SyncOutboxEntity
import com.woong.monitorstack.display.AppDisplayNameFormatter
import com.woong.monitorstack.sync.AndroidOutboxSyncProcessor
import java.io.File
import kotlinx.coroutines.runBlocking
import org.json.JSONArray
import org.json.JSONObject
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Assume.assumeTrue
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
        assumeTrue(
            "App-switch QA requires scripts/run-android-app-switch-qa.ps1 to pass fromUtcMillis/toUtcMillis after a real Chrome foreground interval.",
            arguments.containsKey("fromUtcMillis") && arguments.containsKey("toUtcMillis")
        )
        val chromePackageName = arguments.getString("chromePackageName") ?: "com.android.chrome"
        val monitorPackageName = context.packageName
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
        val currentAppStates = database.currentAppStateDao()
            .queryAfterCheckpoint(observedAtUtcMillis = 0L, clientStateId = "", limit = 100)
        val latestCurrentAppState = currentAppStates.latestCurrentAppState()
        val pendingCurrentAppStateOutboxItems =
            database.pendingCurrentAppStateOutboxItems(monitorPackageName)
        val hasWoongCurrentAppState =
            currentAppStates.any { it.isValidCurrentAppStateFor(monitorPackageName) }
        val hasPendingWoongCurrentAppStateOutbox =
            pendingCurrentAppStateOutboxItems.isNotEmpty()
        val priorExternalChromeMetadataOnly = hasValidChromeSession &&
            chromeSessions.all { it.toMetadataJson().toString().containsOnlyMetadataFields() } &&
            chromeOutboxItems.all { it.payloadJson.containsOnlyMetadataFields() }
        val currentAppStateOutboxMetadataOnly =
            pendingCurrentAppStateOutboxItems.isNotEmpty() &&
                pendingCurrentAppStateOutboxItems.all {
                    it.payloadJson.containsOnlyMetadataFields()
                }
        val passed =
            hasValidChromeSession &&
                chromeOutboxItems.isNotEmpty() &&
                hasWoongCurrentAppState &&
                hasPendingWoongCurrentAppStateOutbox &&
                priorExternalChromeMetadataOnly &&
                currentAppStateOutboxMetadataOnly

        val assertions = JSONObject()
            .put("status", if (passed) "PASS" else "FAIL")
            .put("privacy", PrivacyBoundary)
            .put("chromePackageName", chromePackageName)
            .put("fromUtcMillis", fromUtcMillis)
            .put("toUtcMillis", toUtcMillis)
            .put("focusSessionChromeRows", chromeSessions.size)
            .put("syncOutboxChromeRows", chromeOutboxItems.size)
            .put("currentAppStateRows", currentAppStates.size)
            .put(
                "latestCurrentAppState",
                latestCurrentAppState?.toMetadataJson() ?: JSONObject.NULL
            )
            .put(
                "latestCurrentAppStatePackageName",
                latestCurrentAppState?.packageName ?: ""
            )
            .put("pendingCurrentAppStateOutboxRows", pendingCurrentAppStateOutboxItems.size)
            .put("hasWoongCurrentAppState", hasWoongCurrentAppState)
            .put("hasPendingWoongCurrentAppStateOutbox", hasPendingWoongCurrentAppStateOutbox)
            .put("priorExternalChromeMetadataOnly", priorExternalChromeMetadataOnly)
            .put("currentAppStateOutboxMetadataOnly", currentAppStateOutboxMetadataOnly)
            .put(
                "chromeFocusSessions",
                JSONArray(
                    chromeSessions.map { session ->
                        session.toMetadataJson()
                    }
                )
            )
            .put(
                "currentAppStateOutboxItems",
                JSONArray(
                    pendingCurrentAppStateOutboxItems.map { it.toMetadataJson() }
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
        assertTrue(
            "Expected at least one Woong Monitor current_app_states row after returning from Chrome.",
            hasWoongCurrentAppState
        )
        assertTrue(
            "Expected a pending current_app_state sync_outbox item for Woong Monitor.",
            hasPendingWoongCurrentAppStateOutbox
        )
        assertTrue(
            "Expected prior Chrome evidence to remain metadata-only.",
            priorExternalChromeMetadataOnly
        )
        assertTrue(
            "Expected current_app_state outbox payloads to remain metadata-only.",
            currentAppStateOutboxMetadataOnly
        )
    }

    @Test
    fun dashboardAfterChromeReturnShowsWoongAsCurrentAndChromeAsLatestExternal() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        val device = UiDevice.getInstance(instrumentation)
        val arguments = InstrumentationRegistry.getArguments()
        assumeTrue(
            "Dashboard current-focus QA requires scripts/run-android-app-switch-qa.ps1 to prepare Chrome usage evidence first.",
            arguments.containsKey("chromePackageName")
        )
        val monitorPackageName = context.packageName
        val chromePackageName = arguments.getString("chromePackageName") ?: "com.android.chrome"
        val expectedCurrentAppName = AppDisplayNameFormatter.format(monitorPackageName)
        val expectedExternalAppName = AppDisplayNameFormatter.format(chromePackageName)
        val database = MonitorDatabase.getInstance(context)
        val outputDir = outputDir(context)
        outputDir.mkdirs()

        val monitorSessions = database.focusSessionDao().queryByPackage(monitorPackageName, limit = 20)
        val chromeSessions = database.focusSessionDao().queryByPackage(chromePackageName, limit = 20)
        val hasValidMonitorSession = monitorSessions.any { it.isValidUsageStatsSession() }
        val hasValidChromeSession = chromeSessions.any { it.isValidUsageStatsSession() }

        withMainActivityTestGates {
            ActivityScenario.launch(MainActivity::class.java).use { scenario ->
                val values = waitForDashboardCurrentFocusValues(
                    scenario = scenario,
                    device = device,
                    expectedCurrentPackageName = monitorPackageName,
                    expectedExternalPackageName = chromePackageName
                )
                captureScreen(
                    device = device,
                    outputDir = outputDir,
                    baseName = DashboardCurrentFocusAfterChromeReturnBaseName
                )
                Thread.sleep(750)
                val currentAppStates = database.currentAppStateDao()
                    .queryAfterCheckpoint(observedAtUtcMillis = 0L, clientStateId = "", limit = 100)
                val latestCurrentAppState = currentAppStates.latestCurrentAppState()
                val pendingCurrentAppStateOutboxItems =
                    database.pendingCurrentAppStateOutboxItems(monitorPackageName)
                val hasWoongCurrentAppState =
                    currentAppStates.any { it.isValidCurrentAppStateFor(monitorPackageName) }
                val hasLatestWoongCurrentAppState =
                    latestCurrentAppState?.isValidCurrentAppStateFor(monitorPackageName) == true
                val hasPendingWoongCurrentAppStateOutbox =
                    pendingCurrentAppStateOutboxItems.isNotEmpty()
                val currentAppStateOutboxMetadataOnly =
                    pendingCurrentAppStateOutboxItems.isNotEmpty() &&
                        pendingCurrentAppStateOutboxItems.all {
                            it.payloadJson.containsOnlyMetadataFields()
                        }

                val status = if (
                    values.currentAppName == expectedCurrentAppName &&
                    values.currentPackageName == monitorPackageName &&
                    values.latestExternalAppName == expectedExternalAppName &&
                    values.latestExternalPackageName == chromePackageName &&
                    hasValidMonitorSession &&
                    hasValidChromeSession &&
                    hasWoongCurrentAppState &&
                    hasPendingWoongCurrentAppStateOutbox &&
                    currentAppStateOutboxMetadataOnly
                ) {
                    "PASS"
                } else {
                    "FAIL"
                }

                writeJson(
                    File(outputDir, DashboardCurrentFocusEvidenceFileName),
                    JSONObject()
                        .put("status", status)
                        .put("privacy", PrivacyBoundary)
                        .put("expectedCurrentAppText", expectedCurrentAppName)
                        .put("expectedCurrentPackageText", monitorPackageName)
                        .put("actualCurrentAppText", values.currentAppName)
                        .put("actualCurrentPackageText", values.currentPackageName)
                        .put("expectedLatestExternalAppText", expectedExternalAppName)
                        .put("expectedLatestExternalPackageText", chromePackageName)
                        .put("actualLatestExternalAppText", values.latestExternalAppName)
                        .put("actualLatestExternalPackageText", values.latestExternalPackageName)
                        .put("roomFocusSessionMonitorRows", monitorSessions.size)
                        .put("roomFocusSessionChromeRows", chromeSessions.size)
                        .put("hasValidMonitorUsageStatsSession", hasValidMonitorSession)
                        .put("hasValidChromeUsageStatsSession", hasValidChromeSession)
                        .put("currentAppStateRows", currentAppStates.size)
                        .put(
                            "latestCurrentAppState",
                            latestCurrentAppState?.toMetadataJson() ?: JSONObject.NULL
                        )
                        .put(
                            "latestCurrentAppStatePackageName",
                            latestCurrentAppState?.packageName ?: ""
                        )
                        .put(
                            "hasLatestWoongCurrentAppState",
                            hasLatestWoongCurrentAppState
                        )
                        .put(
                            "hasWoongCurrentAppState",
                            hasWoongCurrentAppState
                        )
                        .put(
                            "pendingCurrentAppStateOutboxRows",
                            pendingCurrentAppStateOutboxItems.size
                        )
                        .put(
                            "hasPendingWoongCurrentAppStateOutbox",
                            hasPendingWoongCurrentAppStateOutbox
                        )
                        .put(
                            "currentAppStateOutboxMetadataOnly",
                            currentAppStateOutboxMetadataOnly
                        )
                        .put(
                            "monitorFocusSessions",
                            JSONArray(monitorSessions.map { it.toMetadataJson() })
                        )
                        .put(
                            "chromeFocusSessions",
                            JSONArray(chromeSessions.map { it.toMetadataJson() })
                        )
                        .put(
                            "dashboardScreenshot",
                            "$DashboardCurrentFocusAfterChromeReturnBaseName.png"
                        )
                        .put(
                            "dashboardHierarchy",
                            "$DashboardCurrentFocusAfterChromeReturnBaseName.xml"
                        )
                )

                assertEquals(expectedCurrentAppName, values.currentAppName)
                assertEquals(monitorPackageName, values.currentPackageName)
                assertEquals(expectedExternalAppName, values.latestExternalAppName)
                assertEquals(chromePackageName, values.latestExternalPackageName)
                assertTrue(
                    "Expected at least one valid Woong Monitor UsageStats focus_session row.",
                    hasValidMonitorSession
                )
                assertTrue(
                    "Expected at least one valid Chrome UsageStats focus_session row.",
                    hasValidChromeSession
                )
                assertTrue(
                    "Expected at least one Woong Monitor current_app_states row after returning from Chrome.",
                    hasWoongCurrentAppState
                )
                assertTrue(
                    "Expected pending current_app_state outbox evidence.",
                    hasPendingWoongCurrentAppStateOutbox
                )
                assertTrue(
                    "Expected current_app_state outbox evidence to stay metadata-only.",
                    currentAppStateOutboxMetadataOnly
                )
            }
        }
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

    private fun waitForDashboardCurrentFocusValues(
        scenario: ActivityScenario<MainActivity>,
        device: UiDevice,
        expectedCurrentPackageName: String,
        expectedExternalPackageName: String
    ): DashboardCurrentFocusValues {
        var latestValues = DashboardCurrentFocusValues()

        repeat(20) {
            waitForScreen(device)
            latestValues = readDashboardCurrentFocusValues(scenario)
            if (
                latestValues.currentPackageName == expectedCurrentPackageName &&
                latestValues.latestExternalPackageName == expectedExternalPackageName
            ) {
                return latestValues
            }

            Thread.sleep(250)
        }

        return latestValues
    }

    private fun readDashboardCurrentFocusValues(
        scenario: ActivityScenario<MainActivity>
    ): DashboardCurrentFocusValues {
        var values = DashboardCurrentFocusValues()

        scenario.onActivity { activity ->
            values = DashboardCurrentFocusValues(
                currentAppName = activity.textOf(R.id.currentAppText),
                currentPackageName = activity.textOf(R.id.currentPackageText),
                latestExternalAppName = activity.textOf(R.id.latestCollectedExternalAppText),
                latestExternalPackageName = activity.textOf(
                    R.id.latestCollectedExternalPackageText
                )
            )
        }

        return values
    }

    private fun MainActivity.textOf(viewId: Int): String {
        return findViewById<TextView>(viewId)?.text?.toString().orEmpty()
    }

    private fun FocusSessionEntity.isValidUsageStatsSession(): Boolean {
        return source == "android_usage_stats" &&
            durationMs > 0L &&
            startedAtUtcMillis < endedAtUtcMillis &&
            localDate.isNotBlank() &&
            timezoneId.isNotBlank()
    }

    private fun FocusSessionEntity.toMetadataJson(): JSONObject {
        return JSONObject()
            .put("clientSessionId", clientSessionId)
            .put("packageName", packageName)
            .put("startedAtUtcMillis", startedAtUtcMillis)
            .put("endedAtUtcMillis", endedAtUtcMillis)
            .put("durationMs", durationMs)
            .put("localDate", localDate)
            .put("timezoneId", timezoneId)
            .put("source", source)
    }

    private fun CurrentAppStateEntity.isValidCurrentAppStateFor(packageName: String): Boolean {
        return this.packageName == packageName &&
            appLabel.isNotBlank() &&
            observedAtUtcMillis > 0L &&
            localDate.isNotBlank() &&
            timezoneId.isNotBlank() &&
            source == "android_usage_stats_current_app"
    }

    private fun CurrentAppStateEntity.toMetadataJson(): JSONObject {
        return JSONObject()
            .put("clientStateId", clientStateId)
            .put("packageName", packageName)
            .put("appLabel", appLabel)
            .put("status", status.name)
            .put("observedAtUtcMillis", observedAtUtcMillis)
            .put("localDate", localDate)
            .put("timezoneId", timezoneId)
            .put("source", source)
    }

    private fun SyncOutboxEntity.toMetadataJson(): JSONObject {
        return JSONObject()
            .put("clientItemId", clientItemId)
            .put("aggregateType", aggregateType)
            .put("status", status.name)
            .put("retryCount", retryCount)
            .put("createdAtUtcMillis", createdAtUtcMillis)
            .put("updatedAtUtcMillis", updatedAtUtcMillis)
            .put("payloadMetadataOnly", payloadJson.containsOnlyMetadataFields())
            .put("payloadJson", payloadJson)
    }

    private fun List<CurrentAppStateEntity>.latestCurrentAppState(): CurrentAppStateEntity? {
        return maxWithOrNull(
            compareBy<CurrentAppStateEntity> { it.observedAtUtcMillis }
                .thenBy { it.clientStateId }
        )
    }

    private fun MonitorDatabase.pendingCurrentAppStateOutboxItems(
        packageName: String
    ): List<SyncOutboxEntity> {
        return syncOutboxDao()
            .queryPending(limit = 100)
            .filter { item ->
                item.aggregateType == AndroidOutboxSyncProcessor.CurrentAppStateAggregateType &&
                    item.payloadJson.contains("\"platformAppKey\":\"$packageName\"")
            }
    }

    private fun String.containsOnlyMetadataFields(): Boolean {
        val lower = lowercase()
        return ForbiddenEvidenceFragments.none { fragment -> fragment in lower }
    }

    private fun withMainActivityTestGates(block: () -> Unit) {
        val originalUsageAccessGateFactory = MainActivity.usageAccessGateFactory
        val originalUsageCollectionReconcilerFactory = MainActivity.usageCollectionReconcilerFactory
        val originalUsageImmediateCollectorFactory = MainActivity.usageImmediateCollectorFactory
        val originalForegroundAppStateRecorderFactory =
            MainActivity.foregroundAppStateRecorderFactory
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
        MainActivity.foregroundAppStateRecorderFactory = {
            RoomAndroidForegroundAppStateRecorder.create(it.applicationContext)
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
            MainActivity.foregroundAppStateRecorderFactory =
                originalForegroundAppStateRecorderFactory
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
        private const val DashboardCurrentFocusAfterChromeReturnBaseName =
            "dashboard-current-focus-after-chrome-return"
        private const val DashboardCurrentFocusEvidenceFileName =
            "dashboard-current-focus-evidence.json"
        private const val DashboardAfterAppSwitchScreenshot = "dashboard-after-app-switch.png"
        private const val SessionsAfterAppSwitchScreenshot = "sessions-after-app-switch.png"
        private const val PrivacyBoundary =
            "No Chrome screenshots, no Chrome UI hierarchy, no typed text, no form contents, no browser/page contents."
        private val ForbiddenEvidenceFragments = listOf(
            "windowtitle",
            "window_title",
            "pagetitle",
            "page_title",
            "url",
            "domain",
            "pagecontent",
            "page_content",
            "contenttext",
            "content_text",
            "typedtext",
            "typed_text",
            "clipboard",
            "password",
            "messagebody",
            "message_body",
            "forminput",
            "form_input",
            "touchcoordinate",
            "touch_coordinate",
            "screenshot"
        )
    }

    private data class DashboardCurrentFocusValues(
        val currentAppName: String = "",
        val currentPackageName: String = "",
        val latestExternalAppName: String = "",
        val latestExternalPackageName: String = ""
    )
}
