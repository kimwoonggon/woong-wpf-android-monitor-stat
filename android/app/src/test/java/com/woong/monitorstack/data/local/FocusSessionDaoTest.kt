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
class FocusSessionDaoTest {
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
    fun insertAndQueryByLocalDateRangeReturnsMatchingSessions() {
        val dao = database.focusSessionDao()
        dao.insert(
            FocusSessionEntity(
                clientSessionId = "session-1",
                packageName = "com.android.chrome",
                startedAtUtcMillis = 1_000,
                endedAtUtcMillis = 61_000,
                durationMs = 60_000,
                localDate = "2026-04-28",
                timezoneId = "Asia/Seoul",
                isIdle = false,
                source = "usage_stats"
            )
        )
        dao.insert(
            FocusSessionEntity(
                clientSessionId = "session-2",
                packageName = "com.slack",
                startedAtUtcMillis = 100_000,
                endedAtUtcMillis = 160_000,
                durationMs = 60_000,
                localDate = "2026-04-29",
                timezoneId = "Asia/Seoul",
                isIdle = false,
                source = "usage_stats"
            )
        )

        val sessions = dao.queryByLocalDateRange("2026-04-28", "2026-04-28")

        val session = sessions.single()
        assertEquals("session-1", session.clientSessionId)
        assertEquals("com.android.chrome", session.packageName)
    }

    @Test
    fun queryRecentReturnsNewestSessionsFirst() {
        val dao = database.focusSessionDao()
        dao.insert(focusSession("old-session", "com.android.chrome", startedAtUtcMillis = 1_000))
        dao.insert(focusSession("new-session", "com.slack", startedAtUtcMillis = 5_000))

        val sessions = dao.queryRecent(limit = 10)

        assertEquals(listOf("new-session", "old-session"), sessions.map { it.clientSessionId })
    }

    private fun focusSession(
        clientSessionId: String,
        packageName: String,
        startedAtUtcMillis: Long
    ): FocusSessionEntity {
        return FocusSessionEntity(
            clientSessionId = clientSessionId,
            packageName = packageName,
            startedAtUtcMillis = startedAtUtcMillis,
            endedAtUtcMillis = startedAtUtcMillis + 60_000,
            durationMs = 60_000,
            localDate = "2026-04-29",
            timezoneId = "Asia/Seoul",
            isIdle = false,
            source = "android_usage_stats"
        )
    }
}
