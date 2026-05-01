package com.woong.monitorstack.layout

import org.junit.Assert.assertEquals
import org.junit.Test

class SystemInsetsLayoutCalculatorTest {
    @Test
    fun calculateAddsOnlySystemBottomInsetToBottomNavigationAndContentMargin() {
        val calculator = SystemInsetsLayoutCalculator(baseBottomNavigationHeightPx = 72)

        val layout = calculator.calculate(systemNavigationBottomInsetPx = 24)

        assertEquals(96, layout.bottomNavigationHeightPx)
        assertEquals(96, layout.fragmentBottomMarginPx)
        assertEquals(24, layout.bottomNavigationPaddingBottomPx)
    }

    @Test
    fun calculateTreatsNegativeInsetsAsZero() {
        val calculator = SystemInsetsLayoutCalculator(baseBottomNavigationHeightPx = 72)

        val layout = calculator.calculate(systemNavigationBottomInsetPx = -10)

        assertEquals(72, layout.bottomNavigationHeightPx)
        assertEquals(72, layout.fragmentBottomMarginPx)
        assertEquals(0, layout.bottomNavigationPaddingBottomPx)
    }
}
