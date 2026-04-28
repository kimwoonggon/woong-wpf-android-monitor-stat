package com.woong.monitorstack.sessions

import com.woong.monitorstack.data.local.FocusSessionDao
import com.woong.monitorstack.data.local.FocusSessionEntity

class RoomSessionsRepository(
    private val focusSessionDao: FocusSessionDao
) {
    fun loadRecentSessions(limit: Int = DefaultLimit): List<SessionRow> {
        return focusSessionDao.queryRecent(limit)
            .map { it.toSessionRow() }
    }

    private fun FocusSessionEntity.toSessionRow(): SessionRow {
        return SessionRow(
            packageName = packageName,
            durationText = formatDuration(durationMs)
        )
    }

    private fun formatDuration(durationMs: Long): String {
        val totalSeconds = durationMs / 1_000
        val minutes = totalSeconds / 60
        val seconds = totalSeconds % 60

        return when {
            minutes > 0 && seconds > 0 -> "${minutes}m ${seconds}s"
            minutes > 0 -> "${minutes}m"
            else -> "${seconds}s"
        }
    }

    companion object {
        private const val DefaultLimit = 50
    }
}

data class SessionRow(
    val packageName: String,
    val durationText: String
)
