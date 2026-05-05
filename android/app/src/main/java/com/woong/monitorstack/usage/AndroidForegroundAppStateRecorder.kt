package com.woong.monitorstack.usage

import android.content.Context
import com.woong.monitorstack.data.local.CurrentAppStateEntity
import com.woong.monitorstack.data.local.CurrentAppStateStatus
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.display.AppDisplayNameFormatter
import java.time.Instant
import java.time.ZoneId
import kotlinx.coroutines.runBlocking

interface AndroidForegroundAppStateRecorder {
    fun recordForegroundApp(packageName: String)
}

class RoomAndroidForegroundAppStateRecorder(
    private val store: CurrentAppStateStore,
    private val outboxEnqueuer: CurrentAppStateOutboxEnqueuer,
    private val timezoneId: ZoneId = ZoneId.systemDefault(),
    private val clock: () -> Long = { System.currentTimeMillis() },
    private val appLabelFormatter: (String) -> String = AppDisplayNameFormatter::format
) : AndroidForegroundAppStateRecorder {
    override fun recordForegroundApp(packageName: String) {
        val observedAtUtcMillis = clock()
        val state = CurrentAppStateEntity(
            clientStateId = "android-current:$packageName:$observedAtUtcMillis",
            packageName = packageName,
            appLabel = appLabelFormatter(packageName),
            status = CurrentAppStateStatus.Active,
            observedAtUtcMillis = observedAtUtcMillis,
            localDate = Instant.ofEpochMilli(observedAtUtcMillis)
                .atZone(timezoneId)
                .toLocalDate()
                .toString(),
            timezoneId = timezoneId.id,
            source = Source,
            createdAtUtcMillis = observedAtUtcMillis
        )

        runBlocking {
            store.insert(state)
            outboxEnqueuer.enqueueCurrentAppState(state)
        }
    }

    companion object {
        const val Source = "android_usage_stats_current_app"

        fun create(context: Context): AndroidForegroundAppStateRecorder {
            val database = MonitorDatabase.getInstance(context.applicationContext)
            return RoomAndroidForegroundAppStateRecorder(
                store = RoomCurrentAppStateStore(database.currentAppStateDao()),
                outboxEnqueuer = CurrentAppStateSyncOutboxEnqueuer(database.syncOutboxDao())
            )
        }
    }
}

object NoopAndroidForegroundAppStateRecorder : AndroidForegroundAppStateRecorder {
    override fun recordForegroundApp(packageName: String) = Unit
}
