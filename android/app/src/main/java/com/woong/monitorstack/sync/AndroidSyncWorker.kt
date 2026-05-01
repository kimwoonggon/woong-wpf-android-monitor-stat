package com.woong.monitorstack.sync

import android.content.Context
import androidx.work.CoroutineWorker
import androidx.work.WorkerParameters
import androidx.work.workDataOf
import com.woong.monitorstack.settings.AndroidSyncSettings
import com.woong.monitorstack.settings.SharedPreferencesAndroidSyncSettings
import java.io.IOException

class AndroidSyncWorker @JvmOverloads constructor(
    appContext: Context,
    workerParams: WorkerParameters,
    private val runner: AndroidSyncRunner = AndroidSyncRunnerFactory.create(
        appContext,
        workerParams
    ),
    private val syncSettings: AndroidSyncSettings = SharedPreferencesAndroidSyncSettings(appContext)
) : CoroutineWorker(appContext, workerParams) {
    override suspend fun doWork(): Result {
        val limit = inputData.getInt(KEY_PENDING_LIMIT, DEFAULT_PENDING_LIMIT)
        if (limit <= 0) {
            return Result.failure()
        }
        if (!syncSettings.isSyncEnabled()) {
            return Result.success(
                workDataOf(
                    KEY_SYNCED_COUNT to 0,
                    KEY_FAILED_COUNT to 0,
                    KEY_SYNC_SKIPPED to true
                )
            )
        }

        val missingConfigurationMessage = missingConfigurationMessage()
        if (missingConfigurationMessage != null) {
            return Result.failure(
                workDataOf(
                    KEY_SYNCED_COUNT to 0,
                    KEY_FAILED_COUNT to 0,
                    KEY_SYNC_STATUS to STATUS_MISSING_CONFIGURATION,
                    KEY_SYNC_MESSAGE to missingConfigurationMessage
                )
            )
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

    private fun missingConfigurationMessage(): String? {
        val missingKeys = mutableListOf<String>()
        if (inputData.getString(KEY_DEVICE_ID).isNullOrBlank()) {
            missingKeys += "KEY_DEVICE_ID"
        }
        if (inputData.getString(KEY_BASE_URL).isNullOrBlank()) {
            missingKeys += "KEY_BASE_URL"
        }

        return if (missingKeys.isEmpty()) {
            null
        } else {
            "Android sync is not configured. Missing worker input: ${missingKeys.joinToString(", ")}."
        }
    }

    companion object {
        const val KEY_DEVICE_ID = "deviceId"
        const val KEY_BASE_URL = "baseUrl"
        const val KEY_PENDING_LIMIT = "pendingLimit"
        const val KEY_SYNCED_COUNT = "syncedCount"
        const val KEY_FAILED_COUNT = "failedCount"
        const val KEY_SYNC_SKIPPED = "syncSkipped"
        const val KEY_SYNC_STATUS = "syncStatus"
        const val KEY_SYNC_MESSAGE = "syncMessage"
        const val STATUS_MISSING_CONFIGURATION = "missing_configuration"
        const val DEFAULT_PENDING_LIMIT = 50
    }
}
