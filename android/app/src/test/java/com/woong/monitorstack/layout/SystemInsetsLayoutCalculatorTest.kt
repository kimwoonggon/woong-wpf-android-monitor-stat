package com.woong.monitorstack.layout

import org.junit.Assert.assertEquals
import org.junit.Test

class SystemInsetsLayoutCalculatorTest {
    @Test
    fun calculateKeepsBottomNavigationContentAboveSystemNavigationBar() {
        val calculator = SystemInsetsLayoutCalculator(
            baseBottomNavigationHeightPx = 56,
            contentBottomClearancePx = 0
        )

        val layout = calculator.calculate(systemNavigationBottomInsetPx = 24)

        assertEquals(80, layout.bottomNavigationHeightPx)
        assertEquals(80, layout.fragmentBottomMarginPx)
        assertEquals(24, layout.bottomNavigationPaddingBottomPx)
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
    }

    @Test
    fun calculateKeepsContentMarginAlignedToCompactBottomNavigation() {
        val calculator = SystemInsetsLayoutCalculator(
            baseBottomNavigationHeightPx = 56,
            contentBottomClearancePx = 0
        )

        val layout = calculator.calculate(systemNavigationBottomInsetPx = 24)

        assertEquals(
            "The main shell content should reserve exactly the visible bottom navigation height, including the one required system-navigation safe inset.",
            layout.bottomNavigationHeightPx,
            layout.fragmentBottomMarginPx
        )
    }
}
