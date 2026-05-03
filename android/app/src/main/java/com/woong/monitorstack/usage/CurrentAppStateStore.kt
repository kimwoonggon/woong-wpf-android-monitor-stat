package com.woong.monitorstack.usage

import com.woong.monitorstack.data.local.CurrentAppStateDao
import com.woong.monitorstack.data.local.CurrentAppStateEntity

interface CurrentAppStateStore {
    suspend fun insert(state: CurrentAppStateEntity)
}

class RoomCurrentAppStateStore(
    private val dao: CurrentAppStateDao
) : CurrentAppStateStore {
    override suspend fun insert(state: CurrentAppStateEntity) {
        dao.insert(state)
    }
}

object NoopCurrentAppStateStore : CurrentAppStateStore {
    override suspend fun insert(state: CurrentAppStateEntity) = Unit
}
