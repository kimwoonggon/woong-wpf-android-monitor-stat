package com.woong.monitorstack.dashboard

interface DashboardRepository {
    fun load(period: DashboardPeriod): DashboardSnapshot
}
