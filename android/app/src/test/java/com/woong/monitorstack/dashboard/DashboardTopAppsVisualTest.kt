package com.woong.monitorstack.dashboard

import android.content.Context
import android.os.Looper
import android.view.View
import android.widget.ProgressBar
import androidx.recyclerview.widget.RecyclerView
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.MainActivity
import com.woong.monitorstack.R
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.usage.AndroidRecentUsageCollector
import com.woong.monitorstack.usage.UsageCollectionScheduleResult
import java.time.LocalDate
import java.time.ZoneId
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.Robolectric
import org.robolectric.RobolectricTestRunner
import org.robolectric.Shadows.shadowOf
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class DashboardTopAppsVisualTest {
    @Before
    fun setUp() {
        MainActivity.splashDelayMillis = 0L
        MainActivity.usageAccessGateFactory = { GrantedUsageAccessGate }
        MainActivity.usageCollectionReconcilerFactory = {
            ScheduledUsageCollectionReconciler
        }
        MainActivity.usageImmediateCollectorFactory = {
            NoopImmediateCollector
        }
    }

    @After
    fun tearDown() {
        MainActivity.usageAccessGateFactory = MainActivity.defaultUsageAccessGateFactory()
        MainActivity.usageCollectionReconcilerFactory =
            MainActivity.defaultUsageCollectionReconcilerFactory()
        MainActivity.usageImmediateCollectorFactory =
            MainActivity.defaultUsageImmediateCollectorFactory()
        MainActivity.splashDelayMillis = MainActivity.DefaultSplashDelayMillis
    }

    @Test
    fun dashboardTopAppsRenderProportionalUsageBars() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        clearMonitorDatabase(context)
        val database = MonitorDatabase.getInstance(context)
        val timezoneId = ZoneId.systemDefault()
        val today = LocalDate.now(timezoneId)

        Thread {
            database.focusSessionDao().insert(
                dashboardSession(
                    clientSessionId = "dashboard-bars-chrome",
                    packageName = "com.android.chrome",
                    localDate = today,
                    durationMs = 40 * 60_000L,
                    timezoneId = timezoneId
                )
            )
            database.focusSessionDao().insert(
                dashboardSession(
                    clientSessionId = "dashboard-bars-youtube",
                    packageName = "com.google.android.youtube",
                    localDate = today,
                    durationMs = 20 * 60_000L,
                    timezoneId = timezoneId
                )
            )
        }.also { thread -> thread.start(); thread.join() }

        val activity = Robolectric.buildActivity(MainActivity::class.java)
            .setup()
            .get()
        activity.supportFragmentManager.executePendingTransactions()
        waitForMainThreadWork()

        val recyclerView = activity.findViewById<RecyclerView>(R.id.topAppsRecyclerView)
        val adapter = recyclerView.adapter
        assertNotNull(adapter)
        assertEquals(2, adapter?.itemCount)

        val topRow = adapter!!.onCreateViewHolder(recyclerView, 0)
        adapter.onBindViewHolder(topRow, 0)
        val secondRow = adapter.onCreateViewHolder(recyclerView, 0)
        adapter.onBindViewHolder(secondRow, 1)

        assertEquals(
            100,
            topRow.itemView.findViewById<ProgressBar>(R.id.appUsageProportionBar).progress
        )
        assertEquals(
            50,
            secondRow.itemView.findViewById<ProgressBar>(R.id.appUsageProportionBar).progress
        )
        assertEquals(
            View.VISIBLE,
            topRow.itemView.findViewById<ProgressBar>(R.id.appUsageProportionBar).visibility
        )
    }

    private fun clearMonitorDatabase(context: Context) {
        Thread {
            MonitorDatabase.getInstance(context).clearAllTables()
        }.also { thread -> thread.start(); thread.join() }
    }

    private fun waitForMainThreadWork() {
        repeat(5) {
            shadowOf(Looper.getMainLooper()).idle()
            Thread.sleep(10)
        }
    }

    private fun dashboardSession(
        clientSessionId: String,
        packageName: String,
        localDate: LocalDate,
        durationMs: Long,
        timezoneId: ZoneId
    ): FocusSessionEntity {
        val startedAtUtcMillis = localDate.atTime(9, 0)
            .atZone(timezoneId)
            .toInstant()
            .toEpochMilli()

        return FocusSessionEntity(
            clientSessionId = clientSessionId,
            packageName = packageName,
            startedAtUtcMillis = startedAtUtcMillis,
            endedAtUtcMillis = startedAtUtcMillis + durationMs,
            durationMs = durationMs,
            localDate = localDate.toString(),
            timezoneId = timezoneId.id,
            isIdle = false,
            source = "dashboard_visual_test"
        )
    }

    private object GrantedUsageAccessGate : MainActivity.UsageAccessGate {
        override fun hasUsageAccess(packageName: String): Boolean = true
    }

    private object ScheduledUsageCollectionReconciler : MainActivity.UsageCollectionReconciler {
        override fun reconcile(packageName: String): UsageCollectionScheduleResult =
            UsageCollectionScheduleResult.Scheduled
    }

    private object NoopImmediateCollector : AndroidRecentUsageCollector {
        override fun collectRecentUsage(): Int = 0
    }
}
