package com.woong.monitorstack.dashboard

import java.io.File
import org.junit.Assert.assertTrue
import org.junit.Test

class AndroidGoogleMapsConfigurationTest {
    @Test
    fun gradleExposesOptionalGoogleMapsApiKeyWithoutCommittingASecret() {
        val gradle = File("build.gradle.kts").readText()

        assertTrue(gradle.contains("woongGoogleMapsApiKey"))
        assertTrue(gradle.contains("WOONG_ANDROID_GOOGLE_MAPS_API_KEY"))
        assertTrue(gradle.contains("GOOGLE_MAPS_API_KEY"))
        assertTrue(gradle.contains("manifestPlaceholders[\"googleMapsApiKey\"]"))
    }

    @Test
    fun manifestDeclaresGoogleMapsApiKeyPlaceholder() {
        val manifest = File("src/main/AndroidManifest.xml").readText()

        assertTrue(manifest.contains("com.google.android.geo.API_KEY"))
        assertTrue(manifest.contains("\${googleMapsApiKey}"))
        assertTrue(manifest.contains("com.google.android.gms.version"))
    }

    @Test
    fun dashboardLayoutProvidesGoogleMapContainerAndLocalFallback() {
        val layout = File("src/main/res/layout/fragment_dashboard.xml").readText()

        assertTrue(layout.contains("@+id/googleLocationMapContainer"))
        assertTrue(layout.contains("@+id/locationMiniMapView"))
        assertTrue(layout.contains("@+id/locationMapProviderStatusText"))
        assertTrue(
            "Google map container should be declared before the local fallback preview.",
            layout.indexOf("@+id/googleLocationMapContainer") <
                layout.indexOf("@+id/locationMiniMapView")
        )
    }

    @Test
    fun versionCatalogPinsGoogleMapsSdkVersion() {
        val catalog = File("../gradle/libs.versions.toml").readText()

        assertTrue(catalog.contains("playServicesMaps"))
        assertTrue(catalog.contains("com.google.android.gms"))
        assertTrue(catalog.contains("play-services-maps"))
    }
}
