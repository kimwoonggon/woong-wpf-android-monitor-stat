package com.woong.monitorstack.usage

import com.woong.monitorstack.data.local.FocusSessionDao
import com.woong.monitorstack.data.local.FocusSessionEntity

class RoomUsageSessionStore(
    private val dao: FocusSessionDao
) : UsageSessionStore {
    override suspend fun insertAll(sessions: List<FocusSessionEntity>) {
        if (sessions.isNotEmpty()) {
            dao.insertAll(sessions)
        }
    }
}
