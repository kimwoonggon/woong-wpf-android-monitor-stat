package com.woong.monitorstack.location

import com.woong.monitorstack.data.local.LocationContextSnapshotEntity
import com.woong.monitorstack.data.local.LocationVisitEntity
import com.woong.monitorstack.data.local.LocationVisitStore
import java.util.Locale
import java.util.UUID

interface LocationVisitWriter {
    fun record(snapshot: LocationContextSnapshotEntity): LocationVisitRecordResult
}

object NoopLocationVisitWriter : LocationVisitWriter {
    override fun record(snapshot: LocationContextSnapshotEntity): LocationVisitRecordResult {
        return LocationVisitRecordResult.SkippedNoRecorder
    }
}

class LocationVisitRecorder(
    private val store: LocationVisitStore,
    private val mergeGapMillis: Long = DefaultMergeGapMillis,
    private val coordinatePrecisionDecimals: Int = DefaultCoordinatePrecisionDecimals,
    private val clock: () -> Long = System::currentTimeMillis,
    private val idFactory: (LocationContextSnapshotEntity) -> String = {
        UUID.randomUUID().toString()
    }
) : LocationVisitWriter {
    override fun record(snapshot: LocationContextSnapshotEntity): LocationVisitRecordResult {
        val latitude = snapshot.latitude ?: return LocationVisitRecordResult.SkippedNoCoordinates
        val longitude = snapshot.longitude ?: return LocationVisitRecordResult.SkippedNoCoordinates
        val locationKey = locationKey(latitude, longitude)
        val roundedLatitude = locationKey.substringBefore(",").toDouble()
        val roundedLongitude = locationKey.substringAfter(",").toDouble()
        val earliestMergeTime = snapshot.capturedAtUtcMillis - mergeGapMillis
        val candidate = store.findMergeCandidate(
            deviceId = snapshot.deviceId,
            locationKey = locationKey,
            earliestLastCapturedAtUtcMillis = earliestMergeTime
        )

        if (candidate == null) {
            store.insert(snapshot.toNewVisit(locationKey, roundedLatitude, roundedLongitude))
            return LocationVisitRecordResult.Created
        }

        store.update(candidate.merge(snapshot))
        return LocationVisitRecordResult.Merged
    }

    private fun LocationContextSnapshotEntity.toNewVisit(
        locationKey: String,
        roundedLatitude: Double,
        roundedLongitude: Double
    ): LocationVisitEntity {
        val now = clock()
        return LocationVisitEntity(
            id = idFactory(this),
            deviceId = deviceId,
            locationKey = locationKey,
            latitude = roundedLatitude,
            longitude = roundedLongitude,
            coordinatePrecisionDecimals = coordinatePrecisionDecimals,
            firstCapturedAtUtcMillis = capturedAtUtcMillis,
            lastCapturedAtUtcMillis = capturedAtUtcMillis,
            durationMs = 0L,
            sampleCount = 1,
            accuracyMeters = accuracyMeters,
            permissionState = permissionState,
            captureMode = captureMode,
            createdAtUtcMillis = now,
            updatedAtUtcMillis = now
        )
    }

    private fun LocationVisitEntity.merge(snapshot: LocationContextSnapshotEntity): LocationVisitEntity {
        val firstSeen = minOf(firstCapturedAtUtcMillis, snapshot.capturedAtUtcMillis)
        val lastSeen = maxOf(lastCapturedAtUtcMillis, snapshot.capturedAtUtcMillis)

        return copy(
            firstCapturedAtUtcMillis = firstSeen,
            lastCapturedAtUtcMillis = lastSeen,
            durationMs = (lastSeen - firstSeen).coerceAtLeast(0L),
            sampleCount = sampleCount + 1,
            accuracyMeters = bestAccuracy(accuracyMeters, snapshot.accuracyMeters),
            permissionState = snapshot.permissionState,
            captureMode = snapshot.captureMode,
            updatedAtUtcMillis = clock()
        )
    }

    private fun locationKey(latitude: Double, longitude: Double): String {
        val format = "%.${coordinatePrecisionDecimals}f"
        return "${String.format(Locale.US, format, latitude)}," +
            String.format(Locale.US, format, longitude)
    }

    private fun bestAccuracy(current: Float?, incoming: Float?): Float? {
        return when {
            current == null -> incoming
            incoming == null -> current
            else -> minOf(current, incoming)
        }
    }

    companion object {
        const val DefaultCoordinatePrecisionDecimals = 4
        const val DefaultMergeGapMillis = 30 * 60_000L
    }
}

enum class LocationVisitRecordResult {
    Created,
    Merged,
    SkippedNoCoordinates,
    SkippedNoRecorder
}
