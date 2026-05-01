package com.woong.monitorstack.usage

import com.woong.monitorstack.settings.AndroidUsageCollectionSettings
import org.junit.Assert.assertEquals
import org.junit.Test

class AndroidUsageCollectionSchedulerTest {
    @Test
    fun reconcileCancelsWorkWhenCollectionSettingIsDisabled() {
        val workScheduler = FakeUsageCollectionWorkScheduler()
        val scheduler = AndroidUsageCollectionScheduler(
            usageAccessPermissionChecker = UsageAccessPermissionChecker(
                FakeUsageAccessPermissionReader(isGranted = true)
            ),
            collectionSettings = FakeAndroidUsageCollectionSettings(isEnabled = false),
            workScheduler = workScheduler
        )

        val result = scheduler.reconcile(packageName = "com.woong.monitorstack")

        assertEquals(UsageCollectionScheduleResult.CollectionDisabled, result)
        assertEquals(listOf("cancel"), workScheduler.actions)
    }

    @Test
    fun reconcileCancelsWorkWhenUsageAccessPermissionIsMissing() {
        val workScheduler = FakeUsageCollectionWorkScheduler()
        val scheduler = AndroidUsageCollectionScheduler(
            usageAccessPermissionChecker = UsageAccessPermissionChecker(
                FakeUsageAccessPermissionReader(isGranted = false)
            ),
            collectionSettings = FakeAndroidUsageCollectionSettings(isEnabled = true),
            workScheduler = workScheduler
        )

        val result = scheduler.reconcile(packageName = "com.woong.monitorstack")

        assertEquals(UsageCollectionScheduleResult.UsageAccessMissing, result)
        assertEquals(listOf("cancel"), workScheduler.actions)
    }

    @Test
    fun reconcileSchedulesPeriodicWorkWhenCollectionIsEnabledAndPermissionGranted() {
        val workScheduler = FakeUsageCollectionWorkScheduler()
        val scheduler = AndroidUsageCollectionScheduler(
            usageAccessPermissionChecker = UsageAccessPermissionChecker(
                FakeUsageAccessPermissionReader(isGranted = true)
            ),
            collectionSettings = FakeAndroidUsageCollectionSettings(isEnabled = true),
            workScheduler = workScheduler
        )

        val result = scheduler.reconcile(packageName = "com.woong.monitorstack")

        assertEquals(UsageCollectionScheduleResult.Scheduled, result)
        assertEquals(listOf("schedule", "collectNow"), workScheduler.actions)
    }

    private class FakeUsageAccessPermissionReader(
        private val isGranted: Boolean
    ) : UsageAccessPermissionReader {
        override fun isUsageAccessGranted(packageName: String): Boolean = isGranted
    }

    private class FakeAndroidUsageCollectionSettings(
        private val isEnabled: Boolean
    ) : AndroidUsageCollectionSettings {
        override fun isCollectionEnabled(): Boolean = isEnabled
    }

    private class FakeUsageCollectionWorkScheduler : UsageCollectionWorkScheduler {
        val actions = mutableListOf<String>()

        override fun schedulePeriodicCollection() {
            actions += "schedule"
        }

        override fun scheduleImmediateCollection() {
            actions += "collectNow"
        }

        override fun cancelPeriodicCollection() {
            actions += "cancel"
        }
    }
}
