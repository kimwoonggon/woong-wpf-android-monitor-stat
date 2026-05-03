package com.woong.monitorstack.data.local

import androidx.room.Entity
import androidx.room.Index
import androidx.room.PrimaryKey

@Entity(
    tableName = "current_app_states",
    indices = [
        Index(
            value = ["observedAtUtcMillis", "clientStateId"],
            name = "index_current_app_states_observed_client"
        )
    ]
)
data class CurrentAppStateEntity(
    @PrimaryKey val clientStateId: String,
    val packageName: String,
    val appLabel: String,
    val status: CurrentAppStateStatus,
    val observedAtUtcMillis: Long,
    val localDate: String,
    val timezoneId: String,
    val source: String,
    val createdAtUtcMillis: Long
)
