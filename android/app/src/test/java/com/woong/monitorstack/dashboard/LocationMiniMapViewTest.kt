package com.woong.monitorstack.dashboard

import android.content.Context
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

    private fun context(): Context = ApplicationProvider.getApplicationContext()
}
