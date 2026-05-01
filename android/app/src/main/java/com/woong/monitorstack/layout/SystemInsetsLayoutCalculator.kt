package com.woong.monitorstack.layout

data class SystemInsetsLayout(
    val bottomNavigationHeightPx: Int,
    val fragmentBottomMarginPx: Int,
    val bottomNavigationPaddingBottomPx: Int
)

class SystemInsetsLayoutCalculator(
    private val baseBottomNavigationHeightPx: Int
) {
    init {
        require(baseBottomNavigationHeightPx > 0) {
            "baseBottomNavigationHeightPx must be positive."
        }
    }

    fun calculate(systemNavigationBottomInsetPx: Int): SystemInsetsLayout {
        val safeBottomInsetPx = systemNavigationBottomInsetPx.coerceAtLeast(0)
        val adjustedHeightPx = baseBottomNavigationHeightPx + safeBottomInsetPx

        return SystemInsetsLayout(
            bottomNavigationHeightPx = adjustedHeightPx,
            fragmentBottomMarginPx = adjustedHeightPx,
            bottomNavigationPaddingBottomPx = safeBottomInsetPx
        )
    }
}
