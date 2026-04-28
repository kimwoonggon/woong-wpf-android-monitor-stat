package com.woong.monitorstack.usage

import com.woong.monitorstack.data.local.FocusSessionEntity

interface UsageSessionStore {
    suspend fun insertAll(sessions: List<FocusSessionEntity>)
}
