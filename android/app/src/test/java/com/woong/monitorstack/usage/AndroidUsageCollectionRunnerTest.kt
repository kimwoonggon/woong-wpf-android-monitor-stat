package com.woong.monitorstack.usage

import com.woong.monitorstack.data.local.CurrentAppStateEntity
import com.woong.monitorstack.data.local.CurrentAppStateStatus
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

    @Test
    fun collectReadsAnchoredLookbackAndStoresOnlyRequestedWindow() = runBlocking {
        val store = FakeUsageSessionStore()
        val reader = FakeUsageEventsReader(
            listOf(
                UsageEventSnapshot(
                    packageName = "com.android.chrome",
                    eventType = UsageEventType.ACTIVITY_RESUMED,
                    occurredAtUtcMillis = 1_000L
                ),
                UsageEventSnapshot(
                    packageName = "com.android.chrome",
                    eventType = UsageEventType.ACTIVITY_PAUSED,
                    occurredAtUtcMillis = 20_000L
                )
            )
        )
        val runner = AndroidUsageCollectionRunner(
            collector = UsageStatsCollector(reader),
            sessionizer = UsageSessionizer(),
            store = store,
            timezoneId = ZoneId.of("Asia/Seoul"),
            anchorLookbackMs = 9_000L
        )

        val collectedCount = runner.collect(10_000L, 30_000L)

        assertEquals(1, collectedCount)
        assertEquals(1_000L, reader.fromUtcMillis)
        assertEquals(30_000L, reader.toUtcMillis)
        val session = store.sessions.single()
        assertEquals("com.android.chrome", session.packageName)
        assertEquals(10_000L, session.startedAtUtcMillis)
        assertEquals(20_000L, session.endedAtUtcMillis)
        assertEquals(10_000L, session.durationMs)
    }

    @Test
    fun collectReportsRequestedAndAnchoredQueryWindowToDebugHook() = runBlocking {
        val debugHook = RecordingUsageCollectionDebugHook()
        val runner = AndroidUsageCollectionRunner(
            collector = UsageStatsCollector(FakeUsageEventsReader(emptyList<UsageEventSnapshot>())),
            sessionizer = UsageSessionizer(),
            store = FakeUsageSessionStore(),
            timezoneId = ZoneId.of("Asia/Seoul"),
            anchorLookbackMs = 9_000L,
            debugHook = debugHook
        )

        runner.collect(10_000L, 30_000L)

        assertEquals(
            UsageCollectionDebugWindow(
                requestedFromUtcMillis = 10_000L,
                requestedToUtcMillis = 30_000L,
                queryFromUtcMillis = 1_000L,
                queryToUtcMillis = 30_000L
            ),
            debugHook.windows.single()
        )
    }

    @Test
    fun collectPersistsCurrentAppSnapshotFromLatestActivePackage() = runBlocking {
        val currentAppStateStore = FakeCurrentAppStateStore()
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
                            packageName = "com.slack",
                            eventType = UsageEventType.ACTIVITY_RESUMED,
                            occurredAtUtcMillis = 5_000L
                        )
                    )
                )
            ),
            sessionizer = UsageSessionizer(),
            store = FakeUsageSessionStore(),
            timezoneId = ZoneId.of("Asia/Seoul"),
            currentAppStateStore = currentAppStateStore
        )

        runner.collect(1_000L, 10_000L)

        val state = currentAppStateStore.states.single()
        assertEquals("android-current:com.slack:10000", state.clientStateId)
        assertEquals("com.slack", state.packageName)
        assertEquals("Slack", state.appLabel)
        assertEquals(CurrentAppStateStatus.Active, state.status)
        assertEquals(10_000L, state.observedAtUtcMillis)
        assertEquals("1970-01-01", state.localDate)
        assertEquals("Asia/Seoul", state.timezoneId)
        assertEquals("android_usage_stats_current_app", state.source)
    }

    @Test
    fun collectEnqueuesCurrentAppSnapshotForSync() = runBlocking {
        val currentAppStateOutbox = FakeCurrentAppStateOutboxEnqueuer()
        val runner = AndroidUsageCollectionRunner(
            collector = UsageStatsCollector(
                FakeUsageEventsReader(
                    listOf(
                        UsageEventSnapshot(
                            packageName = "com.android.chrome",
                            eventType = UsageEventType.ACTIVITY_RESUMED,
                            occurredAtUtcMillis = 1_000L
                        )
                    )
                )
            ),
            sessionizer = UsageSessionizer(),
            store = FakeUsageSessionStore(),
            timezoneId = ZoneId.of("Asia/Seoul"),
            currentAppStateOutboxEnqueuer = currentAppStateOutbox
        )

        runner.collect(1_000L, 10_000L)

        val state = currentAppStateOutbox.states.single()
        assertEquals("android-current:com.android.chrome:10000", state.clientStateId)
        assertEquals("com.android.chrome", state.packageName)
        assertEquals("Chrome", state.appLabel)
        assertEquals("android_usage_stats_current_app", state.source)
    }

    @Test
    fun collectDoesNotPersistCurrentAppSnapshotWhenNoActivePackageExists() = runBlocking {
        val currentAppStateStore = FakeCurrentAppStateStore()
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
                            occurredAtUtcMillis = 5_000L
                        )
                    )
                )
            ),
            sessionizer = UsageSessionizer(),
            store = FakeUsageSessionStore(),
            timezoneId = ZoneId.of("Asia/Seoul"),
            currentAppStateStore = currentAppStateStore
        )

        runner.collect(1_000L, 10_000L)

        assertEquals(emptyList<CurrentAppStateEntity>(), currentAppStateStore.states)
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

    private class FakeCurrentAppStateStore : CurrentAppStateStore {
        val states = mutableListOf<CurrentAppStateEntity>()

        override suspend fun insert(state: CurrentAppStateEntity) {
            states += state
        }
    }

    private class FakeCurrentAppStateOutboxEnqueuer : CurrentAppStateOutboxEnqueuer {
        val states = mutableListOf<CurrentAppStateEntity>()

        override suspend fun enqueueCurrentAppState(state: CurrentAppStateEntity) {
            states += state
        }
    }

    private class RecordingUsageCollectionDebugHook : UsageCollectionDebugHook {
        val windows = mutableListOf<UsageCollectionDebugWindow>()

        override fun onCollectionWindow(window: UsageCollectionDebugWindow) {
            windows += window
        }
    }
}
