package com.woong.monitorstack.sync

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
class AndroidSyncWorkerTest {
    @Test
    fun doWorkRunsSyncAndReturnsSyncedAndFailedCounts() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val runner = FakeAndroidSyncRunner(
            result = AndroidOutboxSyncResult(
                syncedCount = 2,
                failedCount = 0
            )
        )
        val worker = TestListenableWorkerBuilder.from(context, AndroidSyncWorker::class.java)
            .setInputData(
                workDataOf(
                    AndroidSyncWorker.KEY_PENDING_LIMIT to 25
                )
            )
            .setWorkerFactory(FakeWorkerFactory(runner))
            .build()

        val result = worker.startWork().get()

        assertEquals(25, runner.limit)
        assertEquals(
            ListenableWorker.Result.success(
                workDataOf(
                    AndroidSyncWorker.KEY_SYNCED_COUNT to 2,
                    AndroidSyncWorker.KEY_FAILED_COUNT to 0
                )
            ),
            result
        )
    }

    @Test
    fun doWorkRetriesWhenAnyOutboxItemFailed() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val runner = FakeAndroidSyncRunner(
            result = AndroidOutboxSyncResult(
                syncedCount = 1,
                failedCount = 1
            )
        )
        val worker = TestListenableWorkerBuilder.from(context, AndroidSyncWorker::class.java)
            .setWorkerFactory(FakeWorkerFactory(runner))
            .build()

        val result = worker.startWork().get()

        assertEquals(ListenableWorker.Result.retry(), result)
    }

    private class FakeAndroidSyncRunner(
        private val result: AndroidOutboxSyncResult
    ) : AndroidSyncRunner {
        var limit: Int? = null
            private set

        override suspend fun syncPending(limit: Int): AndroidOutboxSyncResult {
            this.limit = limit
            return result
        }
    }

    private class FakeWorkerFactory(
        private val runner: AndroidSyncRunner
    ) : WorkerFactory() {
        override fun createWorker(
            appContext: Context,
            workerClassName: String,
            workerParameters: WorkerParameters
        ): ListenableWorker? {
            return if (workerClassName == AndroidSyncWorker::class.java.name) {
                AndroidSyncWorker(appContext, workerParameters, runner)
            } else {
                null
            }
        }
    }
}
