package com.woong.monitorstack.data.local

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import androidx.room.Update

interface LocationVisitStore {
    fun insert(visit: LocationVisitEntity)

    fun update(visit: LocationVisitEntity)

    fun findMergeCandidate(
        deviceId: String,
        locationKey: String,
        earliestLastCapturedAtUtcMillis: Long
    ): LocationVisitEntity?

    fun queryByRange(
        deviceId: String,
        fromUtcMillis: Long,
        toUtcMillis: Long
    ): List<LocationVisitEntity>
}

@Dao
interface LocationVisitDao : LocationVisitStore {
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    override fun insert(visit: LocationVisitEntity)

    @Update
    override fun update(visit: LocationVisitEntity)

    @Query(
        """
        SELECT *
        FROM location_visits
        WHERE deviceId = :deviceId
            AND locationKey = :locationKey
            AND lastCapturedAtUtcMillis >= :earliestLastCapturedAtUtcMillis
        ORDER BY lastCapturedAtUtcMillis DESC
        LIMIT 1
        """
    )
    override fun findMergeCandidate(
        deviceId: String,
        locationKey: String,
        earliestLastCapturedAtUtcMillis: Long
    ): LocationVisitEntity?

    @Query(
        """
        SELECT *
        FROM location_visits
        WHERE deviceId = :deviceId
            AND lastCapturedAtUtcMillis >= :fromUtcMillis
            AND firstCapturedAtUtcMillis <= :toUtcMillis
        ORDER BY durationMs DESC, lastCapturedAtUtcMillis DESC
        """
    )
    override fun queryByRange(
        deviceId: String,
        fromUtcMillis: Long,
        toUtcMillis: Long
    ): List<LocationVisitEntity>
}
