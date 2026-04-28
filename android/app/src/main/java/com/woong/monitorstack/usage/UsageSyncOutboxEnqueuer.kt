package com.woong.monitorstack.usage

import com.woong.monitorstack.data.local.FocusSessionEntity

interface UsageSyncOutboxEnqueuer {
    suspend fun enqueueFocusSessions(sessions: List<FocusSessionEntity>)
}

object NoopUsageSyncOutboxEnqueuer : UsageSyncOutboxEnqueuer {
    override suspend fun enqueueFocusSessions(sessions: List<FocusSessionEntity>) = Unit
}
