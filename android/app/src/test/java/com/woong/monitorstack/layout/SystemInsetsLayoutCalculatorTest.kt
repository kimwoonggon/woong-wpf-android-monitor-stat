package com.woong.monitorstack.layout

import org.junit.Assert.assertEquals
import org.junit.Test

class SystemInsetsLayoutCalculatorTest {
    @Test
    fun calculateKeepsBottomNavigationCompactWhenRootAlreadyFitsSystemWindows() {
        val calculator = SystemInsetsLayoutCalculator(
            baseBottomNavigationHeightPx = 56,
            contentBottomClearancePx = 0
        )

        val layout = calculator.calculate(systemNavigationBottomInsetPx = 24)

        assertEquals(56, layout.bottomNavigationHeightPx)
        assertEquals(56, layout.fragmentBottomMarginPx)
        assertEquals(0, layout.bottomNavigationPaddingBottomPx)
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
            "The main shell already fits system windows, so the content margin should not add a second navigation-bar-sized blank area.",
            layout.bottomNavigationHeightPx,
            layout.fragmentBottomMarginPx
        )
    }
}
