package com.woong.monitorstack.dashboard

import android.content.Context
import android.view.View
import android.widget.FrameLayout
import android.widget.TextView
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.R
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class DashboardLocationMapControllerTest {
    @Test
    fun blankApiKeyShowsLocalPreviewAndDoesNotCreateGoogleMapView() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val googleContainer = FrameLayout(context)
        val localPreview = LocationMiniMapView(context)
        val statusText = TextView(context)
        val controller = DashboardLocationMapController(
            context = context,
            googleMapContainer = googleContainer,
            localPreview = localPreview,
            providerStatusText = statusText,
            apiKey = ""
        )

        controller.onCreate(savedInstanceState = null)
        controller.render(
            listOf(
                LocationMapPoint(
                    latitude = 37.5665,
                    longitude = 126.9780,
                    durationMs = 10 * 60_000L,
                    sampleCount = 2,
                    capturedAtLocalText = "10:20"
                )
            )
        )

        assertEquals(View.GONE, googleContainer.visibility)
        assertEquals(0, googleContainer.childCount)
        assertEquals(View.VISIBLE, localPreview.visibility)
        assertEquals(1, localPreview.pointCount)
        assertTrue(
            statusText.text.toString().contains(
                context.getString(R.string.location_google_map_unavailable)
            )
        )
    }
}
