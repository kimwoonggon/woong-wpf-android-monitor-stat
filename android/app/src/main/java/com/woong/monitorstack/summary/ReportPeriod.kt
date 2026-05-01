package com.woong.monitorstack.summary

enum class ReportPeriod(
    val dayCount: Int
) {
    Last7Days(7),
    Last30Days(30),
    Last90Days(90)
}
