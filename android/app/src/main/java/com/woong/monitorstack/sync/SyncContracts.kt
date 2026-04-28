package com.woong.monitorstack.sync

import com.squareup.moshi.FromJson
import com.squareup.moshi.ToJson

data class SyncFocusSessionUploadRequest(
    val deviceId: String,
    val sessions: List<SyncFocusSessionUploadItem>
)

data class SyncFocusSessionUploadItem(
    val clientSessionId: String,
    val platformAppKey: String,
    val startedAtUtc: String,
    val endedAtUtc: String,
    val durationMs: Long,
    val localDate: String,
    val timezoneId: String,
    val isIdle: Boolean,
    val source: String
)

data class SyncUploadBatchResult(
    val items: List<SyncUploadItemResult>
)

data class SyncUploadItemResult(
    val clientId: String,
    val status: SyncUploadItemStatus,
    val errorMessage: String?
)

enum class SyncUploadItemStatus(val code: Int) {
    Accepted(1),
    Duplicate(2),
    Error(3);

    companion object {
        fun fromCode(code: Int): SyncUploadItemStatus {
            return entries.firstOrNull { it.code == code }
                ?: throw IllegalArgumentException("Unknown upload item status code: $code")
        }
    }
}

class SyncUploadItemStatusJsonAdapter {
    @FromJson
    fun fromJson(code: Int): SyncUploadItemStatus = SyncUploadItemStatus.fromCode(code)

    @ToJson
    fun toJson(status: SyncUploadItemStatus): Int = status.code
}
