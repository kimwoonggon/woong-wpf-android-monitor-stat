package com.woong.monitorstack.data.local

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query

@Dao
interface FocusSessionDao {
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    fun insert(session: FocusSessionEntity)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    fun insertAll(sessions: List<FocusSessionEntity>)

    @Query(
        """
        SELECT *
        FROM focus_sessions
        WHERE localDate >= :fromLocalDate AND localDate <= :toLocalDate
        ORDER BY startedAtUtcMillis
        """
    )
    fun queryByLocalDateRange(
        fromLocalDate: String,
        toLocalDate: String
    ): List<FocusSessionEntity>

    @Query(
        """
        SELECT *
        FROM focus_sessions
        ORDER BY startedAtUtcMillis DESC
        LIMIT :limit
        """
    )
    fun queryRecent(limit: Int): List<FocusSessionEntity>
}
