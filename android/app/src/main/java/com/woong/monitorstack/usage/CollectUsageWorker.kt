package com.woong.monitorstack.usage

import android.content.Context
import androidx.work.CoroutineWorker
import androidx.work.WorkerParameters
import androidx.work.workDataOf
import com.woong.monitorstack.location.LocationContextCollectionResult
import com.woong.monitorstack.location.LocationContextCollectionRunner
import com.woong.monitorstack.location.LocationContextCollector

class CollectUsageWorker @JvmOverloads constructor(
    appContext: Context,
    workerParams: WorkerParameters,
    private val runner: UsageCollectionRunner = AndroidUsageCollectionRunner.create(appContext),
    private val locationCollector: LocationContextCollector = LocationContextCollectionRunner.create(appContext)
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
            val deviceId = inputData.getString(KEY_DEVICE_ID) ?: DEFAULT_DEVICE_ID
            val locationCaptured = locationCollector.collect(deviceId) == LocationContextCollectionResult.Captured
            Result.success(
                workDataOf(
                    KEY_STORED_SESSION_COUNT to storedCount,
                    KEY_LOCATION_CONTEXT_CAPTURED to locationCaptured
                )
            )
        } catch (_: SecurityException) {
            Result.failure()
        }
    }

    companion object {
        const val KEY_FROM_UTC_MILLIS = "fromUtcMillis"
        const val KEY_TO_UTC_MILLIS = "toUtcMillis"
        const val KEY_DEVICE_ID = "deviceId"
        const val KEY_STORED_SESSION_COUNT = "storedSessionCount"
        const val KEY_LOCATION_CONTEXT_CAPTURED = "locationContextCaptured"
        const val DEFAULT_LOOKBACK_MS = 15 * 60 * 1_000L
        const val DEFAULT_DEVICE_ID = "local-android-device"
    }
}
