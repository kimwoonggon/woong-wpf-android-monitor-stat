package com.woong.monitorstack.data.local

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query

@Dao
interface CurrentAppStateDao {
    @Insert(onConflict = OnConflictStrategy.IGNORE)
    fun insert(state: CurrentAppStateEntity): Long

    @Query(
        """
        SELECT *
        FROM current_app_states
        WHERE observedAtUtcMillis > :observedAtUtcMillis
            OR (
                observedAtUtcMillis = :observedAtUtcMillis
                AND clientStateId > :clientStateId
            )
        ORDER BY observedAtUtcMillis ASC, clientStateId ASC
        LIMIT :limit
        """
    )
    fun queryAfterCheckpoint(
        observedAtUtcMillis: Long,
        clientStateId: String,
        limit: Int
    ): List<CurrentAppStateEntity>
}
