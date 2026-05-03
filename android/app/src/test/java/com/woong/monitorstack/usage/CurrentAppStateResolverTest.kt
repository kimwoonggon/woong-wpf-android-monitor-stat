package com.woong.monitorstack.usage

import com.woong.monitorstack.data.local.CurrentAppStateStatus
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class CurrentAppStateResolverTest {
    private val resolver = CurrentAppStateResolver()

    @Test
    fun resolveReturnsLatestResumedNonNoisePackageAtCollectionEnd() {
        val snapshot = resolver.resolve(
            events = listOf(
                UsageEventSnapshot(
                    packageName = "com.android.chrome",
                    eventType = UsageEventType.ACTIVITY_RESUMED,
                    occurredAtUtcMillis = 1_000L
                ),
                UsageEventSnapshot(
                    packageName = "com.slack",
                    eventType = UsageEventType.ACTIVITY_RESUMED,
                    occurredAtUtcMillis = 5_000L
                )
            ),
            collectionEndUtcMillis = 10_000L
        )

        assertEquals("com.slack", snapshot?.packageName)
        assertEquals("Slack", snapshot?.appLabel)
        assertEquals(CurrentAppStateStatus.Active, snapshot?.status)
        assertEquals(10_000L, snapshot?.observedAtUtcMillis)
    }

    @Test
    fun resolveReturnsNullWhenLatestPackagePausedBeforeCollectionEnd() {
        val snapshot = resolver.resolve(
            events = listOf(
                UsageEventSnapshot(
                    packageName = "com.android.chrome",
                    eventType = UsageEventType.ACTIVITY_RESUMED,
                    occurredAtUtcMillis = 1_000L
                ),
                UsageEventSnapshot(
                    packageName = "com.android.chrome",
                    eventType = UsageEventType.ACTIVITY_PAUSED,
                    occurredAtUtcMillis = 5_000L
                )
            ),
            collectionEndUtcMillis = 10_000L
        )

        assertNull(snapshot)
    }

    @Test
    fun resolveIgnoresLauncherAndSystemUiNoise() {
        val snapshot = resolver.resolve(
            events = listOf(
                UsageEventSnapshot(
                    packageName = "com.android.chrome",
                    eventType = UsageEventType.ACTIVITY_RESUMED,
                    occurredAtUtcMillis = 1_000L
                ),
                UsageEventSnapshot(
                    packageName = "com.google.android.apps.nexuslauncher",
                    eventType = UsageEventType.ACTIVITY_RESUMED,
                    occurredAtUtcMillis = 5_000L
                ),
                UsageEventSnapshot(
                    packageName = "com.android.systemui",
                    eventType = UsageEventType.ACTIVITY_RESUMED,
                    occurredAtUtcMillis = 6_000L
                )
            ),
            collectionEndUtcMillis = 10_000L
        )

        assertEquals("com.android.chrome", snapshot?.packageName)
        assertEquals("Chrome", snapshot?.appLabel)
        assertEquals(CurrentAppStateStatus.Active, snapshot?.status)
    }
}
