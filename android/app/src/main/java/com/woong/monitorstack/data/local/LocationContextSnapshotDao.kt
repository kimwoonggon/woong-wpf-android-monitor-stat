package com.woong.monitorstack.data.local

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query

@Dao
interface LocationContextSnapshotDao {
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    fun insert(snapshot: LocationContextSnapshotEntity)

    @Query(
        """
        SELECT *
        FROM location_context_snapshots
        WHERE deviceId = :deviceId
        ORDER BY capturedAtUtcMillis DESC
        LIMIT :limit
        """
    )
    fun queryRecent(
        deviceId: String,
        limit: Int
    ): List<LocationContextSnapshotEntity>

    @Query(
        """
        SELECT *
        FROM location_context_snapshots
        WHERE deviceId = :deviceId
            AND capturedAtUtcMillis >= :fromUtcMillis
            AND capturedAtUtcMillis <= :toUtcMillis
        ORDER BY capturedAtUtcMillis ASC
        """
    )
    fun queryByCapturedRange(
        deviceId: String,
        fromUtcMillis: Long,
        toUtcMillis: Long
    ): List<LocationContextSnapshotEntity>
}
