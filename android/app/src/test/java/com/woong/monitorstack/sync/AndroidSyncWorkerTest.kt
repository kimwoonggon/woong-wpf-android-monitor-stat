package com.woong.monitorstack.sync

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import androidx.work.Data
import androidx.work.ListenableWorker
import androidx.work.WorkerFactory
import androidx.work.WorkerParameters
import androidx.work.testing.TestListenableWorkerBuilder
import androidx.work.workDataOf
import com.woong.monitorstack.data.local.SyncOutboxEntity
import com.woong.monitorstack.data.local.SyncOutboxStatus
import com.woong.monitorstack.data.local.SyncOutboxStore
import com.woong.monitorstack.settings.AndroidLocationSettings
import com.woong.monitorstack.settings.AndroidSyncSettings
import org.junit.Assert.assertEquals
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import java.io.IOException

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class AndroidSyncWorkerTest {
    @Test
    fun doWorkRunsSyncAndReturnsSyncedAndFailedCounts() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val runner = FakeAndroidSyncRunner(
            result = AndroidOutboxSyncResult(
                syncedCount = 2,
                failedCount = 0
            )
        )
        val worker = TestListenableWorkerBuilder.from(context, AndroidSyncWorker::class.java)
            .setInputData(
                syncInputData(pendingLimit = 25)
            )
            .setWorkerFactory(FakeWorkerFactory(runner))
            .build()

        val result = worker.startWork().get()

        assertEquals(25, runner.limit)
        assertEquals(
            ListenableWorker.Result.success(
                workDataOf(
                    AndroidSyncWorker.KEY_SYNCED_COUNT to 2,
                    AndroidSyncWorker.KEY_FAILED_COUNT to 0
                )
            ),
            result
        )
    }

    @Test
    fun doWorkRetriesWhenAnyOutboxItemFailed() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val runner = FakeAndroidSyncRunner(
            result = AndroidOutboxSyncResult(
                syncedCount = 1,
                failedCount = 1
            )
        )
        val worker = TestListenableWorkerBuilder.from(context, AndroidSyncWorker::class.java)
            .setInputData(syncInputData())
            .setWorkerFactory(FakeWorkerFactory(runner))
            .build()

        val result = worker.startWork().get()

        assertEquals(ListenableWorker.Result.retry(), result)
    }

    @Test
    fun doWorkRetriesWhenRunnerReportsRealUploadIOException() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val runner = ThrowingAndroidSyncRunner(IOException("network unavailable"))
        val worker = TestListenableWorkerBuilder.from(context, AndroidSyncWorker::class.java)
            .setInputData(syncInputData())
            .setWorkerFactory(FakeWorkerFactory(runner))
            .build()

        val result = worker.startWork().get()

        assertEquals(AndroidSyncWorker.DEFAULT_PENDING_LIMIT, runner.limit)
        assertEquals(ListenableWorker.Result.retry(), result)
    }

    @Test
    fun doWorkRetriesAndLeavesOutboxPendingWhenFocusUploadThrowsIOException() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val outbox = FakeSyncOutboxStore(
            listOf(focusOutboxItem("focus-outbox-1", "focus-session-1"))
        )
        val syncApi = ThrowingFocusAndroidSyncApi(IOException("socket closed"))
        val runner = ProcessorBackedAndroidSyncRunner(
            processor = AndroidOutboxSyncProcessor(
                deviceId = "device-1",
                outbox = outbox,
                syncApi = syncApi,
                clock = { 9_000L }
            )
        )
        val worker = TestListenableWorkerBuilder.from(context, AndroidSyncWorker::class.java)
            .setInputData(syncInputData())
            .setWorkerFactory(FakeWorkerFactory(runner, FakeAndroidSyncSettings(isEnabled = true)))
            .build()

        val result = worker.startWork().get()

        assertEquals(ListenableWorker.Result.retry(), result)
        assertEquals(
            "focus-session-1",
            requireNotNull(syncApi.focusRequest).sessions.single().clientSessionId
        )
        assertEquals(emptyList<String>(), outbox.syncedClientItemIds)
        assertEquals(emptyList<FailedOutboxItem>(), outbox.failedItems)
    }

    @Test
    fun doWorkFailsWithAuthRequiredStatusWhenUploadIsUnauthorized() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val runner = ThrowingAndroidSyncRunner(
            AndroidSyncAuthenticationException(
                statusCode = 401,
                message = "Focus session upload failed with HTTP 401."
            )
        )
        val syncSettings = FakeAndroidSyncSettings(isEnabled = true)
        val worker = TestListenableWorkerBuilder.from(context, AndroidSyncWorker::class.java)
            .setInputData(syncInputData())
            .setWorkerFactory(FakeWorkerFactory(runner, syncSettings))
            .build()

        val result = worker.startWork().get()

        assertEquals(AndroidSyncWorker.DEFAULT_PENDING_LIMIT, runner.limit)
        assertEquals(
            ListenableWorker.Result.failure(
                workDataOf(
                    AndroidSyncWorker.KEY_SYNCED_COUNT to 0,
                    AndroidSyncWorker.KEY_FAILED_COUNT to 0,
                    AndroidSyncWorker.KEY_SYNC_STATUS to AndroidSyncWorker.STATUS_AUTH_REQUIRED,
                    AndroidSyncWorker.KEY_SYNC_MESSAGE to
                        "Android sync authorization failed. Register this device again."
                )
            ),
            result
        )
        assertEquals(AndroidSyncWorker.STATUS_AUTH_REQUIRED, syncSettings.recordedStatus)
        assertEquals(
            "Android sync authorization failed. Register this device again.",
            syncSettings.recordedMessage
        )
    }

    @Test
    fun doWorkFailsWithValidationStatusWhenUploadPayloadIsRejected() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val runner = ThrowingAndroidSyncRunner(
            AndroidSyncValidationException(
                statusCode = 422,
                message = "Focus session upload failed with HTTP 422."
            )
        )
        val worker = TestListenableWorkerBuilder.from(context, AndroidSyncWorker::class.java)
            .setInputData(syncInputData())
            .setWorkerFactory(FakeWorkerFactory(runner))
            .build()

        val result = worker.startWork().get()

        assertEquals(AndroidSyncWorker.DEFAULT_PENDING_LIMIT, runner.limit)
        assertEquals(
            ListenableWorker.Result.failure(
                workDataOf(
                    AndroidSyncWorker.KEY_SYNCED_COUNT to 0,
                    AndroidSyncWorker.KEY_FAILED_COUNT to 0,
                    AndroidSyncWorker.KEY_SYNC_STATUS to AndroidSyncWorker.STATUS_VALIDATION_FAILED,
                    AndroidSyncWorker.KEY_SYNC_MESSAGE to
                        "Android sync payload was rejected by the server. Data remains local."
                )
            ),
            result
        )
    }

    @Test
    fun doWorkSkipsSyncWhenSyncOptInIsDisabled() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val runner = FakeAndroidSyncRunner(
            result = AndroidOutboxSyncResult(
                syncedCount = 1,
                failedCount = 0
            )
        )
        val worker = TestListenableWorkerBuilder.from(context, AndroidSyncWorker::class.java)
            .setWorkerFactory(FakeWorkerFactory(runner, FakeAndroidSyncSettings(isEnabled = false)))
            .build()

        val result = worker.startWork().get()

        assertEquals(null, runner.limit)
        assertEquals(
            ListenableWorker.Result.success(
                workDataOf(
                    AndroidSyncWorker.KEY_SYNCED_COUNT to 0,
                    AndroidSyncWorker.KEY_FAILED_COUNT to 0,
                    AndroidSyncWorker.KEY_SYNC_SKIPPED to true
                )
            ),
            result
        )
    }

    @Test
    fun doWorkFailsWithClearStatusAndDoesNotRunSyncWhenWorkerConfigIsMissing() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val runner = FakeAndroidSyncRunner(
            result = AndroidOutboxSyncResult(
                syncedCount = 1,
                failedCount = 0
            )
        )
        val worker = TestListenableWorkerBuilder.from(context, AndroidSyncWorker::class.java)
            .setWorkerFactory(FakeWorkerFactory(runner, FakeAndroidSyncSettings(isEnabled = true)))
            .build()

        val result = worker.startWork().get()

        assertEquals(null, runner.limit)
        assertEquals(
            ListenableWorker.Result.failure(
                workDataOf(
                    AndroidSyncWorker.KEY_SYNCED_COUNT to 0,
                    AndroidSyncWorker.KEY_FAILED_COUNT to 0,
                    AndroidSyncWorker.KEY_SYNC_STATUS to
                        AndroidSyncWorker.STATUS_MISSING_CONFIGURATION,
                    AndroidSyncWorker.KEY_SYNC_MESSAGE to
                        "Android sync is not configured. Missing worker input: KEY_DEVICE_ID, KEY_BASE_URL."
                )
            ),
            result
        )
    }

    @Test
    fun doWorkFailsWithClearStatusAndDoesNotRunSyncWhenDeviceTokenIsMissing() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val runner = FakeAndroidSyncRunner(
            result = AndroidOutboxSyncResult(
                syncedCount = 1,
                failedCount = 0
            )
        )
        val worker = TestListenableWorkerBuilder.from(context, AndroidSyncWorker::class.java)
            .setInputData(syncInputData())
            .setWorkerFactory(
                FakeWorkerFactory(
                    runner,
                    FakeAndroidSyncSettings(isEnabled = true, deviceToken = "")
                )
            )
            .build()

        val result = worker.startWork().get()

        assertEquals(null, runner.limit)
        assertEquals(
            ListenableWorker.Result.failure(
                workDataOf(
                    AndroidSyncWorker.KEY_SYNCED_COUNT to 0,
                    AndroidSyncWorker.KEY_FAILED_COUNT to 0,
                    AndroidSyncWorker.KEY_SYNC_STATUS to AndroidSyncWorker.STATUS_MISSING_TOKEN,
                    AndroidSyncWorker.KEY_SYNC_MESSAGE to
                        "Android sync is not registered. Missing persisted device token."
                )
            ),
            result
        )
    }

    @Test
    fun doWorkSkipsLocationContextOutboxWhenSyncOptInIsDisabled() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val outbox = FakeSyncOutboxStore(
            listOf(locationOutboxItem("location-outbox-1", "location-context-1"))
        )
        val syncApi = FakeAndroidSyncApi(
            locationResult = SyncUploadBatchResult(
                items = listOf(
                    SyncUploadItemResult(
                        clientId = "location-context-1",
                        status = SyncUploadItemStatus.Accepted,
                        errorMessage = null
                    )
                )
            )
        )
        val runner = ProcessorBackedAndroidSyncRunner(
            processor = AndroidOutboxSyncProcessor(
                deviceId = "device-1",
                outbox = outbox,
                syncApi = syncApi,
                locationSettings = FakeLocationSettings(isLocationEnabled = true),
                clock = { 9_000L }
            )
        )
        val worker = TestListenableWorkerBuilder.from(context, AndroidSyncWorker::class.java)
            .setWorkerFactory(FakeWorkerFactory(runner, FakeAndroidSyncSettings(isEnabled = false)))
            .build()

        val result = worker.startWork().get()

        assertEquals(null, runner.limit)
        assertEquals(null, syncApi.locationRequest)
        assertEquals(emptyList<String>(), outbox.syncedClientItemIds)
        assertEquals(emptyList<FailedOutboxItem>(), outbox.failedItems)
        assertEquals(
            ListenableWorker.Result.success(
                workDataOf(
                    AndroidSyncWorker.KEY_SYNCED_COUNT to 0,
                    AndroidSyncWorker.KEY_FAILED_COUNT to 0,
                    AndroidSyncWorker.KEY_SYNC_SKIPPED to true
                )
            ),
            result
        )
    }

    @Test
    fun doWorkUploadsLocationContextOutboxWhenSyncAndLocationOptInAreEnabled() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val outbox = FakeSyncOutboxStore(
            listOf(locationOutboxItem("location-outbox-1", "location-context-1"))
        )
        val syncApi = FakeAndroidSyncApi(
            locationResult = SyncUploadBatchResult(
                items = listOf(
                    SyncUploadItemResult(
                        clientId = "location-context-1",
                        status = SyncUploadItemStatus.Accepted,
                        errorMessage = null
                    )
                )
            )
        )
        val runner = ProcessorBackedAndroidSyncRunner(
            processor = AndroidOutboxSyncProcessor(
                deviceId = "device-1",
                outbox = outbox,
                syncApi = syncApi,
                locationSettings = FakeLocationSettings(isLocationEnabled = true),
                clock = { 9_000L }
            )
        )
        val worker = TestListenableWorkerBuilder.from(context, AndroidSyncWorker::class.java)
            .setInputData(syncInputData())
            .setWorkerFactory(FakeWorkerFactory(runner, FakeAndroidSyncSettings(isEnabled = true)))
            .build()

        val result = worker.startWork().get()

        val request = requireNotNull(syncApi.locationRequest)
        assertEquals("device-1", request.deviceId)
        assertEquals("location-context-1", request.contexts.single().clientContextId)
        assertEquals(37.5665, request.contexts.single().latitude ?: 0.0, 0.0001)
        assertEquals(126.9780, request.contexts.single().longitude ?: 0.0, 0.0001)
        assertEquals(listOf("location-outbox-1"), outbox.syncedClientItemIds)
        assertEquals(emptyList<FailedOutboxItem>(), outbox.failedItems)
        assertEquals(
            ListenableWorker.Result.success(
                workDataOf(
                    AndroidSyncWorker.KEY_SYNCED_COUNT to 1,
                    AndroidSyncWorker.KEY_FAILED_COUNT to 0
                )
            ),
            result
        )
    }

    @Test
    fun doWorkRetriesWhenLocationContextUploadFails() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val outbox = FakeSyncOutboxStore(
            listOf(locationOutboxItem("location-outbox-1", "location-context-1"))
        )
        val syncApi = FakeAndroidSyncApi(
            locationResult = SyncUploadBatchResult(
                items = listOf(
                    SyncUploadItemResult(
                        clientId = "location-context-1",
                        status = SyncUploadItemStatus.Error,
                        errorMessage = "location rejected"
                    )
                )
            )
        )
        val runner = ProcessorBackedAndroidSyncRunner(
            processor = AndroidOutboxSyncProcessor(
                deviceId = "device-1",
                outbox = outbox,
                syncApi = syncApi,
                locationSettings = FakeLocationSettings(isLocationEnabled = true),
                clock = { 9_000L }
            )
        )
        val worker = TestListenableWorkerBuilder.from(context, AndroidSyncWorker::class.java)
            .setInputData(syncInputData())
            .setWorkerFactory(FakeWorkerFactory(runner, FakeAndroidSyncSettings(isEnabled = true)))
            .build()

        val result = worker.startWork().get()

        assertEquals(ListenableWorker.Result.retry(), result)
        assertEquals(emptyList<String>(), outbox.syncedClientItemIds)
        assertEquals(
            listOf(FailedOutboxItem("location-outbox-1", "location rejected", 9_000L)),
            outbox.failedItems
        )
    }

    private fun syncInputData(
        deviceId: String? = "device-1",
        baseUrl: String? = "https://sync.example",
        pendingLimit: Int? = null
    ): Data {
        val values = mutableListOf<Pair<String, Any?>>()
        if (deviceId != null) {
            values += AndroidSyncWorker.KEY_DEVICE_ID to deviceId
        }
        if (baseUrl != null) {
            values += AndroidSyncWorker.KEY_BASE_URL to baseUrl
        }
        if (pendingLimit != null) {
            values += AndroidSyncWorker.KEY_PENDING_LIMIT to pendingLimit
        }
        return workDataOf(*values.toTypedArray())
    }

    private class FakeAndroidSyncRunner(
        private val result: AndroidOutboxSyncResult
    ) : AndroidSyncRunner {
        var limit: Int? = null
            private set

        override suspend fun syncPending(limit: Int): AndroidOutboxSyncResult {
            this.limit = limit
            return result
        }
    }

    private class ThrowingAndroidSyncRunner(
        private val exception: IOException
    ) : AndroidSyncRunner {
        var limit: Int? = null
            private set

        override suspend fun syncPending(limit: Int): AndroidOutboxSyncResult {
            this.limit = limit
            throw exception
        }
    }

    private class FakeWorkerFactory(
        private val runner: AndroidSyncRunner,
        private val syncSettings: AndroidSyncSettings = FakeAndroidSyncSettings(isEnabled = true)
    ) : WorkerFactory() {
        override fun createWorker(
            appContext: Context,
            workerClassName: String,
            workerParameters: WorkerParameters
        ): ListenableWorker? {
            return if (workerClassName == AndroidSyncWorker::class.java.name) {
                AndroidSyncWorker(appContext, workerParameters, runner, syncSettings)
            } else {
                null
            }
        }
    }

    private class FakeAndroidSyncSettings(
        private val isEnabled: Boolean,
        private val deviceToken: String = "device-token-secret"
    ) : AndroidSyncSettings {
        var recordedStatus: String? = null
            private set
        var recordedMessage: String? = null
            private set

        override fun isSyncEnabled(): Boolean = isEnabled
        override fun deviceToken(): String = deviceToken

        override fun recordSyncStatus(status: String, message: String) {
            recordedStatus = status
            recordedMessage = message
        }
    }

    private class ProcessorBackedAndroidSyncRunner(
        private val processor: AndroidOutboxSyncProcessor
    ) : AndroidSyncRunner {
        var limit: Int? = null
            private set

        override suspend fun syncPending(limit: Int): AndroidOutboxSyncResult {
            this.limit = limit
            return processor.processPending(limit)
        }
    }

    private class FakeSyncOutboxStore(
        private val pendingItems: List<SyncOutboxEntity>
    ) : SyncOutboxStore {
        val syncedClientItemIds = mutableListOf<String>()
        val failedItems = mutableListOf<FailedOutboxItem>()

        override fun queryPending(limit: Int): List<SyncOutboxEntity> = pendingItems.take(limit)

        override fun markSynced(
            clientItemId: String,
            updatedAtUtcMillis: Long
        ) {
            syncedClientItemIds += clientItemId
        }

        override fun markFailed(
            clientItemId: String,
            lastError: String,
            updatedAtUtcMillis: Long
        ) {
            failedItems += FailedOutboxItem(clientItemId, lastError, updatedAtUtcMillis)
        }
    }

    private class FakeAndroidSyncApi(
        private val locationResult: SyncUploadBatchResult
    ) : AndroidSyncApi {
        var locationRequest: SyncLocationContextUploadRequest? = null
            private set

        override fun uploadFocusSessions(
            request: SyncFocusSessionUploadRequest
        ): SyncUploadBatchResult {
            throw AssertionError("Focus session upload is not expected in this test.")
        }

        override fun uploadLocationContexts(
            request: SyncLocationContextUploadRequest
        ): SyncUploadBatchResult {
            locationRequest = request
            return locationResult
        }
    }

    private class ThrowingFocusAndroidSyncApi(
        private val exception: IOException
    ) : AndroidSyncApi {
        var focusRequest: SyncFocusSessionUploadRequest? = null
            private set

        override fun uploadFocusSessions(
            request: SyncFocusSessionUploadRequest
        ): SyncUploadBatchResult {
            focusRequest = request
            throw exception
        }

        override fun uploadLocationContexts(
            request: SyncLocationContextUploadRequest
        ): SyncUploadBatchResult {
            throw AssertionError("Location context upload is not expected in this test.")
        }
    }

    private class FakeLocationSettings(
        private val isLocationEnabled: Boolean
    ) : AndroidLocationSettings {
        override fun isLocationCaptureEnabled(): Boolean = isLocationEnabled
        override fun isPreciseLatitudeLongitudeEnabled(): Boolean = false
        override fun isApproximateLocationPreferred(): Boolean = true
    }

    private fun focusOutboxItem(
        clientItemId: String,
        clientSessionId: String
    ): SyncOutboxEntity {
        return SyncOutboxEntity(
            clientItemId = clientItemId,
            aggregateType = AndroidOutboxSyncProcessor.FocusSessionAggregateType,
            payloadJson = """
                {
                  "clientSessionId": "$clientSessionId",
                  "platformAppKey": "com.android.chrome",
                  "startedAtUtc": "2026-04-28T00:00:00Z",
                  "endedAtUtc": "2026-04-28T00:15:00Z",
                  "durationMs": 900000,
                  "localDate": "2026-04-28",
                  "timezoneId": "Asia/Seoul",
                  "isIdle": false,
                  "source": "usage_stats"
                }
            """.trimIndent(),
            status = SyncOutboxStatus.Pending,
            retryCount = 0,
            lastError = null,
            createdAtUtcMillis = 1_000,
            updatedAtUtcMillis = 1_000
        )
    }

    private fun locationOutboxItem(
        clientItemId: String,
        clientContextId: String
    ): SyncOutboxEntity {
        return SyncOutboxEntity(
            clientItemId = clientItemId,
            aggregateType = AndroidOutboxSyncProcessor.LocationContextAggregateType,
            payloadJson = """
                {
                  "clientContextId": "$clientContextId",
                  "capturedAtUtc": "2026-04-28T00:00:00Z",
                  "localDate": "2026-04-28",
                  "timezoneId": "Asia/Seoul",
                  "latitude": 37.5665,
                  "longitude": 126.9780,
                  "accuracyMeters": 35.5,
                  "captureMode": "AppUsageContext",
                  "permissionState": "GrantedApproximate",
                  "source": "android_location_context"
                }
            """.trimIndent(),
            status = SyncOutboxStatus.Pending,
            retryCount = 0,
            lastError = null,
            createdAtUtcMillis = 1_000,
            updatedAtUtcMillis = 1_000
        )
    }

    private data class FailedOutboxItem(
        val clientItemId: String,
        val lastError: String,
        val updatedAtUtcMillis: Long
    )
}
