package com.woong.monitorstack.dashboard

import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.RectF
import android.util.AttributeSet
import android.view.View
import androidx.core.content.ContextCompat
import com.woong.monitorstack.R
import kotlin.math.max
import kotlin.math.roundToInt

class LocationMiniMapView @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null,
    defStyleAttr: Int = 0
) : View(context, attrs, defStyleAttr) {
    private val points = mutableListOf<LocationMapPoint>()
    private val bounds = RectF()
    private val backgroundPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.WHITE
        style = Paint.Style.FILL
    }
    private val borderPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = ContextCompat.getColor(context, R.color.wms_border)
        style = Paint.Style.STROKE
        strokeWidth = 1.5f
    }
    private val gridPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = ContextCompat.getColor(context, R.color.wms_border)
        style = Paint.Style.STROKE
        strokeWidth = 1f
        alpha = 140
    }
    private val pointPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = ContextCompat.getColor(context, R.color.wms_primary)
        style = Paint.Style.FILL
        alpha = 210
    }
    private val textPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = ContextCompat.getColor(context, R.color.wms_text_muted)
        textAlign = Paint.Align.CENTER
        textSize = 14f * resources.displayMetrics.scaledDensity
    }

    val pointCount: Int
        get() = points.size

    init {
        minimumHeight = (132f * resources.displayMetrics.density).roundToInt()
        contentDescription = "No location statistics"
    }

    fun setPoints(newPoints: List<LocationMapPoint>) {
        points.clear()
        points.addAll(newPoints)
        contentDescription = buildContentDescription()
        invalidate()
    }

    override fun onDraw(canvas: Canvas) {
        super.onDraw(canvas)
        bounds.set(
            paddingLeft.toFloat(),
            paddingTop.toFloat(),
            (width - paddingRight).toFloat(),
            (height - paddingBottom).toFloat()
        )
        canvas.drawRoundRect(bounds, 16f, 16f, backgroundPaint)
        canvas.drawRoundRect(bounds, 16f, 16f, borderPaint)

        if (points.isEmpty()) {
            canvas.drawText(
                "No location statistics",
                bounds.centerX(),
                bounds.centerY() - ((textPaint.descent() + textPaint.ascent()) / 2f),
                textPaint
            )
            return
        }

        drawGrid(canvas)
        drawPoints(canvas)
    }

    private fun drawGrid(canvas: Canvas) {
        val left = bounds.left + 18f
        val top = bounds.top + 18f
        val right = bounds.right - 18f
        val bottom = bounds.bottom - 18f

        repeat(3) { index ->
            val fraction = (index + 1) / 4f
            val x = left + ((right - left) * fraction)
            val y = top + ((bottom - top) * fraction)
            canvas.drawLine(x, top, x, bottom, gridPaint)
            canvas.drawLine(left, y, right, y, gridPaint)
        }
    }

    private fun drawPoints(canvas: Canvas) {
        val minLatitude = points.minOf { it.latitude }
        val maxLatitude = points.maxOf { it.latitude }
        val minLongitude = points.minOf { it.longitude }
        val maxLongitude = points.maxOf { it.longitude }
        val latitudeRange = max(0.0001, maxLatitude - minLatitude)
        val longitudeRange = max(0.0001, maxLongitude - minLongitude)
        val maxDuration = points.maxOf { it.durationMs }.coerceAtLeast(1L).toDouble()
        val left = bounds.left + 26f
        val top = bounds.top + 26f
        val width = (bounds.width() - 52f).coerceAtLeast(1f)
        val height = (bounds.height() - 52f).coerceAtLeast(1f)

        points.forEach { point ->
            val x = left + (((point.longitude - minLongitude) / longitudeRange).toFloat() * width)
            val y = top + height - (((point.latitude - minLatitude) / latitudeRange).toFloat() * height)
            val radius = 7f + (18f * (point.durationMs / maxDuration).toFloat())
            canvas.drawCircle(x, y, radius, pointPaint)
        }
    }

    private fun buildContentDescription(): String {
        if (points.isEmpty()) {
            return "No location statistics"
        }

        val topPoint = points.maxWith(
            compareBy<LocationMapPoint> { it.durationMs }
                .thenBy { it.sampleCount }
        )
        return "${points.size} location visits. Top location %.4f, %.4f - %s".format(
            topPoint.latitude,
            topPoint.longitude,
            formatDuration(topPoint.durationMs)
        )
    }

    private fun formatDuration(durationMs: Long): String {
        val totalMinutes = durationMs / 60_000
        val hours = totalMinutes / 60
        val minutes = totalMinutes % 60
        return if (hours > 0) {
            "${hours}h ${minutes}m"
        } else {
            "${minutes}m"
        }
    }
}
