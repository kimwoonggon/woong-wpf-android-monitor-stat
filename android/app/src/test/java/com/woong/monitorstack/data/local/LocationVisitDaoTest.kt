package com.woong.monitorstack.data.local

import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class LocationVisitDaoTest {
    private lateinit var database: MonitorDatabase

    @Before
    fun setUp() {
        database = Room.inMemoryDatabaseBuilder(
            ApplicationProvider.getApplicationContext(),
            MonitorDatabase::class.java
        )
            .allowMainThreadQueries()
            .build()
    }

    @After
    fun tearDown() {
        database.close()
    }

    @Test
    fun insertUpdateAndQueryByRangePreservesVisitStatistics() {
        val dao = database.locationVisitDao()
        dao.insert(
            visit(
                id = "visit-seoul",
                locationKey = "37.5665,126.9780",
                firstCapturedAtUtcMillis = 1_000L,
                lastCapturedAtUtcMillis = 10_000L,
                durationMs = 9_000L,
                sampleCount = 2
            )
        )
        dao.insert(
            visit(
                id = "visit-busan",
                locationKey = "35.1796,129.0756",
                firstCapturedAtUtcMillis = 20_000L,
                lastCapturedAtUtcMillis = 30_000L,
                durationMs = 10_000L,
                sampleCount = 3
            )
        )

        val seoul = requireNotNull(
            dao.findMergeCandidate(
                deviceId = "android-device-1",
                locationKey = "37.5665,126.9780",
                earliestLastCapturedAtUtcMillis = 0L
            )
        )
        dao.update(
            seoul.copy(
                lastCapturedAtUtcMillis = 40_000L,
                durationMs = 39_000L,
                sampleCount = 3
            )
        )

        val visits = dao.queryByRange(
            deviceId = "android-device-1",
            fromUtcMillis = 5_000L,
            toUtcMillis = 45_000L
        )

        assertEquals(listOf("visit-seoul", "visit-busan"), visits.map { it.id })
        assertEquals(39_000L, visits.first().durationMs)
        assertEquals(3, visits.first().sampleCount)
    }

    private fun visit(
        id: String,
        locationKey: String,
        firstCapturedAtUtcMillis: Long,
        lastCapturedAtUtcMillis: Long,
        durationMs: Long,
        sampleCount: Int
    ): LocationVisitEntity {
        val latitude = locationKey.substringBefore(",").toDouble()
        val longitude = locationKey.substringAfter(",").toDouble()

        return LocationVisitEntity(
            id = id,
            deviceId = "android-device-1",
            locationKey = locationKey,
            latitude = latitude,
            longitude = longitude,
            coordinatePrecisionDecimals = 4,
            firstCapturedAtUtcMillis = firstCapturedAtUtcMillis,
            lastCapturedAtUtcMillis = lastCapturedAtUtcMillis,
            durationMs = durationMs,
            sampleCount = sampleCount,
            accuracyMeters = 16.0f,
            permissionState = LocationPermissionState.GrantedPrecise,
            captureMode = LocationCaptureMode.AppUsageContext,
            createdAtUtcMillis = firstCapturedAtUtcMillis,
            updatedAtUtcMillis = lastCapturedAtUtcMillis
        )
    }
}
