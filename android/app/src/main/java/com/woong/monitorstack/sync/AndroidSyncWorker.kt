package com.woong.monitorstack.sync

import android.content.Context
import androidx.work.CoroutineWorker
import androidx.work.WorkerParameters
import androidx.work.workDataOf
import java.io.IOException

class AndroidSyncWorker @JvmOverloads constructor(
    appContext: Context,
    workerParams: WorkerParameters,
    private val runner: AndroidSyncRunner = AndroidSyncRunnerFactory.create(
        appContext,
        workerParams
    )
) : CoroutineWorker(appContext, workerParams) {
    override suspend fun doWork(): Result {
        val limit = inputData.getInt(KEY_PENDING_LIMIT, DEFAULT_PENDING_LIMIT)
        if (limit <= 0) {
            return Result.failure()
        }

        return try {
            val result = runner.syncPending(limit)
            if (result.failedCount > 0) {
                Result.retry()
            } else {
                Result.success(
                    workDataOf(
                        KEY_SYNCED_COUNT to result.syncedCount,
                        KEY_FAILED_COUNT to result.failedCount
                    )
                )
            }
        } catch (_: IOException) {
            Result.retry()
        }
    }

    companion object {
        const val KEY_DEVICE_ID = "deviceId"
        const val KEY_BASE_URL = "baseUrl"
        const val KEY_PENDING_LIMIT = "pendingLimit"
        const val KEY_SYNCED_COUNT = "syncedCount"
        const val KEY_FAILED_COUNT = "failedCount"
        const val DEFAULT_PENDING_LIMIT = 50
    }
}
