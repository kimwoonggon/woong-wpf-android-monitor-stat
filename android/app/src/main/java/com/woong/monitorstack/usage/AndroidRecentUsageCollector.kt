package com.woong.monitorstack.usage

import android.content.Context
import kotlinx.coroutines.runBlocking

interface AndroidRecentUsageCollector {
    fun collectRecentUsage(): Int
}

class RunnerBackedAndroidRecentUsageCollector(
    private val runner: UsageCollectionRunner,
    private val clock: () -> Long = { System.currentTimeMillis() },
    private val lookbackMs: Long = CollectUsageWorker.DEFAULT_LOOKBACK_MS,
    private val collectionFloorProvider: () -> Long = { 0L }
) : AndroidRecentUsageCollector {
    override fun collectRecentUsage(): Int {
        val toUtcMillis = clock()
        val fromUtcMillis = maxOf(toUtcMillis - lookbackMs, collectionFloorProvider())

        return runBlocking {
            runner.collect(fromUtcMillis, toUtcMillis)
        }
    }

    companion object {
        fun create(context: Context): RunnerBackedAndroidRecentUsageCollector {
            return RunnerBackedAndroidRecentUsageCollector(
                runner = AndroidUsageCollectionRunner.create(context.applicationContext),
                collectionFloorProvider = {
                    SharedPreferencesUsageCollectionResetCheckpoint(
                        context.applicationContext
                    ).floorUtcMillis()
                }
            )
        }
    }
}
