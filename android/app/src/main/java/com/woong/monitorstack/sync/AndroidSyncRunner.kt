package com.woong.monitorstack.sync

import android.content.Context
import androidx.work.WorkerParameters
import com.woong.monitorstack.data.local.MonitorDatabase
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
            baseUrl: String
        ): AndroidRoomSyncRunner {
            val database = MonitorDatabase.getInstance(context)
            return AndroidRoomSyncRunner(
                AndroidOutboxSyncProcessor(
                    deviceId = deviceId,
                    outbox = database.syncOutboxDao(),
                    syncApi = AndroidSyncClient(baseUrl)
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

        return if (deviceId.isNullOrBlank() || baseUrl.isNullOrBlank()) {
            MissingAndroidSyncConfigurationRunner
        } else {
            AndroidRoomSyncRunner.create(
                context = context,
                deviceId = deviceId,
                baseUrl = baseUrl
            )
        }
    }
}

private object MissingAndroidSyncConfigurationRunner : AndroidSyncRunner {
    override suspend fun syncPending(limit: Int): AndroidOutboxSyncResult {
        throw IOException("Android sync requires deviceId and baseUrl input data.")
    }
}
