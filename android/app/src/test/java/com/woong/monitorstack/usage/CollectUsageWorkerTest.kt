package com.woong.monitorstack.usage

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import androidx.work.ListenableWorker
import androidx.work.WorkerFactory
import androidx.work.WorkerParameters
import androidx.work.testing.TestListenableWorkerBuilder
import androidx.work.workDataOf
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
                workDataOf(CollectUsageWorker.KEY_STORED_SESSION_COUNT to 2)
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

    private class FakeWorkerFactory(
        private val runner: UsageCollectionRunner
    ) : WorkerFactory() {
        override fun createWorker(
            appContext: Context,
            workerClassName: String,
            workerParameters: WorkerParameters
        ): ListenableWorker? {
            return if (workerClassName == CollectUsageWorker::class.java.name) {
                CollectUsageWorker(appContext, workerParameters, runner)
            } else {
                null
            }
        }
    }
}
