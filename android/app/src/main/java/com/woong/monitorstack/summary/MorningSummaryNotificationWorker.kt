package com.woong.monitorstack.summary

import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.Context
import android.os.Build
import androidx.core.app.NotificationCompat
import androidx.work.CoroutineWorker
import androidx.work.WorkerParameters

interface MorningSummaryNotificationRunner {
    fun show(
        title: String,
        text: String
    )
}

class MorningSummaryNotificationWorker @JvmOverloads constructor(
    appContext: Context,
    workerParams: WorkerParameters,
    private val runner: MorningSummaryNotificationRunner =
        AndroidMorningSummaryNotificationRunner(appContext)
) : CoroutineWorker(appContext, workerParams) {
    override suspend fun doWork(): Result {
        val title = inputData.getString(KEY_TITLE)
        val text = inputData.getString(KEY_TEXT)

        if (title.isNullOrBlank() || text.isNullOrBlank()) {
            return Result.failure()
        }

        return try {
            runner.show(title, text)
            Result.success()
        } catch (_: SecurityException) {
            Result.failure()
        }
    }

    companion object {
        const val KEY_TITLE = "title"
        const val KEY_TEXT = "text"
    }
}

class AndroidMorningSummaryNotificationRunner(
    private val context: Context
) : MorningSummaryNotificationRunner {
    override fun show(
        title: String,
        text: String
    ) {
        val notificationManager = context.getSystemService(NotificationManager::class.java)
        ensureChannel(notificationManager)
        val notification = NotificationCompat.Builder(context, ChannelId)
            .setSmallIcon(android.R.drawable.ic_dialog_info)
            .setContentTitle(title)
            .setContentText(text)
            .setStyle(NotificationCompat.BigTextStyle().bigText(text))
            .setAutoCancel(true)
            .build()

        notificationManager.notify(NotificationId, notification)
    }

    private fun ensureChannel(notificationManager: NotificationManager) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) {
            return
        }

        val channel = NotificationChannel(
            ChannelId,
            "Morning summaries",
            NotificationManager.IMPORTANCE_DEFAULT
        )
        notificationManager.createNotificationChannel(channel)
    }

    companion object {
        private const val ChannelId = "morning_summary"
        private const val NotificationId = 10_428
    }
}
