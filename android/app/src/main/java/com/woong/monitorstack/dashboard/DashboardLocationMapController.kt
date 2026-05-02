package com.woong.monitorstack.dashboard

import android.content.Context
import android.graphics.Color
import android.os.Bundle
import android.view.View
import android.widget.FrameLayout
import android.widget.TextView
import com.google.android.gms.maps.CameraUpdateFactory
import com.google.android.gms.maps.GoogleMap
import com.google.android.gms.maps.MapView
import com.google.android.gms.maps.model.LatLng
import com.google.android.gms.maps.model.LatLngBounds
import com.google.android.gms.maps.model.MarkerOptions
import com.google.android.gms.maps.model.PolylineOptions
import com.woong.monitorstack.BuildConfig
import com.woong.monitorstack.R

class DashboardLocationMapController(
    private val context: Context,
    private val googleMapContainer: FrameLayout,
    private val localPreview: LocationMiniMapView,
    private val providerStatusText: TextView,
    apiKey: String = BuildConfig.GOOGLE_MAPS_API_KEY
) {
    private val mode = GoogleMapsAvailabilityPolicy(apiKey).mode()
    private var mapView: MapView? = null
    private var googleMap: GoogleMap? = null
    private var pendingPoints: List<LocationMapPoint> = emptyList()

    fun onCreate(savedInstanceState: Bundle?) {
        if (mode != LocationMapMode.GoogleMaps) {
            showLocalPreview(context.getString(R.string.location_google_map_unavailable))
            return
        }

        googleMapContainer.visibility = View.VISIBLE
        localPreview.visibility = View.GONE
        providerStatusText.text = context.getString(R.string.location_google_map_enabled)

        mapView = MapView(context).also { view ->
            view.layoutParams = FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.MATCH_PARENT
            )
            googleMapContainer.removeAllViews()
            googleMapContainer.addView(view)
            view.onCreate(savedInstanceState)
            view.getMapAsync { map ->
                googleMap = map
                configureGoogleMap(map)
                renderGoogleMap()
            }
        }
    }

    fun render(points: List<LocationMapPoint>) {
        pendingPoints = points
        localPreview.setPoints(points)

        if (mode != LocationMapMode.GoogleMaps) {
            showLocalPreview(context.getString(R.string.location_google_map_unavailable))
            return
        }

        googleMapContainer.visibility = View.VISIBLE
        localPreview.visibility = View.GONE
        providerStatusText.text = context.getString(R.string.location_google_map_enabled)
        renderGoogleMap()
    }

    fun onStart() {
        mapView?.onStart()
    }

    fun onResume() {
        mapView?.onResume()
    }

    fun onPause() {
        mapView?.onPause()
    }

    fun onStop() {
        mapView?.onStop()
    }

    fun onDestroy() {
        mapView?.onDestroy()
    }

    fun onLowMemory() {
        mapView?.onLowMemory()
    }

    fun onSaveInstanceState(outState: Bundle) {
        mapView?.onSaveInstanceState(outState)
    }

    private fun showLocalPreview(statusText: String) {
        googleMapContainer.visibility = View.GONE
        localPreview.visibility = View.VISIBLE
        providerStatusText.text = statusText
    }

    private fun configureGoogleMap(map: GoogleMap) {
        map.uiSettings.isMapToolbarEnabled = false
        map.uiSettings.isCompassEnabled = true
        map.uiSettings.isZoomControlsEnabled = false
    }

    private fun renderGoogleMap() {
        val map = googleMap ?: return
        map.clear()

        if (pendingPoints.isEmpty()) {
            return
        }

        val boundsBuilder = LatLngBounds.Builder()
        val path = pendingPoints.map { point ->
            LatLng(point.latitude, point.longitude).also { latLng ->
                boundsBuilder.include(latLng)
                map.addMarker(
                    MarkerOptions()
                        .position(latLng)
                        .title(point.capturedAtLocalText)
                        .snippet(
                            context.getString(
                                R.string.location_google_map_marker_snippet,
                                formatDuration(point.durationMs),
                                point.sampleCount
                            )
                        )
                )
            }
        }

        if (path.size > 1) {
            map.addPolyline(
                PolylineOptions()
                    .addAll(path)
                    .color(Color.rgb(15, 107, 222))
                    .width(8f)
            )
        }

        val firstPoint = path.first()
        try {
            if (path.size == 1) {
                map.moveCamera(CameraUpdateFactory.newLatLngZoom(firstPoint, 15f))
            } else {
                map.moveCamera(CameraUpdateFactory.newLatLngBounds(boundsBuilder.build(), 64))
            }
        } catch (_: IllegalStateException) {
            map.moveCamera(CameraUpdateFactory.newLatLngZoom(firstPoint, 14f))
        }
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
