package com.woong.monitorstack.usage

import android.content.Context

interface UsageCollectionFloor {
    fun floorUtcMillis(): Long
}

interface UsageCollectionResetCheckpoint : UsageCollectionFloor {
    fun markResetAt(utcMillis: Long)
}

class SharedPreferencesUsageCollectionResetCheckpoint(
    context: Context
) : UsageCollectionResetCheckpoint {
    private val preferences = context.applicationContext.getSharedPreferences(
        PreferenceName,
        Context.MODE_PRIVATE
    )

    override fun floorUtcMillis(): Long = preferences.getLong(KeyFloorUtcMillis, 0L)

    override fun markResetAt(utcMillis: Long) {
        preferences.edit()
            .putLong(KeyFloorUtcMillis, utcMillis.coerceAtLeast(0L))
            .apply()
    }

    companion object {
        const val PreferenceName = "woong_monitor_usage_collection_checkpoint"
        private const val KeyFloorUtcMillis = "collection_floor_utc_millis"
    }
}

object NoopUsageCollectionFloor : UsageCollectionFloor {
    override fun floorUtcMillis(): Long = 0L
}
