package com.woong.monitorstack.location

import com.squareup.moshi.Moshi
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import com.woong.monitorstack.data.local.LocationCaptureMode
import com.woong.monitorstack.data.local.LocationContextSnapshotDao
import com.woong.monitorstack.data.local.LocationContextSnapshotEntity
import com.woong.monitorstack.data.local.LocationPermissionState
import com.woong.monitorstack.data.local.SyncOutboxEntity
import com.woong.monitorstack.data.local.SyncOutboxStatus
import com.woong.monitorstack.data.local.SyncOutboxWriter
import com.woong.monitorstack.sync.AndroidOutboxSyncProcessor
import com.woong.monitorstack.sync.SyncLocationContextUploadItem
import org.junit.Assert.assertEquals
import org.junit.Test

class LocationContextCollectionRunnerTest {
    private val payloadAdapter = Moshi.Builder()
        .add(KotlinJsonAdapterFactory())
        .build()
        .adapter(SyncLocationContextUploadItem::class.java)

    @Test
    fun collectCapturesSnapshotStoresItAndEnqueuesLocationContextOutbox() {
        val snapshot = locationSnapshot(id = "location-context-1")
        val snapshotDao = FakeLocationContextSnapshotDao()
        val outbox = FakeSyncOutboxWriter()
        val runner = LocationContextCollectionRunner(
            provider = FakeRuntimeLocationSnapshotProvider(snapshot),
            snapshotDao = snapshotDao,
            outbox = outbox,
            timezoneId = "Asia/Seoul",
            clock = { 9_000L }
        )

        val result = runner.collect(deviceId = "android-device-1")

        assertEquals(LocationContextCollectionResult.Captured, result)
        assertEquals(listOf(snapshot), snapshotDao.insertedSnapshots)
        val outboxItem = outbox.insertedItems.single()
        assertEquals("location_context:location-context-1", outboxItem.clientItemId)
        assertEquals(AndroidOutboxSyncProcessor.LocationContextAggregateType, outboxItem.aggregateType)
        assertEquals(SyncOutboxStatus.Pending, outboxItem.status)
        assertEquals(0, outboxItem.retryCount)
        assertEquals(null, outboxItem.lastError)
        assertEquals(9_000L, outboxItem.createdAtUtcMillis)
        assertEquals(9_000L, outboxItem.updatedAtUtcMillis)

        val payload = requireNotNull(payloadAdapter.fromJson(outboxItem.payloadJson))
        assertEquals("location-context-1", payload.clientContextId)
        assertEquals("2026-04-30T00:00:00Z", payload.capturedAtUtc)
        assertEquals("2026-04-30", payload.localDate)
        assertEquals("Asia/Seoul", payload.timezoneId)
        assertEquals(37.5665, payload.latitude ?: 0.0, 0.0001)
        assertEquals(126.9780, payload.longitude ?: 0.0, 0.0001)
        assertEquals("AppUsageContext", payload.captureMode)
        assertEquals("GrantedPrecise", payload.permissionState)
        assertEquals("android_location_context", payload.source)
    }

    @Test
    fun collectWritesNothingWhenProviderReturnsNull() {
        val snapshotDao = FakeLocationContextSnapshotDao()
        val outbox = FakeSyncOutboxWriter()
        val runner = LocationContextCollectionRunner(
            provider = FakeRuntimeLocationSnapshotProvider(null),
            snapshotDao = snapshotDao,
            outbox = outbox
        )

        val result = runner.collect(deviceId = "android-device-1")

        assertEquals(LocationContextCollectionResult.Skipped, result)
        assertEquals(emptyList<LocationContextSnapshotEntity>(), snapshotDao.insertedSnapshots)
        assertEquals(emptyList<SyncOutboxEntity>(), outbox.insertedItems)
    }

    @Test
    fun collectRecordsLocationVisitStatisticsForCoordinateSnapshots() {
        val snapshot = locationSnapshot(id = "location-context-visit")
        val snapshotDao = FakeLocationContextSnapshotDao()
        val outbox = FakeSyncOutboxWriter()
        val visitWriter = FakeLocationVisitWriter()
        val runner = LocationContextCollectionRunner(
            provider = FakeRuntimeLocationSnapshotProvider(snapshot),
            snapshotDao = snapshotDao,
            outbox = outbox,
            locationVisitWriter = visitWriter
        )

        val result = runner.collect(deviceId = "android-device-1")

        assertEquals(LocationContextCollectionResult.Captured, result)
        assertEquals(listOf(snapshot), visitWriter.recordedSnapshots)
    }

    private class FakeRuntimeLocationSnapshotProvider(
        private val snapshot: LocationContextSnapshotEntity?
    ) : RuntimeLocationSnapshotProvider {
        override fun captureSnapshot(deviceId: String): LocationContextSnapshotEntity? = snapshot
    }

    private class FakeLocationContextSnapshotDao : LocationContextSnapshotDao {
        val insertedSnapshots = mutableListOf<LocationContextSnapshotEntity>()

        override fun insert(snapshot: LocationContextSnapshotEntity) {
            insertedSnapshots += snapshot
        }

        override fun queryRecent(
            deviceId: String,
            limit: Int
        ): List<LocationContextSnapshotEntity> = emptyList()

        override fun queryByCapturedRange(
            deviceId: String,
            fromUtcMillis: Long,
            toUtcMillis: Long
        ): List<LocationContextSnapshotEntity> = emptyList()
    }

    private class FakeSyncOutboxWriter : SyncOutboxWriter {
        val insertedItems = mutableListOf<SyncOutboxEntity>()

        override fun insert(item: SyncOutboxEntity) {
            insertedItems += item
        }
    }

    private class FakeLocationVisitWriter : LocationVisitWriter {
        val recordedSnapshots = mutableListOf<LocationContextSnapshotEntity>()

        override fun record(snapshot: LocationContextSnapshotEntity): LocationVisitRecordResult {
            recordedSnapshots += snapshot
            return LocationVisitRecordResult.Created
        }
    }

    private fun locationSnapshot(id: String): LocationContextSnapshotEntity {
        return LocationContextSnapshotEntity(
            id = id,
            deviceId = "android-device-1",
            capturedAtUtcMillis = 1_777_507_200_000L,
            latitude = 37.5665,
            longitude = 126.9780,
            accuracyMeters = 8.5f,
            permissionState = LocationPermissionState.GrantedPrecise,
            captureMode = LocationCaptureMode.AppUsageContext,
            createdAtUtcMillis = 1_777_507_200_000L
        )
    }
}
