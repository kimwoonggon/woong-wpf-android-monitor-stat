package com.woong.monitorstack.data.local

import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class LocationContextSnapshotDaoTest {
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
    fun insertAndQueryRecentPreservesNullableCoordinates() {
        val dao = database.locationContextSnapshotDao()
        dao.insert(
            LocationContextSnapshotEntity(
                id = "location-1",
                deviceId = "android-device-1",
                capturedAtUtcMillis = 1_777_000_100_000,
                latitude = 37.5665,
                longitude = 126.9780,
                accuracyMeters = 35.5f,
                permissionState = LocationPermissionState.GrantedApproximate,
                captureMode = LocationCaptureMode.AppUsageContext,
                createdAtUtcMillis = 1_777_000_100_500
            )
        )
        dao.insert(
            LocationContextSnapshotEntity(
                id = "location-2",
                deviceId = "android-device-1",
                capturedAtUtcMillis = 1_777_000_200_000,
                latitude = null,
                longitude = null,
                accuracyMeters = null,
                permissionState = LocationPermissionState.NotGranted,
                captureMode = LocationCaptureMode.DisabledOrUnavailable,
                createdAtUtcMillis = 1_777_000_200_500
            )
        )

        val snapshots = dao.queryRecent(deviceId = "android-device-1", limit = 10)

        assertEquals(listOf("location-2", "location-1"), snapshots.map { it.id })
        assertNull(snapshots[0].latitude)
        assertNull(snapshots[0].longitude)
        assertNull(snapshots[0].accuracyMeters)
        assertEquals(LocationPermissionState.NotGranted, snapshots[0].permissionState)
        assertEquals(LocationCaptureMode.DisabledOrUnavailable, snapshots[0].captureMode)
        assertEquals(37.5665, snapshots[1].latitude ?: 0.0, 0.0001)
        assertEquals(126.9780, snapshots[1].longitude ?: 0.0, 0.0001)
        assertEquals(35.5f, snapshots[1].accuracyMeters ?: 0f, 0.0001f)
        assertEquals(1_777_000_100_000, snapshots[1].capturedAtUtcMillis)
    }

    @Test
    fun queryByCapturedRangeReturnsOnlyDeviceSnapshotsInRange() {
        val dao = database.locationContextSnapshotDao()
        dao.insert(locationSnapshot("before-range", "android-device-1", capturedAtUtcMillis = 900))
        dao.insert(locationSnapshot("matching-1", "android-device-1", capturedAtUtcMillis = 1_000))
        dao.insert(locationSnapshot("matching-2", "android-device-1", capturedAtUtcMillis = 2_000))
        dao.insert(locationSnapshot("other-device", "android-device-2", capturedAtUtcMillis = 1_500))
        dao.insert(locationSnapshot("after-range", "android-device-1", capturedAtUtcMillis = 2_100))

        val snapshots = dao.queryByCapturedRange(
            deviceId = "android-device-1",
            fromUtcMillis = 1_000,
            toUtcMillis = 2_000
        )

        assertEquals(listOf("matching-1", "matching-2"), snapshots.map { it.id })
    }

    private fun locationSnapshot(
        id: String,
        deviceId: String,
        capturedAtUtcMillis: Long
    ): LocationContextSnapshotEntity {
        return LocationContextSnapshotEntity(
            id = id,
            deviceId = deviceId,
            capturedAtUtcMillis = capturedAtUtcMillis,
            latitude = null,
            longitude = null,
            accuracyMeters = null,
            permissionState = LocationPermissionState.NotGranted,
            captureMode = LocationCaptureMode.DisabledOrUnavailable,
            createdAtUtcMillis = capturedAtUtcMillis
        )
    }
}
