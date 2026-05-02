package com.woong.monitorstack.layout

import org.junit.Assert.assertEquals
import org.junit.Test

class SystemInsetsLayoutCalculatorTest {
    @Test
    fun calculateAddsOnlySystemBottomInsetToBottomNavigationAndContentMargin() {
        val calculator = SystemInsetsLayoutCalculator(
            baseBottomNavigationHeightPx = 72,
            contentBottomClearancePx = 16
        )

        val layout = calculator.calculate(systemNavigationBottomInsetPx = 24)

        assertEquals(96, layout.bottomNavigationHeightPx)
        assertEquals(112, layout.fragmentBottomMarginPx)
        assertEquals(24, layout.bottomNavigationPaddingBottomPx)
    }

    @Test
    fun calculateTreatsNegativeInsetsAsZero() {
        val calculator = SystemInsetsLayoutCalculator(
            baseBottomNavigationHeightPx = 72,
            contentBottomClearancePx = 16
        )

        val layout = calculator.calculate(systemNavigationBottomInsetPx = -10)

        assertEquals(72, layout.bottomNavigationHeightPx)
        assertEquals(88, layout.fragmentBottomMarginPx)
        assertEquals(0, layout.bottomNavigationPaddingBottomPx)
    }

    @Test
    fun calculateKeepsPhoneViewportContentAboveBottomNavigationWithClearance() {
        val calculator = SystemInsetsLayoutCalculator(
            baseBottomNavigationHeightPx = 72,
            contentBottomClearancePx = 16
        )

        val layout = calculator.calculate(systemNavigationBottomInsetPx = 24)

        assertEquals(
            "Content container must keep scrollable tab content clear of the bottom navigation plus a visible buffer.",
            layout.bottomNavigationHeightPx + 16,
            layout.fragmentBottomMarginPx
        )
    }
}
