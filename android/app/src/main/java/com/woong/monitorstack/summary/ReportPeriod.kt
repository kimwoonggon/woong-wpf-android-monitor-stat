package com.woong.monitorstack.summary

import java.time.LocalDate
import java.time.temporal.ChronoUnit

sealed class ReportPeriod(
    val dayCount: Int
) {
    data object Last7Days : ReportPeriod(7)
    data object Last30Days : ReportPeriod(30)
    data object Last90Days : ReportPeriod(90)

    data class Custom(
        val from: LocalDate,
        val to: LocalDate
    ) : ReportPeriod(dayCountBetween(from, to))
}

private fun dayCountBetween(from: LocalDate, to: LocalDate): Int {
    require(!to.isBefore(from)) { "Custom report end date must be on or after start date." }

    return ChronoUnit.DAYS.between(from, to).toInt() + 1
}
