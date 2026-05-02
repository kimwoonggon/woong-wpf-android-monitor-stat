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
    private val pointLabelPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = ContextCompat.getColor(context, R.color.wms_text_secondary)
        textAlign = Paint.Align.CENTER
        textSize = 10f * resources.displayMetrics.scaledDensity
    }
    private val roadPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = ContextCompat.getColor(context, R.color.wms_border)
        style = Paint.Style.STROKE
        strokeWidth = 5f
        alpha = 170
    }
    private val blockPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = ContextCompat.getColor(context, R.color.wms_background)
        style = Paint.Style.FILL
        alpha = 230
    }

    val pointCount: Int
        get() = points.size

    init {
        minimumHeight = (132f * resources.displayMetrics.density).roundToInt()
        contentDescription = "No location statistics. Local map preview, no network map tiles."
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
        drawLocalMapContext(canvas)

        if (points.isEmpty()) {
            canvas.drawText(
                "No location statistics",
                bounds.centerX(),
                bounds.centerY() - ((textPaint.descent() + textPaint.ascent()) / 2f),
                textPaint
            )
            return
        }

        drawPoints(canvas)
    }

    private fun drawLocalMapContext(canvas: Canvas) {
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

        val blockWidth = (right - left) / 5f
        val blockHeight = (bottom - top) / 3f
        canvas.drawRoundRect(
            RectF(
                left + blockWidth * 0.3f,
                top + blockHeight * 0.25f,
                left + blockWidth * 1.5f,
                top + blockHeight
            ),
            10f,
            10f,
            blockPaint
        )
        canvas.drawRoundRect(
            RectF(
                right - blockWidth * 1.6f,
                bottom - blockHeight * 1.1f,
                right - blockWidth * 0.35f,
                bottom - blockHeight * 0.25f
            ),
            10f,
            10f,
            blockPaint
        )
        canvas.drawLine(left, bottom - blockHeight * 0.7f, right, top + blockHeight * 0.65f, roadPaint)
        canvas.drawLine(left + blockWidth * 2.2f, top, left + blockWidth * 3.0f, bottom, roadPaint)
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
            canvas.drawText(
                point.capturedAtLocalText,
                x,
                (y - radius - 6f).coerceAtLeast(bounds.top + 18f),
                pointLabelPaint
            )
        }
    }

    private fun buildContentDescription(): String {
        if (points.isEmpty()) {
            return "No location statistics. Local map preview, no network map tiles."
        }

        val topPoint = points.maxWith(
            compareBy<LocationMapPoint> { it.durationMs }
                .thenBy { it.sampleCount }
        )
        val pointLabels = points
            .sortedBy { it.capturedAtLocalText }
            .joinToString(separator = ". ") { point ->
                "Point ${point.capturedAtLocalText}: %.4f, %.4f, %s, %d samples".format(
                    point.latitude,
                    point.longitude,
                    formatDuration(point.durationMs),
                    point.sampleCount
                )
            }

        return (
            "${points.size} location visits. Local map preview, no network map tiles. " +
                "Top location %.4f, %.4f - %s. %s"
            ).format(
            topPoint.latitude,
            topPoint.longitude,
            formatDuration(topPoint.durationMs),
            pointLabels
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
