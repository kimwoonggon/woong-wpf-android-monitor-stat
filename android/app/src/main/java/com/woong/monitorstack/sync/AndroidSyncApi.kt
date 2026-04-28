package com.woong.monitorstack.sync

interface AndroidSyncApi {
    fun uploadFocusSessions(request: SyncFocusSessionUploadRequest): SyncUploadBatchResult
}
