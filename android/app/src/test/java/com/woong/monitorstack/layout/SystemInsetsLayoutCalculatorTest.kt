package com.woong.monitorstack.layout

import org.junit.Assert.assertEquals
import org.junit.Test

class SystemInsetsLayoutCalculatorTest {
    @Test
    fun calculateKeepsBottomNavigationCompactAndLeavesSystemButtonsOutsideApp() {
        val calculator = SystemInsetsLayoutCalculator(
            baseBottomNavigationHeightPx = 56,
            contentBottomClearancePx = 0
        )

        val layout = calculator.calculate(systemNavigationBottomInsetPx = 24)

        assertEquals(56, layout.bottomNavigationHeightPx)
        assertEquals(80, layout.fragmentBottomMarginPx)
        assertEquals(0, layout.bottomNavigationPaddingBottomPx)
        assertEquals(24, layout.bottomNavigationBottomMarginPx)
    }

    @Test
    fun calculateTreatsNegativeInsetsAsZero() {
        val calculator = SystemInsetsLayoutCalculator(
            baseBottomNavigationHeightPx = 56,
            contentBottomClearancePx = 0
        )

        val layout = calculator.calculate(systemNavigationBottomInsetPx = -10)

        assertEquals(56, layout.bottomNavigationHeightPx)
        assertEquals(56, layout.fragmentBottomMarginPx)
        assertEquals(0, layout.bottomNavigationPaddingBottomPx)
        assertEquals(0, layout.bottomNavigationBottomMarginPx)
    }

    @Test
    fun calculateKeepsContentMarginAlignedToVisibleBottomNavigation() {
        val calculator = SystemInsetsLayoutCalculator(
            baseBottomNavigationHeightPx = 56,
            contentBottomClearancePx = 0
        )

        val layout = calculator.calculate(systemNavigationBottomInsetPx = 24)

        assertEquals(
            "The main shell content should reserve the visible bottom navigation plus the Android system navigation area below it.",
            layout.bottomNavigationHeightPx + layout.bottomNavigationBottomMarginPx,
            layout.fragmentBottomMarginPx
        )
    }
}
