package com.woong.monitorstack.data.local

interface SyncOutboxStore {
    fun queryPending(limit: Int): List<SyncOutboxEntity>

    fun markSynced(
        clientItemId: String,
        updatedAtUtcMillis: Long
    )

    fun markFailed(
        clientItemId: String,
        lastError: String,
        updatedAtUtcMillis: Long
    )
}
