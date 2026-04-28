package com.woong.monitorstack.usage

import org.junit.Assert.assertEquals
import org.junit.Test

class UsageStatsCollectorTest {
    @Test
    fun collectReturnsReaderEventsSortedByTimestamp() {
        val reader = FakeUsageEventsReader(
            listOf(
                UsageEventSnapshot("com.slack", UsageEventType.ACTIVITY_RESUMED, 2_000),
                UsageEventSnapshot("com.android.chrome", UsageEventType.ACTIVITY_RESUMED, 1_000)
            )
        )
        val collector = UsageStatsCollector(reader)

        val events = collector.collect(1_000, 3_000)

        assertEquals("com.android.chrome", events[0].packageName)
        assertEquals("com.slack", events[1].packageName)
        assertEquals(1_000L, reader.fromUtcMillis)
        assertEquals(3_000L, reader.toUtcMillis)
    }

    private class FakeUsageEventsReader(
        private val events: List<UsageEventSnapshot>
    ) : UsageEventsReader {
        var fromUtcMillis: Long? = null
            private set

        var toUtcMillis: Long? = null
            private set

        override fun readEvents(fromUtcMillis: Long, toUtcMillis: Long): List<UsageEventSnapshot> {
            this.fromUtcMillis = fromUtcMillis
            this.toUtcMillis = toUtcMillis

            return events
        }
    }
}
