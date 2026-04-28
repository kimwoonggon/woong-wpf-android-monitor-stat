package com.woong.monitorstack.summary

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
class MorningSummaryNotificationWorkerTest {
    @Test
    fun doWorkShowsMorningSummaryNotification() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val runner = FakeMorningSummaryNotificationRunner()
        val worker = TestListenableWorkerBuilder.from(
            context,
            MorningSummaryNotificationWorker::class.java
        )
            .setInputData(
                workDataOf(
                    MorningSummaryNotificationWorker.KEY_TITLE to "Yesterday",
                    MorningSummaryNotificationWorker.KEY_TEXT to "Active 15m"
                )
            )
            .setWorkerFactory(FakeWorkerFactory(runner))
            .build()

        val result = worker.startWork().get()

        assertEquals("Yesterday", runner.title)
        assertEquals("Active 15m", runner.text)
        assertEquals(ListenableWorker.Result.success(), result)
    }

    private class FakeMorningSummaryNotificationRunner : MorningSummaryNotificationRunner {
        var title: String? = null
            private set

        var text: String? = null
            private set

        override fun show(
            title: String,
            text: String
        ) {
            this.title = title
            this.text = text
        }
    }

    private class FakeWorkerFactory(
        private val runner: MorningSummaryNotificationRunner
    ) : WorkerFactory() {
        override fun createWorker(
            appContext: Context,
            workerClassName: String,
            workerParameters: WorkerParameters
        ): ListenableWorker? {
            return if (workerClassName == MorningSummaryNotificationWorker::class.java.name) {
                MorningSummaryNotificationWorker(appContext, workerParameters, runner)
            } else {
                null
            }
        }
    }
}
