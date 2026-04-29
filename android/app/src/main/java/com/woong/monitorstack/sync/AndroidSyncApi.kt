package com.woong.monitorstack.sync

interface AndroidSyncApi {
    fun uploadFocusSessions(request: SyncFocusSessionUploadRequest): SyncUploadBatchResult

    fun uploadLocationContexts(request: SyncLocationContextUploadRequest): SyncUploadBatchResult
}
