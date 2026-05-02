package com.woong.monitorstack.dashboard

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class DashboardCurrentFocusResolverTest {
    private val resolver = DashboardCurrentFocusResolver(
        monitorPackageName = "com.woong.monitorstack"
    )

    @Test
    fun resolveShowsRecentlyForegroundChromeAheadOfNexusLauncherNoise() {
        val selection = resolver.resolve(
            listOf(
                row("NexusLauncher", "com.google.android.apps.nexuslauncher", "10:03"),
                row("Chrome", "com.android.chrome", "10:02"),
                row("Settings", "com.android.settings", "09:55")
            )
        )

        assertEquals("Chrome", selection.currentSession?.appName)
        assertEquals("com.android.chrome", selection.currentSession?.packageName)
        assertEquals("Chrome", selection.latestExternalSession?.appName)
    }

    @Test
    fun resolveShowsRecentlyForegroundMonitorAheadOfLauncherNoise() {
        val selection = resolver.resolve(
            listOf(
                row("NexusLauncher", "com.google.android.apps.nexuslauncher", "10:03"),
                row("Woong Monitor", "com.woong.monitorstack", "10:02"),
                row("Chrome", "com.android.chrome", "09:55")
            )
        )

        assertEquals("Woong Monitor", selection.currentSession?.appName)
        assertEquals("com.woong.monitorstack", selection.currentSession?.packageName)
        assertEquals("Chrome", selection.latestExternalSession?.appName)
    }

    @Test
    fun resolveReturnsNoCurrentSessionWhenOnlyLauncherNoiseExists() {
        val selection = resolver.resolve(
            listOf(
                row("NexusLauncher", "com.google.android.apps.nexuslauncher", "10:03"),
                row("System UI", "com.android.systemui", "10:02")
            )
        )

        assertNull(selection.currentSession)
        assertNull(selection.latestExternalSession)
    }

    private fun row(
        appName: String,
        packageName: String,
        startedAtLocalText: String
    ): DashboardSessionRow {
        return DashboardSessionRow(
            appName = appName,
            packageName = packageName,
            startedAtLocalText = startedAtLocalText,
            durationText = "1m",
            durationMs = 60_000L
        )
    }
}
