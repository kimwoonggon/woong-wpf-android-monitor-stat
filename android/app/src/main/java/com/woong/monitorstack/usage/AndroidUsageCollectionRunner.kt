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
    private val timezoneId: ZoneId = ZoneId.systemDefault()
) : UsageCollectionRunner {
    override suspend fun collect(fromUtcMillis: Long, toUtcMillis: Long): Int {
        val sessions = sessionizer.sessionize(collector.collect(fromUtcMillis, toUtcMillis))
        val entities = sessions.map { it.toFocusSessionEntity(timezoneId) }
        store.insertAll(entities)

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
        private const val SOURCE_USAGE_STATS = "usage_stats"

        fun create(context: Context): AndroidUsageCollectionRunner {
            val appContext = context.applicationContext
            val database = MonitorDatabase.getInstance(appContext)

            return AndroidUsageCollectionRunner(
                collector = UsageStatsCollector(AndroidUsageEventsReader(appContext)),
                sessionizer = UsageSessionizer(),
                store = RoomUsageSessionStore(database.focusSessionDao())
            )
        }
    }
}
