package com.woong.monitorstack.usage

import android.content.Context
import androidx.work.ExistingPeriodicWorkPolicy
import androidx.work.ExistingWorkPolicy
import androidx.work.OneTimeWorkRequestBuilder
import androidx.work.PeriodicWorkRequestBuilder
import androidx.work.WorkManager
import com.woong.monitorstack.settings.AndroidUsageCollectionSettings
import com.woong.monitorstack.settings.SharedPreferencesAndroidUsageCollectionSettings
import java.util.concurrent.TimeUnit

class AndroidUsageCollectionScheduler(
    private val usageAccessPermissionChecker: UsageAccessPermissionChecker,
    private val collectionSettings: AndroidUsageCollectionSettings,
    private val workScheduler: UsageCollectionWorkScheduler
) {
    fun reconcile(packageName: String): UsageCollectionScheduleResult {
        if (!collectionSettings.isCollectionEnabled()) {
            workScheduler.cancelPeriodicCollection()
            return UsageCollectionScheduleResult.CollectionDisabled
        }

        if (!usageAccessPermissionChecker.hasUsageAccess(packageName)) {
            workScheduler.cancelPeriodicCollection()
            return UsageCollectionScheduleResult.UsageAccessMissing
        }

        workScheduler.schedulePeriodicCollection()
        workScheduler.scheduleImmediateCollection()
        return UsageCollectionScheduleResult.Scheduled
    }

    companion object {
        fun create(context: Context): AndroidUsageCollectionScheduler {
            val appContext = context.applicationContext

            return AndroidUsageCollectionScheduler(
                usageAccessPermissionChecker = UsageAccessPermissionChecker(
                    AndroidUsageAccessPermissionReader(appContext)
                ),
                collectionSettings = SharedPreferencesAndroidUsageCollectionSettings(appContext),
                workScheduler = WorkManagerUsageCollectionWorkScheduler(
                    WorkManager.getInstance(appContext)
                )
            )
        }
    }
}

enum class UsageCollectionScheduleResult {
    CollectionDisabled,
    UsageAccessMissing,
    Scheduled
}

interface UsageCollectionWorkScheduler {
    fun schedulePeriodicCollection()

    fun scheduleImmediateCollection()

    fun cancelPeriodicCollection()
}

class WorkManagerUsageCollectionWorkScheduler(
    private val workManager: WorkManager
) : UsageCollectionWorkScheduler {
    override fun schedulePeriodicCollection() {
        val request = PeriodicWorkRequestBuilder<CollectUsageWorker>(
            RepeatIntervalMinutes,
            TimeUnit.MINUTES
        ).build()

        workManager.enqueueUniquePeriodicWork(
            UniqueWorkName,
            ExistingPeriodicWorkPolicy.UPDATE,
            request
        )
    }

    override fun scheduleImmediateCollection() {
        val request = OneTimeWorkRequestBuilder<CollectUsageWorker>().build()

        workManager.enqueueUniqueWork(
            ImmediateWorkName,
            ExistingWorkPolicy.REPLACE,
            request
        )
    }

    override fun cancelPeriodicCollection() {
        workManager.cancelUniqueWork(UniqueWorkName)
        workManager.cancelUniqueWork(ImmediateWorkName)
    }

    companion object {
        const val UniqueWorkName = "usage_collection_periodic"
        const val ImmediateWorkName = "usage_collection_immediate"
        const val RepeatIntervalMinutes = 15L
    }
}
