package com.woong.monitorstack.dashboard

import org.junit.Assert.assertEquals
import org.junit.Test

class GoogleMapsAvailabilityPolicyTest {
    @Test
    fun blankApiKeyUsesLocalFallbackMap() {
        val policy = GoogleMapsAvailabilityPolicy(apiKey = "")

        assertEquals(LocationMapMode.LocalPreview, policy.mode())
    }

    @Test
    fun whitespaceApiKeyUsesLocalFallbackMap() {
        val policy = GoogleMapsAvailabilityPolicy(apiKey = "   ")

        assertEquals(LocationMapMode.LocalPreview, policy.mode())
    }

    @Test
    fun configuredApiKeyEnablesGoogleMaps() {
        val policy = GoogleMapsAvailabilityPolicy(apiKey = "AIza-test-key")

        assertEquals(LocationMapMode.GoogleMaps, policy.mode())
    }
}
