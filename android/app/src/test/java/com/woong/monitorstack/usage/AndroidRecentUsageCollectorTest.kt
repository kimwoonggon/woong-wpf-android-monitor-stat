package com.woong.monitorstack.usage

import org.junit.Assert.assertEquals
import org.junit.Test

class AndroidRecentUsageCollectorTest {
    @Test
    fun collectRecentUsageUsesLookbackWindowEndingAtCurrentTime() {
        val runner = FakeUsageCollectionRunner()
        val collector = RunnerBackedAndroidRecentUsageCollector(
            runner = runner,
            clock = { 100_000L },
            lookbackMs = 30_000L
        )

        val collected = collector.collectRecentUsage()

        assertEquals(3, collected)
        assertEquals(70_000L, runner.fromUtcMillis)
        assertEquals(100_000L, runner.toUtcMillis)
    }

    private class FakeUsageCollectionRunner : UsageCollectionRunner {
        var fromUtcMillis: Long? = null
            private set
        var toUtcMillis: Long? = null
            private set

        override suspend fun collect(fromUtcMillis: Long, toUtcMillis: Long): Int {
            this.fromUtcMillis = fromUtcMillis
            this.toUtcMillis = toUtcMillis
            return 3
        }
    }
}
