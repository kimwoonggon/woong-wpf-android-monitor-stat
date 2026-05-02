package com.woong.monitorstack.dashboard

import android.content.Context
import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Color
import androidx.test.core.app.ApplicationProvider
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class LocationMiniMapViewTest {
    @Test
    fun setPointsWhenEmptyShowsNoLocationStatisticsState() {
        val view = LocationMiniMapView(context())

        view.setPoints(emptyList())

        assertEquals(0, view.pointCount)
        assertTrue(view.contentDescription.toString().contains("No location statistics"))
    }

    @Test
    fun emptyMapStillDrawsLocalPreviewContext() {
        val view = LocationMiniMapView(context()).apply {
            layout(0, 0, 320, 144)
        }
        val bitmap = Bitmap.createBitmap(320, 144, Bitmap.Config.ARGB_8888)

        view.setPoints(emptyList())
        view.draw(Canvas(bitmap))

        assertTrue(
            "Empty location state should still show the local map preview grid/background, not a blank panel.",
            bitmap.getPixel(84, 30) != Color.WHITE
        )
        assertTrue(
            "Accessibility evidence should name the visible no-network roads, blocks, and grid map context.",
            view.contentDescription.toString().contains("roads, blocks, and grid")
        )
        assertTrue(
            "Local map preview should be visibly map-like in screenshots, with substantial non-white roads/blocks/grid.",
            countNonWhitePixels(bitmap) > 12_000
        )
        assertTrue(
            "A central road should be prominent enough to read as map context behind points.",
            contrastFromWhite(bitmap.getPixel(80, 86)) > 50
        )
    }

    @Test
    fun setPointsSummarizesPersistedLocationVisitsWithoutExternalMapTiles() {
        val view = LocationMiniMapView(context())

        view.setPoints(
            listOf(
                LocationMapPoint(
                    latitude = 37.5665,
                    longitude = 126.9780,
                    durationMs = 45 * 60_000L,
                    sampleCount = 3,
                    capturedAtLocalText = "09:45"
                ),
                LocationMapPoint(
                    latitude = 37.5700,
                    longitude = 126.9820,
                    durationMs = 15 * 60_000L,
                    sampleCount = 2,
                    capturedAtLocalText = "14:15"
                )
            )
        )

        assertEquals(2, view.pointCount)
        assertTrue(view.contentDescription.toString().contains("2 location visits"))
        assertTrue(view.contentDescription.toString().contains("Local map preview"))
        assertTrue(view.contentDescription.toString().contains("no network map tiles"))
        assertTrue(view.contentDescription.toString().contains("37.5665"))
        assertTrue(view.contentDescription.toString().contains("126.9780"))
        assertTrue(view.contentDescription.toString().contains("09:45"))
        assertTrue(view.contentDescription.toString().contains("14:15"))
        assertTrue(view.contentDescription.toString().contains("45m"))
    }

    @Test
    fun setPointsNormalizesDottedHourMinuteLabelsToHhMmForScreenshots() {
        val view = LocationMiniMapView(context())

        view.setPoints(
            listOf(
                LocationMapPoint(
                    latitude = 37.5665,
                    longitude = 126.9780,
                    durationMs = 1,
                    sampleCount = 1,
                    capturedAtLocalText = "16.48"
                )
            )
        )

        assertTrue(view.contentDescription.toString().contains("16:48"))
        assertTrue(!view.contentDescription.toString().contains("16.48"))
    }

    private fun context(): Context = ApplicationProvider.getApplicationContext()

    private fun countNonWhitePixels(bitmap: Bitmap): Int {
        var count = 0
        for (x in 0 until bitmap.width) {
            for (y in 0 until bitmap.height) {
                if (contrastFromWhite(bitmap.getPixel(x, y)) > 12) {
                    count++
                }
            }
        }
        return count
    }

    private fun contrastFromWhite(color: Int): Int {
        return maxOf(
            255 - Color.red(color),
            255 - Color.green(color),
            255 - Color.blue(color)
        )
    }
}
