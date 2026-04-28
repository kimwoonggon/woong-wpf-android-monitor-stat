package com.woong.monitorstack.dashboard

import org.junit.Assert.assertEquals
import org.junit.Test

class DashboardViewModelTest {
    @Test
    fun selectPeriodUpdatesSummaryAndRecentSessions() {
        val repository = FakeDashboardRepository(
            snapshot = DashboardSnapshot(
                totalActiveMs = 3_600_000,
                topAppPackageName = "com.android.chrome",
                idleMs = 300_000,
                recentSessions = listOf(
                    DashboardSessionRow(
                        packageName = "com.android.chrome",
                        startedAtLocalText = "09:00",
                        durationText = "1h 0m"
                    )
                )
            )
        )
        val viewModel = DashboardViewModel(repository)

        viewModel.selectPeriod(DashboardPeriod.Today)

        assertEquals(DashboardPeriod.Today, repository.requestedPeriod)
        assertEquals(DashboardPeriod.Today, viewModel.state.selectedPeriod)
        assertEquals(3_600_000, viewModel.state.totalActiveMs)
        assertEquals("com.android.chrome", viewModel.state.topAppPackageName)
        assertEquals(300_000, viewModel.state.idleMs)
        assertEquals("1h 0m", viewModel.state.recentSessions.single().durationText)
    }

    private class FakeDashboardRepository(
        private val snapshot: DashboardSnapshot
    ) : DashboardRepository {
        var requestedPeriod: DashboardPeriod? = null
            private set

        override fun load(period: DashboardPeriod): DashboardSnapshot {
            requestedPeriod = period

            return snapshot
        }
    }
}
