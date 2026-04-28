package com.woong.monitorstack.usage

interface UsageEventsReader {
    fun readEvents(fromUtcMillis: Long, toUtcMillis: Long): List<UsageEventSnapshot>
}
