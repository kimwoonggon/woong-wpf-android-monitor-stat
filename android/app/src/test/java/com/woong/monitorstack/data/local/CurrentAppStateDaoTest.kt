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
class CurrentAppStateDaoTest {
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
    fun insertDuplicateClientStateIdDoesNotCreateSecondRow() {
        val dao = database.currentAppStateDao()
        dao.insert(currentState("current-state-1", "com.android.chrome", observedAtUtcMillis = 1_000L))
        dao.insert(currentState("current-state-1", "com.slack", observedAtUtcMillis = 2_000L))

        val states = dao.queryAfterCheckpoint(
            observedAtUtcMillis = 0L,
            clientStateId = "",
            limit = 10
        )

        assertEquals(1, states.size)
        assertEquals("com.android.chrome", states.single().packageName)
        assertEquals("Chrome", states.single().appLabel)
        assertEquals(1_000L, states.single().observedAtUtcMillis)
    }

    @Test
    fun queryAfterCheckpointReturnsOnlyNewSnapshots() {
        val dao = database.currentAppStateDao()
        dao.insert(currentState("state-a", "com.android.chrome", observedAtUtcMillis = 1_000L))
        dao.insert(currentState("state-b", "com.slack", observedAtUtcMillis = 1_000L))
        dao.insert(currentState("state-c", "com.google.android.youtube", observedAtUtcMillis = 2_000L))

        val states = dao.queryAfterCheckpoint(
            observedAtUtcMillis = 1_000L,
            clientStateId = "state-a",
            limit = 10
        )

        assertEquals(listOf("state-b", "state-c"), states.map { it.clientStateId })
        assertEquals(listOf(1_000L, 2_000L), states.map { it.observedAtUtcMillis })
    }

    private fun currentState(
        clientStateId: String,
        packageName: String,
        observedAtUtcMillis: Long
    ): CurrentAppStateEntity {
        return CurrentAppStateEntity(
            clientStateId = clientStateId,
            packageName = packageName,
            appLabel = when (packageName) {
                "com.android.chrome" -> "Chrome"
                "com.slack" -> "Slack"
                "com.google.android.youtube" -> "YouTube"
                else -> packageName
            },
            status = CurrentAppStateStatus.Active,
            observedAtUtcMillis = observedAtUtcMillis,
            localDate = "1970-01-01",
            timezoneId = "UTC",
            source = "android_usage_stats_current_app",
            createdAtUtcMillis = observedAtUtcMillis
        )
    }
}
