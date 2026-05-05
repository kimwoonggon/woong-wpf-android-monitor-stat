package com.woong.monitorstack.usage

import com.woong.monitorstack.data.local.CurrentAppStateEntity
import com.woong.monitorstack.data.local.CurrentAppStateStatus
import java.time.ZoneId
import org.junit.Assert.assertEquals
import org.junit.Test

class AndroidForegroundAppStateRecorderTest {
    @Test
    fun recordForegroundAppPersistsMetadataCurrentStateAndOutboxPayload() {
        val store = FakeCurrentAppStateStore()
        val outbox = FakeCurrentAppStateOutboxEnqueuer()
        val recorder = RoomAndroidForegroundAppStateRecorder(
            store = store,
            outboxEnqueuer = outbox,
            timezoneId = ZoneId.of("Asia/Seoul"),
            clock = { 1_000L },
            appLabelFormatter = { "Woong Monitor" }
        )

        recorder.recordForegroundApp("com.woong.monitorstack")

        val state = store.states.single()
        assertEquals("android-current:com.woong.monitorstack:1000", state.clientStateId)
        assertEquals("com.woong.monitorstack", state.packageName)
        assertEquals("Woong Monitor", state.appLabel)
        assertEquals(CurrentAppStateStatus.Active, state.status)
        assertEquals(1_000L, state.observedAtUtcMillis)
        assertEquals("1970-01-01", state.localDate)
        assertEquals("Asia/Seoul", state.timezoneId)
        assertEquals(RoomAndroidForegroundAppStateRecorder.Source, state.source)
        assertEquals(listOf(state), outbox.states)
    }

    private class FakeCurrentAppStateStore : CurrentAppStateStore {
        val states = mutableListOf<CurrentAppStateEntity>()

        override suspend fun insert(state: CurrentAppStateEntity) {
            states += state
        }
    }

    private class FakeCurrentAppStateOutboxEnqueuer : CurrentAppStateOutboxEnqueuer {
        val states = mutableListOf<CurrentAppStateEntity>()

        override suspend fun enqueueCurrentAppState(state: CurrentAppStateEntity) {
            states += state
        }
    }
}
