package com.woong.monitorstack.usage

import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import com.squareup.moshi.Moshi
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.data.local.SyncOutboxEntity
import com.woong.monitorstack.data.local.SyncOutboxStatus
import com.woong.monitorstack.data.local.SyncOutboxWriter
import com.woong.monitorstack.sync.AndroidOutboxSyncProcessor
import com.woong.monitorstack.sync.SyncFocusSessionUploadItem
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class FocusSessionSyncOutboxEnqueuerTest {
    private var database: MonitorDatabase? = null

    @After
    fun tearDown() {
        database?.close()
    }

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

    @Test
    fun enqueueFocusSessionsDoesNotResetSyncedDuplicateToPending() = runBlocking {
        val db = Room.inMemoryDatabaseBuilder(
            ApplicationProvider.getApplicationContext(),
            MonitorDatabase::class.java
        )
            .allowMainThreadQueries()
            .build()
        database = db

        val dao = db.syncOutboxDao()
        val clientItemId = "focus_session:android:com.android.chrome:1000:61000"
        dao.insert(
            SyncOutboxEntity(
                clientItemId = clientItemId,
                aggregateType = AndroidOutboxSyncProcessor.FocusSessionAggregateType,
                payloadJson = """{"clientSessionId":"android:com.android.chrome:1000:61000"}""",
                status = SyncOutboxStatus.Pending,
                retryCount = 0,
                lastError = null,
                createdAtUtcMillis = 1_000L,
                updatedAtUtcMillis = 1_000L
            )
        )
        dao.markSynced(clientItemId, updatedAtUtcMillis = 2_000L)

        val enqueuer = FocusSessionSyncOutboxEnqueuer(
            outbox = dao,
            clock = { 3_000L }
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

        assertEquals(emptyList<SyncOutboxEntity>(), dao.queryPending(limit = 10))
    }

    private class FakeSyncOutboxWriter : SyncOutboxWriter {
        val items = mutableListOf<SyncOutboxEntity>()

        override fun insert(item: SyncOutboxEntity) {
            items += item
        }
    }
}
