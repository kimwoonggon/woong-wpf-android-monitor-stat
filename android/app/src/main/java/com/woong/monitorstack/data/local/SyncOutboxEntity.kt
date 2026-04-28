package com.woong.monitorstack.data.local

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "sync_outbox")
data class SyncOutboxEntity(
    @PrimaryKey val clientItemId: String,
    val aggregateType: String,
    val payloadJson: String,
    val status: SyncOutboxStatus,
    val retryCount: Int,
    val lastError: String?,
    val createdAtUtcMillis: Long,
    val updatedAtUtcMillis: Long
)

enum class SyncOutboxStatus {
    Pending,
    Failed,
    Synced
}
