package com.woong.monitorstack.usage

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import androidx.work.ListenableWorker
import androidx.work.WorkerFactory
import androidx.work.WorkerParameters
import androidx.work.testing.TestListenableWorkerBuilder
import androidx.work.workDataOf
import com.woong.monitorstack.location.LocationContextCollectionResult
import com.woong.monitorstack.location.LocationContextCollector
import com.woong.monitorstack.location.NoopLocationContextCollector
import org.junit.Assert.assertEquals
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class CollectUsageWorkerTest {
    @Test
    fun doWorkCollectsRequestedRangeAndReturnsStoredCount() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val runner = FakeUsageCollectionRunner(storedCount = 2)
        val worker = TestListenableWorkerBuilder.from(context, CollectUsageWorker::class.java)
            .setInputData(
                workDataOf(
                    CollectUsageWorker.KEY_FROM_UTC_MILLIS to 1_000L,
                    CollectUsageWorker.KEY_TO_UTC_MILLIS to 3_000L
                )
            )
            .setWorkerFactory(FakeWorkerFactory(runner))
            .build()

        val result = worker.startWork().get()

        assertEquals(1_000L, runner.fromUtcMillis)
        assertEquals(3_000L, runner.toUtcMillis)
        assertEquals(
            ListenableWorker.Result.success(
                workDataOf(
                    CollectUsageWorker.KEY_STORED_SESSION_COUNT to 2,
                    CollectUsageWorker.KEY_LOCATION_CONTEXT_CAPTURED to false
                )
            ),
            result
        )
    }

    @Test
    fun doWorkCollectsLocationContextWithDeviceIdAfterUsageCollection() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val runner = FakeUsageCollectionRunner(storedCount = 2)
        val locationCollector = FakeLocationContextCollector(
            result = LocationContextCollectionResult.Captured
        )
        val worker = TestListenableWorkerBuilder.from(context, CollectUsageWorker::class.java)
            .setInputData(
                workDataOf(
                    CollectUsageWorker.KEY_FROM_UTC_MILLIS to 1_000L,
                    CollectUsageWorker.KEY_TO_UTC_MILLIS to 3_000L,
                    CollectUsageWorker.KEY_DEVICE_ID to "android-device-1"
                )
            )
            .setWorkerFactory(FakeWorkerFactory(runner, locationCollector))
            .build()

        val result = worker.startWork().get()

        assertEquals(listOf("android-device-1"), locationCollector.deviceIds)
        assertEquals(
            ListenableWorker.Result.success(
                workDataOf(
                    CollectUsageWorker.KEY_STORED_SESSION_COUNT to 2,
                    CollectUsageWorker.KEY_LOCATION_CONTEXT_CAPTURED to true
                )
            ),
            result
        )
    }

    @Test
    fun doWorkReportsLocationContextSkippedWhenCollectorWritesNothing() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val runner = FakeUsageCollectionRunner(storedCount = 0)
        val locationCollector = FakeLocationContextCollector(
            result = LocationContextCollectionResult.Skipped
        )
        val worker = TestListenableWorkerBuilder.from(context, CollectUsageWorker::class.java)
            .setWorkerFactory(FakeWorkerFactory(runner, locationCollector))
            .build()

        val result = worker.startWork().get()

        assertEquals(listOf(CollectUsageWorker.DEFAULT_DEVICE_ID), locationCollector.deviceIds)
        assertEquals(
            ListenableWorker.Result.success(
                workDataOf(
                    CollectUsageWorker.KEY_STORED_SESSION_COUNT to 0,
                    CollectUsageWorker.KEY_LOCATION_CONTEXT_CAPTURED to false
                )
            ),
            result
        )
    }

    private class FakeUsageCollectionRunner(
        private val storedCount: Int
    ) : UsageCollectionRunner {
        var fromUtcMillis: Long? = null
            private set

        var toUtcMillis: Long? = null
            private set

        override suspend fun collect(fromUtcMillis: Long, toUtcMillis: Long): Int {
            this.fromUtcMillis = fromUtcMillis
            this.toUtcMillis = toUtcMillis

            return storedCount
        }
    }

    private class FakeLocationContextCollector(
        private val result: LocationContextCollectionResult
    ) : LocationContextCollector {
        val deviceIds = mutableListOf<String>()

        override fun collect(deviceId: String): LocationContextCollectionResult {
            deviceIds += deviceId
            return result
        }
    }

    private class FakeWorkerFactory(
        private val runner: UsageCollectionRunner,
        private val locationCollector: LocationContextCollector = NoopLocationContextCollector
    ) : WorkerFactory() {
        override fun createWorker(
            appContext: Context,
            workerClassName: String,
            workerParameters: WorkerParameters
        ): ListenableWorker? {
            return if (workerClassName == CollectUsageWorker::class.java.name) {
                CollectUsageWorker(appContext, workerParameters, runner, locationCollector)
            } else {
                null
            }
        }
    }
}
