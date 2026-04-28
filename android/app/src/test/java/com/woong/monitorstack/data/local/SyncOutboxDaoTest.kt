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
            SyncOutboxEntity(
                clientItemId = "outbox-1",
                aggregateType = "focus_session",
                payloadJson = """{"clientSessionId":"session-1"}""",
                status = SyncOutboxStatus.Pending,
                retryCount = 0,
                lastError = null,
                createdAtUtcMillis = 1_000,
                updatedAtUtcMillis = 1_000
            )
        )

        val pending = dao.queryPending(limit = 10)

        assertEquals("outbox-1", pending.single().clientItemId)
        assertEquals("focus_session", pending.single().aggregateType)

        dao.markSynced("outbox-1", updatedAtUtcMillis = 2_000)

        assertTrue(dao.queryPending(limit = 10).isEmpty())
    }
}
