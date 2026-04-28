package com.woong.monitorstack.data.local

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "focus_sessions")
data class FocusSessionEntity(
    @PrimaryKey val clientSessionId: String,
    val packageName: String,
    val startedAtUtcMillis: Long,
    val endedAtUtcMillis: Long,
    val durationMs: Long,
    val localDate: String,
    val timezoneId: String,
    val isIdle: Boolean,
    val source: String
)
