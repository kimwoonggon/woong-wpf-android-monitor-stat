package com.woong.monitorstack.ui

import android.content.res.ColorStateList
import androidx.core.content.ContextCompat
import com.google.android.material.button.MaterialButton
import com.woong.monitorstack.R

object PeriodButtonStyler {
    fun select(selectedButton: MaterialButton, buttons: List<MaterialButton>) {
        buttons.forEach { button ->
            val isSelected = button == selectedButton
            button.isSelected = isSelected
            applyStyle(button, isSelected)
        }
    }

    private fun applyStyle(button: MaterialButton, isSelected: Boolean) {
        val context = button.context
        val backgroundColor = ContextCompat.getColor(
            context,
            if (isSelected) R.color.wms_primary else R.color.wms_surface
        )
        val textColor = ContextCompat.getColor(
            context,
            if (isSelected) android.R.color.white else R.color.wms_primary
        )
        val strokeColor = ContextCompat.getColor(
            context,
            if (isSelected) R.color.wms_primary else R.color.wms_border
        )

        button.backgroundTintList = ColorStateList.valueOf(backgroundColor)
        button.setTextColor(textColor)
        button.strokeColor = ColorStateList.valueOf(strokeColor)
    }
}
