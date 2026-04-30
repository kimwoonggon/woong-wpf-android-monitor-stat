package com.woong.monitorstack.dashboard

import org.junit.Assert.assertEquals
import org.junit.Test

class DashboardChartMapperTest {
    @Test
    fun mapConvertsDurationsToMinuteBasedChartEntries() {
        val chartData = DashboardChartData(
            hourlyActivity = listOf(DashboardActivityBucket(hourOfDay = 9, durationMs = 30 * 60_000L)),
            appUsage = listOf(DashboardUsageSlice(label = "Chrome", durationMs = 45 * 60_000L)),
            domainUsage = listOf(DashboardUsageSlice(label = "example.com", durationMs = 15 * 60_000L))
        )
        val mapper = DashboardChartMapper()

        val mapped = mapper.map(chartData)

        assertEquals(9f, mapped.activityEntries.single().x)
        assertEquals(30f, mapped.activityEntries.single().y)
        assertEquals(0f, mapped.appEntries.single().x)
        assertEquals(45f, mapped.appEntries.single().y)
        assertEquals("Chrome", mapped.appLabels.single())
        assertEquals("example.com", mapped.domainEntries.single().label)
        assertEquals(15f, mapped.domainEntries.single().value)
    }
}
