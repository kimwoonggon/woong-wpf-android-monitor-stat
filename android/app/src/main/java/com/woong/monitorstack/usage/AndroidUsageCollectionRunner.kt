package com.woong.monitorstack.usage

import android.content.Context
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.MonitorDatabase
import java.time.Instant
import java.time.ZoneId

class AndroidUsageCollectionRunner(
    private val collector: UsageStatsCollector,
    private val sessionizer: UsageSessionizer,
    private val store: UsageSessionStore,
    private val timezoneId: ZoneId = ZoneId.systemDefault(),
    private val outboxEnqueuer: UsageSyncOutboxEnqueuer = NoopUsageSyncOutboxEnqueuer,
    private val anchorLookbackMs: Long = DefaultAnchorLookbackMs,
    private val debugHook: UsageCollectionDebugHook = NoopUsageCollectionDebugHook
) : UsageCollectionRunner {
    init {
        require(anchorLookbackMs >= 0) { "anchorLookbackMs must not be negative." }
    }

    override suspend fun collect(fromUtcMillis: Long, toUtcMillis: Long): Int {
        val anchoredFromUtcMillis = (fromUtcMillis - anchorLookbackMs).coerceAtLeast(0L)
        debugHook.onCollectionWindow(
            UsageCollectionDebugWindow(
                requestedFromUtcMillis = fromUtcMillis,
                requestedToUtcMillis = toUtcMillis,
                queryFromUtcMillis = anchoredFromUtcMillis,
                queryToUtcMillis = toUtcMillis
            )
        )
        val sessions = sessionizer.sessionize(
            events = collector.collect(anchoredFromUtcMillis, toUtcMillis),
            collectionStartUtcMillis = fromUtcMillis,
            collectionEndUtcMillis = toUtcMillis
        )
        val entities = sessions.map { it.toFocusSessionEntity(timezoneId) }
        store.insertAll(entities)
        outboxEnqueuer.enqueueFocusSessions(entities)

        return entities.size
    }

    private fun UsageAppSession.toFocusSessionEntity(timezoneId: ZoneId): FocusSessionEntity {
        return FocusSessionEntity(
            clientSessionId = "android:$packageName:$startedAtUtcMillis:$endedAtUtcMillis",
            packageName = packageName,
            startedAtUtcMillis = startedAtUtcMillis,
            endedAtUtcMillis = endedAtUtcMillis,
            durationMs = durationMs,
            localDate = Instant.ofEpochMilli(startedAtUtcMillis)
                .atZone(timezoneId)
                .toLocalDate()
                .toString(),
            timezoneId = timezoneId.id,
            isIdle = false,
            source = SOURCE_USAGE_STATS
        )
    }

    companion object {
        private const val SOURCE_USAGE_STATS = "android_usage_stats"
        private const val DefaultAnchorLookbackMs = 24 * 60 * 60 * 1_000L

        fun create(context: Context): AndroidUsageCollectionRunner {
            val appContext = context.applicationContext
            val database = MonitorDatabase.getInstance(appContext)

            return AndroidUsageCollectionRunner(
                collector = UsageStatsCollector(AndroidUsageEventsReader(appContext)),
                sessionizer = UsageSessionizer(
                    ignoredPackageNames = AndroidForegroundNoise.PackageNames
                ),
                store = RoomUsageSessionStore(database.focusSessionDao()),
                outboxEnqueuer = FocusSessionSyncOutboxEnqueuer(database.syncOutboxDao())
            )
        }
    }
}

data class UsageCollectionDebugWindow(
    val requestedFromUtcMillis: Long,
    val requestedToUtcMillis: Long,
    val queryFromUtcMillis: Long,
    val queryToUtcMillis: Long
)

interface UsageCollectionDebugHook {
    fun onCollectionWindow(window: UsageCollectionDebugWindow)
}

private object NoopUsageCollectionDebugHook : UsageCollectionDebugHook {
    override fun onCollectionWindow(window: UsageCollectionDebugWindow) = Unit
}
