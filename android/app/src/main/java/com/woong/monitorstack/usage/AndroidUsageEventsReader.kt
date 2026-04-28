package com.woong.monitorstack.usage

import android.app.usage.UsageEvents
import android.app.usage.UsageStatsManager
import android.content.Context

class AndroidUsageEventsReader(
    private val context: Context
) : UsageEventsReader {
    override fun readEvents(fromUtcMillis: Long, toUtcMillis: Long): List<UsageEventSnapshot> {
        val usageStatsManager = context.getSystemService(Context.USAGE_STATS_SERVICE) as UsageStatsManager
        val usageEvents = usageStatsManager.queryEvents(fromUtcMillis, toUtcMillis)
        val event = UsageEvents.Event()
        val snapshots = mutableListOf<UsageEventSnapshot>()

        while (usageEvents.hasNextEvent()) {
            usageEvents.getNextEvent(event)
            val eventType = when (event.eventType) {
                UsageEvents.Event.ACTIVITY_RESUMED -> UsageEventType.ACTIVITY_RESUMED
                UsageEvents.Event.ACTIVITY_PAUSED -> UsageEventType.ACTIVITY_PAUSED
                else -> null
            }
            val packageName = event.packageName
            if (eventType != null && !packageName.isNullOrBlank()) {
                snapshots.add(
                    UsageEventSnapshot(
                        packageName,
                        eventType,
                        event.timeStamp
                    )
                )
            }
        }

        return snapshots
    }
}
