package com.woong.monitorstack.sync

import android.content.Context
import androidx.work.WorkerParameters
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.settings.SharedPreferencesAndroidLocationSettings
import com.woong.monitorstack.settings.SharedPreferencesAndroidSyncSettings
import java.io.IOException

interface AndroidSyncRunner {
    suspend fun syncPending(limit: Int): AndroidOutboxSyncResult
}

class AndroidRoomSyncRunner(
    private val processor: AndroidOutboxSyncProcessor
) : AndroidSyncRunner {
    override suspend fun syncPending(limit: Int): AndroidOutboxSyncResult {
        return processor.processPending(limit)
    }

    companion object {
        fun create(
            context: Context,
            deviceId: String,
            baseUrl: String,
            deviceToken: String
        ): AndroidRoomSyncRunner {
            val database = MonitorDatabase.getInstance(context)
            return AndroidRoomSyncRunner(
                AndroidOutboxSyncProcessor(
                    deviceId = deviceId,
                    outbox = database.syncOutboxDao(),
                    syncApi = AndroidSyncClient(
                        baseUrl = baseUrl,
                        deviceToken = deviceToken
                    ),
                    locationSettings = SharedPreferencesAndroidLocationSettings(context)
                )
            )
        }
    }
}

object AndroidSyncRunnerFactory {
    fun create(
        context: Context,
        workerParams: WorkerParameters
    ): AndroidSyncRunner {
        val deviceId = workerParams.inputData.getString(AndroidSyncWorker.KEY_DEVICE_ID)
        val baseUrl = workerParams.inputData.getString(AndroidSyncWorker.KEY_BASE_URL)
        val deviceToken = SharedPreferencesAndroidSyncSettings(context).deviceToken()

        return if (deviceId.isNullOrBlank() || baseUrl.isNullOrBlank() || deviceToken.isBlank()) {
            MissingAndroidSyncConfigurationRunner
        } else {
            AndroidRoomSyncRunner.create(
                context = context,
                deviceId = deviceId,
                baseUrl = baseUrl,
                deviceToken = deviceToken
            )
        }
    }
}

private object MissingAndroidSyncConfigurationRunner : AndroidSyncRunner {
    override suspend fun syncPending(limit: Int): AndroidOutboxSyncResult {
        throw IOException("Android sync requires deviceId, baseUrl, and persisted deviceToken.")
    }
}
