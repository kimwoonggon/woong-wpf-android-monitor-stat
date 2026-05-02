package com.woong.monitorstack.location

import com.woong.monitorstack.data.local.LocationCaptureMode
import com.woong.monitorstack.data.local.LocationContextSnapshotEntity
import com.woong.monitorstack.data.local.LocationPermissionState
import com.woong.monitorstack.data.local.LocationVisitEntity
import com.woong.monitorstack.data.local.LocationVisitStore
import org.junit.Assert.assertEquals
import org.junit.Test

class LocationVisitRecorderTest {
    @Test
    fun recordMergesSameRoundedLocationWithinGapIntoOneVisit() {
        val store = FakeLocationVisitStore()
        val recorder = LocationVisitRecorder(
            store = store,
            mergeGapMillis = 30 * 60_000L,
            clock = { 2_000L },
            idFactory = { snapshot -> "visit-${snapshot.id}" }
        )

        recorder.record(snapshot(id = "first", capturedAtUtcMillis = 1_000L))
        recorder.record(
            snapshot(
                id = "second",
                capturedAtUtcMillis = 16 * 60_000L,
                latitude = 37.56654,
                longitude = 126.97804,
                accuracyMeters = 8.0f
            )
        )

        assertEquals(1, store.visits.size)
        val visit = store.visits.single()
        assertEquals("visit-first", visit.id)
        assertEquals("37.5665,126.9780", visit.locationKey)
        assertEquals(1_000L, visit.firstCapturedAtUtcMillis)
        assertEquals(16 * 60_000L, visit.lastCapturedAtUtcMillis)
        assertEquals(16 * 60_000L - 1_000L, visit.durationMs)
        assertEquals(2, visit.sampleCount)
        assertEquals(8.0f, visit.accuracyMeters ?: 0f, 0.0001f)
    }

    @Test
    fun recordCreatesNewVisitWhenLocationCellChanges() {
        val store = FakeLocationVisitStore()
        val recorder = LocationVisitRecorder(
            store = store,
            clock = { 2_000L },
            idFactory = { snapshot -> "visit-${snapshot.id}" }
        )

        recorder.record(snapshot(id = "seoul", latitude = 37.5665, longitude = 126.9780))
        recorder.record(snapshot(id = "busan", latitude = 35.1796, longitude = 129.0756))

        assertEquals(listOf("37.5665,126.9780", "35.1796,129.0756"), store.visits.map { it.locationKey })
    }

    @Test
    fun recordSkipsSnapshotsWithoutStoredCoordinates() {
        val store = FakeLocationVisitStore()
        val recorder = LocationVisitRecorder(store = store)

        val result = recorder.record(
            snapshot(id = "no-coordinates", latitude = null, longitude = null)
        )

        assertEquals(LocationVisitRecordResult.SkippedNoCoordinates, result)
        assertEquals(emptyList<LocationVisitEntity>(), store.visits)
    }

    private class FakeLocationVisitStore : LocationVisitStore {
        val visits = mutableListOf<LocationVisitEntity>()

        override fun insert(visit: LocationVisitEntity) {
            visits += visit
        }

        override fun update(visit: LocationVisitEntity) {
            val index = visits.indexOfFirst { it.id == visit.id }
            visits[index] = visit
        }

        override fun findMergeCandidate(
            deviceId: String,
            locationKey: String,
            earliestLastCapturedAtUtcMillis: Long
        ): LocationVisitEntity? {
            return visits
                .filter {
                    it.deviceId == deviceId &&
                        it.locationKey == locationKey &&
                        it.lastCapturedAtUtcMillis >= earliestLastCapturedAtUtcMillis
                }
                .maxByOrNull { it.lastCapturedAtUtcMillis }
        }

        override fun queryByRange(
            deviceId: String,
            fromUtcMillis: Long,
            toUtcMillis: Long
        ): List<LocationVisitEntity> {
            return visits
                .filter {
                    it.deviceId == deviceId &&
                        it.lastCapturedAtUtcMillis >= fromUtcMillis &&
                        it.firstCapturedAtUtcMillis <= toUtcMillis
                }
                .sortedByDescending { it.lastCapturedAtUtcMillis }
        }
    }

    private fun snapshot(
        id: String,
        capturedAtUtcMillis: Long = 1_000L,
        latitude: Double? = 37.5665,
        longitude: Double? = 126.9780,
        accuracyMeters: Float? = 12.0f
    ): LocationContextSnapshotEntity {
        return LocationContextSnapshotEntity(
            id = id,
            deviceId = "android-device-1",
            capturedAtUtcMillis = capturedAtUtcMillis,
            latitude = latitude,
            longitude = longitude,
            accuracyMeters = accuracyMeters,
            permissionState = LocationPermissionState.GrantedPrecise,
            captureMode = LocationCaptureMode.AppUsageContext,
            createdAtUtcMillis = capturedAtUtcMillis
        )
    }
}
