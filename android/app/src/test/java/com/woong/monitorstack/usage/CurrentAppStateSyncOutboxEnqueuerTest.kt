package com.woong.monitorstack.usage

import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import com.squareup.moshi.Moshi
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import com.woong.monitorstack.data.local.CurrentAppStateEntity
import com.woong.monitorstack.data.local.CurrentAppStateStatus
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.data.local.SyncOutboxEntity
import com.woong.monitorstack.data.local.SyncOutboxStatus
import com.woong.monitorstack.data.local.SyncOutboxWriter
import com.woong.monitorstack.sync.AndroidOutboxSyncProcessor
import com.woong.monitorstack.sync.SyncCurrentAppStateUploadItem
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
class CurrentAppStateSyncOutboxEnqueuerTest {
    private var database: MonitorDatabase? = null

    @After
    fun tearDown() {
        database?.close()
    }

    @Test
    fun enqueueCurrentAppStateWritesPendingMetadataOnlyUploadPayload() = runBlocking {
        val writer = FakeSyncOutboxWriter()
        val enqueuer = CurrentAppStateSyncOutboxEnqueuer(
            outbox = writer,
            clock = { 99_000L }
        )

        enqueuer.enqueueCurrentAppState(currentAppState())

        val item = writer.items.single()
        assertEquals("current_app_state:android-current:com.android.chrome:1777809600000", item.clientItemId)
        assertEquals(AndroidOutboxSyncProcessor.CurrentAppStateAggregateType, item.aggregateType)
        assertEquals(SyncOutboxStatus.Pending, item.status)
        assertEquals(0, item.retryCount)
        assertNull(item.lastError)
        assertEquals(99_000L, item.createdAtUtcMillis)
        assertEquals(99_000L, item.updatedAtUtcMillis)

        val payload = Moshi.Builder()
            .add(KotlinJsonAdapterFactory())
            .build()
            .adapter(SyncCurrentAppStateUploadItem::class.java)
            .fromJson(item.payloadJson)

        requireNotNull(payload)
        assertEquals("android-current:com.android.chrome:1777809600000", payload.clientStateId)
        assertEquals(2, payload.platform)
        assertEquals("com.android.chrome", payload.platformAppKey)
        assertEquals("2026-05-03T12:00:00Z", payload.observedAtUtc)
        assertEquals("2026-05-03", payload.localDate)
        assertEquals("Asia/Seoul", payload.timezoneId)
        assertEquals("Active", payload.status)
        assertEquals("android_usage_stats_current_app", payload.source)
    }

    @Test
    fun enqueueCurrentAppStateDoesNotResetSyncedDuplicateToPending() = runBlocking {
        val db = Room.inMemoryDatabaseBuilder(
            ApplicationProvider.getApplicationContext(),
            MonitorDatabase::class.java
        )
            .allowMainThreadQueries()
            .build()
        database = db

        val dao = db.syncOutboxDao()
        val clientItemId = "current_app_state:android-current:com.android.chrome:1777809600000"
        dao.insert(
            SyncOutboxEntity(
                clientItemId = clientItemId,
                aggregateType = AndroidOutboxSyncProcessor.CurrentAppStateAggregateType,
                payloadJson = """{"clientStateId":"android-current:com.android.chrome:1777809600000"}""",
                status = SyncOutboxStatus.Pending,
                retryCount = 0,
                lastError = null,
                createdAtUtcMillis = 1_000L,
                updatedAtUtcMillis = 1_000L
            )
        )
        dao.markSynced(clientItemId, updatedAtUtcMillis = 2_000L)

        val enqueuer = CurrentAppStateSyncOutboxEnqueuer(
            outbox = dao,
            clock = { 3_000L }
        )

        enqueuer.enqueueCurrentAppState(currentAppState())

        assertEquals(emptyList<SyncOutboxEntity>(), dao.queryPending(limit = 10))
    }

    private fun currentAppState(): CurrentAppStateEntity {
        return CurrentAppStateEntity(
            clientStateId = "android-current:com.android.chrome:1777809600000",
            packageName = "com.android.chrome",
            appLabel = "Chrome",
            status = CurrentAppStateStatus.Active,
            observedAtUtcMillis = 1_777_809_600_000L,
            localDate = "2026-05-03",
            timezoneId = "Asia/Seoul",
            source = "android_usage_stats_current_app",
            createdAtUtcMillis = 1_777_809_600_000L
        )
    }

    private class FakeSyncOutboxWriter : SyncOutboxWriter {
        val items = mutableListOf<SyncOutboxEntity>()

        override fun insert(item: SyncOutboxEntity) {
            items += item
        }
    }
}
