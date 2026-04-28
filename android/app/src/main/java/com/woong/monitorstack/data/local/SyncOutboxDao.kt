package com.woong.monitorstack.data.local

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query

@Dao
interface SyncOutboxDao {
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
    fun queryPending(limit: Int): List<SyncOutboxEntity>

    @Query(
        """
        UPDATE sync_outbox
        SET status = 'Synced',
            updatedAtUtcMillis = :updatedAtUtcMillis
        WHERE clientItemId = :clientItemId
        """
    )
    fun markSynced(
        clientItemId: String,
        updatedAtUtcMillis: Long
    )
}
