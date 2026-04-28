package com.woong.monitorstack.usage

class UsageStatsCollector(
    private val reader: UsageEventsReader
) {
    fun collect(fromUtcMillis: Long, toUtcMillis: Long): List<UsageEventSnapshot> {
        require(toUtcMillis >= fromUtcMillis) {
            "toUtcMillis must be on or after fromUtcMillis."
        }

        return reader.readEvents(fromUtcMillis, toUtcMillis)
            .sortedBy { it.occurredAtUtcMillis }
    }
}
