package com.woong.monitorstack.sync

import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.data.local.SyncOutboxEntity
import com.woong.monitorstack.data.local.SyncOutboxStatus
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class SyncOutboxIdempotencyTest {
    private var database: MonitorDatabase? = null

    @After
    fun tearDown() {
        database?.close()
    }

    @Test
    fun duplicateInsertDoesNotResetSyncedFocusRowToPending() {
        val db = Room.inMemoryDatabaseBuilder(
            ApplicationProvider.getApplicationContext(),
            MonitorDatabase::class.java
        )
            .allowMainThreadQueries()
            .build()
        database = db

        val dao = db.syncOutboxDao()
        val clientItemId = "focus_session:android:com.android.chrome:1000:61000"
        dao.insert(syncOutboxItem(clientItemId, updatedAtUtcMillis = 1_000L))
        dao.markSynced(clientItemId, updatedAtUtcMillis = 2_000L)

        dao.insert(syncOutboxItem(clientItemId, updatedAtUtcMillis = 3_000L))

        assertEquals(emptyList<SyncOutboxEntity>(), dao.queryPending(limit = 10))
    }

    private fun syncOutboxItem(
        clientItemId: String,
        updatedAtUtcMillis: Long
    ): SyncOutboxEntity {
        return SyncOutboxEntity(
            clientItemId = clientItemId,
            aggregateType = AndroidOutboxSyncProcessor.FocusSessionAggregateType,
            payloadJson = """{"clientSessionId":"android:com.android.chrome:1000:61000"}""",
            status = SyncOutboxStatus.Pending,
            retryCount = 0,
            lastError = null,
            createdAtUtcMillis = 1_000L,
            updatedAtUtcMillis = updatedAtUtcMillis
        )
    }
}
