package com.woong.monitorstack.usage

import org.junit.Assert.assertEquals
import org.junit.Test

class UsageSessionizerTest {
    @Test
    fun sessionizeWhenActivityResumedAndPausedCreatesSession() {
        val sessionizer = UsageSessionizer()
        val events = listOf(
            UsageEventSnapshot("com.android.chrome", UsageEventType.ACTIVITY_RESUMED, 1_000),
            UsageEventSnapshot("com.android.chrome", UsageEventType.ACTIVITY_PAUSED, 61_000)
        )

        val sessions = sessionizer.sessionize(events)

        val session = sessions.single()
        assertEquals("com.android.chrome", session.packageName)
        assertEquals(1_000, session.startedAtUtcMillis)
        assertEquals(61_000, session.endedAtUtcMillis)
        assertEquals(60_000, session.durationMs)
    }

    @Test
    fun sessionizeWhenSameAppEventsAreCloseMergesSessions() {
        val sessionizer = UsageSessionizer(sameAppMergeGapMs = 5_000)
        val events = listOf(
            UsageEventSnapshot("com.android.chrome", UsageEventType.ACTIVITY_RESUMED, 1_000),
            UsageEventSnapshot("com.android.chrome", UsageEventType.ACTIVITY_PAUSED, 10_000),
            UsageEventSnapshot("com.android.chrome", UsageEventType.ACTIVITY_RESUMED, 12_000),
            UsageEventSnapshot("com.android.chrome", UsageEventType.ACTIVITY_PAUSED, 20_000)
        )

        val sessions = sessionizer.sessionize(events)

        val session = sessions.single()
        assertEquals("com.android.chrome", session.packageName)
        assertEquals(1_000, session.startedAtUtcMillis)
        assertEquals(20_000, session.endedAtUtcMillis)
        assertEquals(19_000, session.durationMs)
    }

    @Test
    fun sessionizeWhenDifferentAppStartsClosesPreviousSession() {
        val sessionizer = UsageSessionizer()
        val events = listOf(
            UsageEventSnapshot("com.android.chrome", UsageEventType.ACTIVITY_RESUMED, 1_000),
            UsageEventSnapshot("com.slack", UsageEventType.ACTIVITY_RESUMED, 5_000),
            UsageEventSnapshot("com.slack", UsageEventType.ACTIVITY_PAUSED, 11_000)
        )

        val sessions = sessionizer.sessionize(events)

        assertEquals(2, sessions.size)
        assertEquals("com.android.chrome", sessions[0].packageName)
        assertEquals(1_000, sessions[0].startedAtUtcMillis)
        assertEquals(5_000, sessions[0].endedAtUtcMillis)
        assertEquals("com.slack", sessions[1].packageName)
        assertEquals(5_000, sessions[1].startedAtUtcMillis)
        assertEquals(11_000, sessions[1].endedAtUtcMillis)
    }

    @Test
    fun sessionizeWhenForegroundAppHasNoPauseClosesSessionAtCollectionEnd() {
        val sessionizer = UsageSessionizer()
        val events = listOf(
            UsageEventSnapshot("com.android.chrome", UsageEventType.ACTIVITY_RESUMED, 1_000)
        )

        val sessions = sessionizer.sessionize(
            events = events,
            collectionEndUtcMillis = 61_000
        )

        val session = sessions.single()
        assertEquals("com.android.chrome", session.packageName)
        assertEquals(1_000, session.startedAtUtcMillis)
        assertEquals(61_000, session.endedAtUtcMillis)
        assertEquals(60_000, session.durationMs)
    }
}
