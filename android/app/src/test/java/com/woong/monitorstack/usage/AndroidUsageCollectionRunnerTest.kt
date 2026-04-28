package com.woong.monitorstack.usage

import com.woong.monitorstack.data.local.FocusSessionEntity
import java.time.ZoneId
import kotlinx.coroutines.runBlocking
import org.junit.Assert.assertEquals
import org.junit.Test

class AndroidUsageCollectionRunnerTest {
    @Test
    fun collectStoresUsageSessionsAndEnqueuesThemForSync() = runBlocking {
        val store = FakeUsageSessionStore()
        val outbox = FakeUsageSyncOutboxEnqueuer()
        val runner = AndroidUsageCollectionRunner(
            collector = UsageStatsCollector(
                FakeUsageEventsReader(
                    listOf(
                        UsageEventSnapshot(
                            packageName = "com.android.chrome",
                            eventType = UsageEventType.ACTIVITY_RESUMED,
                            occurredAtUtcMillis = 1_000L
                        ),
                        UsageEventSnapshot(
                            packageName = "com.android.chrome",
                            eventType = UsageEventType.ACTIVITY_PAUSED,
                            occurredAtUtcMillis = 61_000L
                        )
                    )
                )
            ),
            sessionizer = UsageSessionizer(),
            store = store,
            timezoneId = ZoneId.of("Asia/Seoul"),
            outboxEnqueuer = outbox
        )

        val collectedCount = runner.collect(1_000L, 61_000L)

        assertEquals(1, collectedCount)
        assertEquals(store.sessions, outbox.sessions)
        assertEquals("com.android.chrome", outbox.sessions.single().packageName)
        assertEquals("android_usage_stats", outbox.sessions.single().source)
    }

    private class FakeUsageEventsReader(
        private val events: List<UsageEventSnapshot>
    ) : UsageEventsReader {
        override fun readEvents(fromUtcMillis: Long, toUtcMillis: Long): List<UsageEventSnapshot> {
            return events
        }
    }

    private class FakeUsageSessionStore : UsageSessionStore {
        val sessions = mutableListOf<FocusSessionEntity>()

        override suspend fun insertAll(sessions: List<FocusSessionEntity>) {
            this.sessions += sessions
        }
    }

    private class FakeUsageSyncOutboxEnqueuer : UsageSyncOutboxEnqueuer {
        val sessions = mutableListOf<FocusSessionEntity>()

        override suspend fun enqueueFocusSessions(sessions: List<FocusSessionEntity>) {
            this.sessions += sessions
        }
    }
}
