package com.woong.monitorstack.layout

data class SystemInsetsLayout(
    val bottomNavigationHeightPx: Int,
    val fragmentBottomMarginPx: Int,
    val bottomNavigationPaddingBottomPx: Int
)

class SystemInsetsLayoutCalculator(
    private val baseBottomNavigationHeightPx: Int,
    private val contentBottomClearancePx: Int = 0
) {
    init {
        require(baseBottomNavigationHeightPx > 0) {
            "baseBottomNavigationHeightPx must be positive."
        }
        require(contentBottomClearancePx >= 0) {
            "contentBottomClearancePx must not be negative."
        }
    }

    fun calculate(systemNavigationBottomInsetPx: Int): SystemInsetsLayout {
        return SystemInsetsLayout(
            bottomNavigationHeightPx = baseBottomNavigationHeightPx,
            fragmentBottomMarginPx = baseBottomNavigationHeightPx + contentBottomClearancePx,
            bottomNavigationPaddingBottomPx = 0
        )
    }
}
