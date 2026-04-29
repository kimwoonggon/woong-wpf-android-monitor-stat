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
                ),
                locationContext = DashboardLocationContext(
                    statusText = "Location context enabled",
                    latitudeText = "37.5665",
                    longitudeText = "126.9780",
                    accuracyText = "±36m",
                    capturedAtLocalText = "09:30"
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
        assertEquals("Location context enabled", viewModel.state.locationContext.statusText)
        assertEquals("37.5665", viewModel.state.locationContext.latitudeText)
        assertEquals("126.9780", viewModel.state.locationContext.longitudeText)
        assertEquals("±36m", viewModel.state.locationContext.accuracyText)
        assertEquals("09:30", viewModel.state.locationContext.capturedAtLocalText)
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
