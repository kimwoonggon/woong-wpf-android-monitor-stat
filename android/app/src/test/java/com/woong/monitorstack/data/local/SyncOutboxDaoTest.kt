package com.woong.monitorstack.data.local

import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class SyncOutboxDaoTest {
    private lateinit var database: MonitorDatabase

    @Before
    fun setUp() {
        database = Room.inMemoryDatabaseBuilder(
            ApplicationProvider.getApplicationContext(),
            MonitorDatabase::class.java
        )
            .allowMainThreadQueries()
            .build()
    }

    @After
    fun tearDown() {
        database.close()
    }

    @Test
    fun insertAndMarkSyncedRemovesItemFromPendingQueue() {
        val dao = database.syncOutboxDao()
        dao.insert(
            syncOutboxItem(
                clientItemId = "outbox-1",
                retryCount = 0
            )
        )

        val pending = dao.queryPending(limit = 10)

        assertEquals("outbox-1", pending.single().clientItemId)
        assertEquals("focus_session", pending.single().aggregateType)

        dao.markSynced("outbox-1", updatedAtUtcMillis = 2_000)

        assertTrue(dao.queryPending(limit = 10).isEmpty())
    }

    @Test
    fun markFailedKeepsItemRetryableAndStoresError() {
        val dao = database.syncOutboxDao()
        dao.insert(
            syncOutboxItem(
                clientItemId = "outbox-1",
                retryCount = 1
            )
        )

        dao.markFailed(
            clientItemId = "outbox-1",
            lastError = "server rejected",
            updatedAtUtcMillis = 2_000
        )

        val pending = dao.queryPending(limit = 10).single()
        assertEquals(SyncOutboxStatus.Failed, pending.status)
        assertEquals(2, pending.retryCount)
        assertEquals("server rejected", pending.lastError)
        assertEquals(2_000, pending.updatedAtUtcMillis)
    }

    private fun syncOutboxItem(
        clientItemId: String,
        retryCount: Int
    ): SyncOutboxEntity {
        return SyncOutboxEntity(
            clientItemId = clientItemId,
            aggregateType = "focus_session",
            payloadJson = """{"clientSessionId":"session-1"}""",
            status = SyncOutboxStatus.Pending,
            retryCount = retryCount,
            lastError = null,
            createdAtUtcMillis = 1_000,
            updatedAtUtcMillis = 1_000
        )
    }
}
