package com.woong.monitorstack.data.local

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query

@Dao
interface SyncOutboxDao : SyncOutboxStore {
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    fun insert(item: SyncOutboxEntity)

    @Query(
        """
        SELECT *
        FROM sync_outbox
        WHERE status = 'Pending' OR status = 'Failed'
        ORDER BY createdAtUtcMillis
        LIMIT :limit
        """
    )
    override fun queryPending(limit: Int): List<SyncOutboxEntity>

    @Query(
        """
        UPDATE sync_outbox
        SET status = 'Synced',
            updatedAtUtcMillis = :updatedAtUtcMillis
        WHERE clientItemId = :clientItemId
        """
    )
    override fun markSynced(
        clientItemId: String,
        updatedAtUtcMillis: Long
    )

    @Query(
        """
        UPDATE sync_outbox
        SET status = 'Failed',
            retryCount = retryCount + 1,
            lastError = :lastError,
            updatedAtUtcMillis = :updatedAtUtcMillis
        WHERE clientItemId = :clientItemId
        """
    )
    override fun markFailed(
        clientItemId: String,
        lastError: String,
        updatedAtUtcMillis: Long
    )
}
