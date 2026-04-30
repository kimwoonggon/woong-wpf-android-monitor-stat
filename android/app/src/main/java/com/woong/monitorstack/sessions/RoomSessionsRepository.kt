package com.woong.monitorstack.sessions

import com.woong.monitorstack.data.local.FocusSessionDao
import com.woong.monitorstack.data.local.FocusSessionEntity
import com.woong.monitorstack.display.AppDisplayNameFormatter
import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter

class RoomSessionsRepository(
    private val focusSessionDao: FocusSessionDao
) {
    fun loadRecentSessions(limit: Int = DefaultLimit): List<SessionRow> {
        return focusSessionDao.queryRecent(limit)
            .map { it.toSessionRow() }
    }

    private fun FocusSessionEntity.toSessionRow(): SessionRow {
        return SessionRow(
            appName = AppDisplayNameFormatter.format(packageName),
            packageName = packageName,
            durationText = formatDuration(durationMs),
            timeRangeText = formatTimeRange(this),
            stateText = if (isIdle) "Idle" else "Active"
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
        private val TimeFormatter: DateTimeFormatter = DateTimeFormatter.ofPattern("HH:mm")
    }

    private fun formatTimeRange(entity: FocusSessionEntity): String {
        val zoneId = runCatching { ZoneId.of(entity.timezoneId) }
            .getOrDefault(ZoneId.systemDefault())
        val start = TimeFormatter.format(Instant.ofEpochMilli(entity.startedAtUtcMillis).atZone(zoneId))
        val end = TimeFormatter.format(Instant.ofEpochMilli(entity.endedAtUtcMillis).atZone(zoneId))

        return "$start - $end"
    }
}

data class SessionRow(
    val appName: String,
    val packageName: String,
    val durationText: String,
    val timeRangeText: String,
    val stateText: String
)
