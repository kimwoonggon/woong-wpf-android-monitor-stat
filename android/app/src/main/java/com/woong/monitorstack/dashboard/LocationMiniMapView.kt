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
    private val labelBounds = RectF()
    private val backgroundPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.rgb(241, 248, 244)
        style = Paint.Style.FILL
    }
    private val borderPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = ContextCompat.getColor(context, R.color.wms_border)
        style = Paint.Style.STROKE
        strokeWidth = 1.5f
    }
    private val gridPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.rgb(150, 166, 184)
        style = Paint.Style.STROKE
        strokeWidth = 2f
        alpha = 235
    }
    private val pointPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = ContextCompat.getColor(context, R.color.wms_primary)
        style = Paint.Style.FILL
        alpha = 210
    }
    private val pointOutlinePaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.WHITE
        style = Paint.Style.STROKE
        strokeWidth = 3f
    }
    private val textPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = ContextCompat.getColor(context, R.color.wms_text_muted)
        textAlign = Paint.Align.CENTER
        textSize = 14f * resources.displayMetrics.scaledDensity
    }
    private val pointLabelPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = ContextCompat.getColor(context, R.color.wms_text_primary)
        textAlign = Paint.Align.CENTER
        textSize = 12f * resources.displayMetrics.scaledDensity
        isFakeBoldText = true
    }
    private val pointLabelBackgroundPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.WHITE
        style = Paint.Style.FILL
        alpha = 235
    }
    private val roadPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.rgb(104, 121, 145)
        style = Paint.Style.STROKE
        strokeWidth = 16f
        strokeCap = Paint.Cap.ROUND
    }
    private val roadCenterPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.WHITE
        style = Paint.Style.STROKE
        strokeWidth = 5f
        strokeCap = Paint.Cap.ROUND
        alpha = 235
    }
    private val blockPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.rgb(199, 235, 211)
        style = Paint.Style.FILL
    }
    private val waterBlockPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.rgb(204, 226, 255)
        style = Paint.Style.FILL
    }

    val pointCount: Int
        get() = points.size

    init {
        minimumHeight = (132f * resources.displayMetrics.density).roundToInt()
        contentDescription = EmptyContentDescription
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
        canvas.drawRoundRect(
            RectF(
                left + blockWidth * 2.1f,
                top + blockHeight * 0.35f,
                left + blockWidth * 3.15f,
                top + blockHeight * 1.2f
            ),
            10f,
            10f,
            waterBlockPaint
        )
        canvas.drawRoundRect(
            RectF(
                left + blockWidth * 0.7f,
                bottom - blockHeight * 0.95f,
                left + blockWidth * 2.0f,
                bottom - blockHeight * 0.2f
            ),
            10f,
            10f,
            waterBlockPaint
        )
        repeat(5) { index ->
            val fraction = (index + 1) / 6f
            val x = left + ((right - left) * fraction)
            val y = top + ((bottom - top) * fraction)
            canvas.drawLine(x, top, x, bottom, gridPaint)
            canvas.drawLine(left, y, right, y, gridPaint)
        }
        drawRoad(canvas, left, bottom - blockHeight * 0.7f, right, top + blockHeight * 0.65f)
        drawRoad(canvas, left + blockWidth * 2.2f, top, left + blockWidth * 3.0f, bottom)
        drawRoad(canvas, left, top + blockHeight * 1.45f, right, top + blockHeight * 1.2f)
    }

    private fun drawRoad(canvas: Canvas, startX: Float, startY: Float, endX: Float, endY: Float) {
        canvas.drawLine(startX, startY, endX, endY, roadPaint)
        canvas.drawLine(startX, startY, endX, endY, roadCenterPaint)
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
            canvas.drawCircle(x, y, radius + 2f, pointOutlinePaint)
            canvas.drawCircle(x, y, radius, pointPaint)
            drawPointLabel(canvas, normalizeTimeLabel(point.capturedAtLocalText), x, y, radius)
        }
    }

    private fun drawPointLabel(canvas: Canvas, label: String, x: Float, y: Float, radius: Float) {
        val labelY = (y - radius - 8f).coerceAtLeast(bounds.top + 20f)
        val labelWidth = pointLabelPaint.measureText(label)
        val labelCenterX = x.coerceIn(
            bounds.left + (labelWidth / 2f) + 12f,
            bounds.right - (labelWidth / 2f) - 12f
        )
        labelBounds.set(
            labelCenterX - (labelWidth / 2f) - 8f,
            labelY + pointLabelPaint.ascent() - 4f,
            labelCenterX + (labelWidth / 2f) + 8f,
            labelY + pointLabelPaint.descent() + 4f
        )
        canvas.drawRoundRect(labelBounds, 8f, 8f, pointLabelBackgroundPaint)
        canvas.drawText(label, labelCenterX, labelY, pointLabelPaint)
    }

    private fun buildContentDescription(): String {
        if (points.isEmpty()) {
            return EmptyContentDescription
        }

        val topPoint = points.maxWith(
            compareBy<LocationMapPoint> { it.durationMs }
                .thenBy { it.sampleCount }
        )
        val pointLabels = points
            .sortedBy { it.capturedAtLocalText }
            .joinToString(separator = ". ") { point ->
                "Point ${normalizeTimeLabel(point.capturedAtLocalText)}: %.4f, %.4f, %s, %d samples".format(
                    point.latitude,
                    point.longitude,
                    formatDuration(point.durationMs),
                    point.sampleCount
                )
            }

        return (
            "${points.size} location visits. Local map preview with roads, blocks, and grid; no network map tiles. " +
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

    private fun normalizeTimeLabel(label: String): String {
        val trimmed = label.trim()
        val dottedHourMinute = Regex("""^(\d{1,2})\.(\d{2})$""").matchEntire(trimmed)
        if (dottedHourMinute != null) {
            val hour = dottedHourMinute.groupValues[1].padStart(2, '0')
            return "$hour:${dottedHourMinute.groupValues[2]}"
        }
        return trimmed
    }

    private companion object {
        const val EmptyContentDescription =
            "No location statistics. Local map preview with roads, blocks, and grid; no network map tiles."
    }
}
