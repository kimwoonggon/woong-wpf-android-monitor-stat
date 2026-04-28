package com.woong.monitorstack.usage

interface UsageCollectionRunner {
    suspend fun collect(fromUtcMillis: Long, toUtcMillis: Long): Int
}
