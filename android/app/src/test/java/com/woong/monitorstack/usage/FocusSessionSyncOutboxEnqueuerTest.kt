package com.woong.monitorstack.usage

import com.squareup.moshi.Moshi
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.SyncOutboxEntity
import com.woong.monitorstack.data.local.SyncOutboxStatus
import com.woong.monitorstack.data.local.SyncOutboxWriter
import com.woong.monitorstack.sync.AndroidOutboxSyncProcessor
import com.woong.monitorstack.sync.SyncFocusSessionUploadItem
import kotlinx.coroutines.runBlocking
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class FocusSessionSyncOutboxEnqueuerTest {
    @Test
    fun enqueueFocusSessionsWritesPendingFocusSessionUploadPayloads() = runBlocking {
        val writer = FakeSyncOutboxWriter()
        val enqueuer = FocusSessionSyncOutboxEnqueuer(
            outbox = writer,
            clock = { 99_000L }
        )

        enqueuer.enqueueFocusSessions(
            listOf(
                FocusSessionEntity(
                    clientSessionId = "android:com.android.chrome:1000:61000",
                    packageName = "com.android.chrome",
                    startedAtUtcMillis = 1_000L,
                    endedAtUtcMillis = 61_000L,
                    durationMs = 60_000L,
                    localDate = "1970-01-01",
                    timezoneId = "Asia/Seoul",
                    isIdle = false,
                    source = "android_usage_stats"
                )
            )
        )

        val item = writer.items.single()
        assertEquals("focus_session:android:com.android.chrome:1000:61000", item.clientItemId)
        assertEquals(AndroidOutboxSyncProcessor.FocusSessionAggregateType, item.aggregateType)
        assertEquals(SyncOutboxStatus.Pending, item.status)
        assertEquals(0, item.retryCount)
        assertNull(item.lastError)
        assertEquals(99_000L, item.createdAtUtcMillis)
        assertEquals(99_000L, item.updatedAtUtcMillis)

        val payload = Moshi.Builder()
            .add(KotlinJsonAdapterFactory())
            .build()
            .adapter(SyncFocusSessionUploadItem::class.java)
            .fromJson(item.payloadJson)

        requireNotNull(payload)
        assertEquals("android:com.android.chrome:1000:61000", payload.clientSessionId)
        assertEquals("com.android.chrome", payload.platformAppKey)
        assertEquals("1970-01-01T00:00:01Z", payload.startedAtUtc)
        assertEquals("1970-01-01T00:01:01Z", payload.endedAtUtc)
        assertEquals(60_000L, payload.durationMs)
        assertEquals("1970-01-01", payload.localDate)
        assertEquals("Asia/Seoul", payload.timezoneId)
        assertEquals(false, payload.isIdle)
        assertEquals("android_usage_stats", payload.source)
    }

    private class FakeSyncOutboxWriter : SyncOutboxWriter {
        val items = mutableListOf<SyncOutboxEntity>()

        override fun insert(item: SyncOutboxEntity) {
            items += item
        }
    }
}
