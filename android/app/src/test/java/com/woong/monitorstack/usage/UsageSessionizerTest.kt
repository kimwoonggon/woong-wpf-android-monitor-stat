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
}
