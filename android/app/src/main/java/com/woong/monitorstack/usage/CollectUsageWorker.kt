package com.woong.monitorstack.usage

import android.content.Context
import androidx.work.CoroutineWorker
import androidx.work.WorkerParameters
import androidx.work.workDataOf

class CollectUsageWorker @JvmOverloads constructor(
    appContext: Context,
    workerParams: WorkerParameters,
    private val runner: UsageCollectionRunner = AndroidUsageCollectionRunner.create(appContext)
) : CoroutineWorker(appContext, workerParams) {
    override suspend fun doWork(): Result {
        val toUtcMillis = inputData.getLong(KEY_TO_UTC_MILLIS, System.currentTimeMillis())
        val fromUtcMillis = inputData.getLong(
            KEY_FROM_UTC_MILLIS,
            toUtcMillis - DEFAULT_LOOKBACK_MS
        )

        if (toUtcMillis < fromUtcMillis) {
            return Result.failure()
        }

        return try {
            val storedCount = runner.collect(fromUtcMillis, toUtcMillis)
            Result.success(workDataOf(KEY_STORED_SESSION_COUNT to storedCount))
        } catch (_: SecurityException) {
            Result.failure()
        }
    }

    companion object {
        const val KEY_FROM_UTC_MILLIS = "fromUtcMillis"
        const val KEY_TO_UTC_MILLIS = "toUtcMillis"
        const val KEY_STORED_SESSION_COUNT = "storedSessionCount"
        const val DEFAULT_LOOKBACK_MS = 15 * 60 * 1_000L
    }
}
