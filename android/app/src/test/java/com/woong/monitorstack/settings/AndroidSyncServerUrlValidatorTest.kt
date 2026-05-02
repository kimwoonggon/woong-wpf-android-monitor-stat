package com.woong.monitorstack.settings

import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class AndroidSyncServerUrlValidatorTest {
    @Test
    fun acceptsHttpsServerUrlsAndExplicitLoopbackHttpUrlsOnly() {
        assertTrue(AndroidSyncServerUrlValidator.isValid("https://sync.example"))
        assertTrue(AndroidSyncServerUrlValidator.isValid("https://sync.example/"))
        assertTrue(AndroidSyncServerUrlValidator.isValid("http://localhost:5080"))
        assertTrue(AndroidSyncServerUrlValidator.isValid("http://127.0.0.1:5080"))
        assertTrue(AndroidSyncServerUrlValidator.isValid("http://10.0.2.2:5080"))
        assertTrue(AndroidSyncServerUrlValidator.isValid("http://[::1]:5080"))

        assertFalse(AndroidSyncServerUrlValidator.isValid(""))
        assertFalse(AndroidSyncServerUrlValidator.isValid("   "))
        assertFalse(AndroidSyncServerUrlValidator.isValid("http://sync.example"))
        assertFalse(AndroidSyncServerUrlValidator.isValid("https://user:pass@sync.example"))
        assertFalse(AndroidSyncServerUrlValidator.isValid("not a url"))
    }

    @Test
    fun productionEndpointRequiresHttpsAndRejectsLoopbackOrExampleFallbacks() {
        assertTrue(AndroidSyncServerUrlValidator.isValidProductionEndpoint("https://sync.example"))

        assertFalse(AndroidSyncServerUrlValidator.isValidProductionEndpoint(""))
        assertFalse(AndroidSyncServerUrlValidator.isValidProductionEndpoint("   "))
        assertFalse(AndroidSyncServerUrlValidator.isValidProductionEndpoint("http://localhost:5080"))
        assertFalse(AndroidSyncServerUrlValidator.isValidProductionEndpoint("http://127.0.0.1:5080"))
        assertFalse(AndroidSyncServerUrlValidator.isValidProductionEndpoint("http://10.0.2.2:5080"))
        assertFalse(AndroidSyncServerUrlValidator.isValidProductionEndpoint("http://[::1]:5080"))
        assertFalse(AndroidSyncServerUrlValidator.isValidProductionEndpoint("https://example.com"))
        assertFalse(AndroidSyncServerUrlValidator.isValidProductionEndpoint("https://user:pass@sync.example"))
    }

    @Test
    fun classifiesLocalDevelopmentHttpEndpointsForVisibleNonProductionLabeling() {
        assertTrue(AndroidSyncServerUrlValidator.isLocalDevelopmentEndpoint("http://localhost:5080"))
        assertTrue(AndroidSyncServerUrlValidator.isLocalDevelopmentEndpoint("http://127.0.0.1:5080"))
        assertTrue(AndroidSyncServerUrlValidator.isLocalDevelopmentEndpoint("http://10.0.2.2:5080"))
        assertTrue(AndroidSyncServerUrlValidator.isLocalDevelopmentEndpoint("http://[::1]:5080"))

        assertFalse(AndroidSyncServerUrlValidator.isLocalDevelopmentEndpoint("https://sync.example"))
        assertFalse(AndroidSyncServerUrlValidator.isLocalDevelopmentEndpoint("http://sync.example"))
        assertFalse(AndroidSyncServerUrlValidator.isLocalDevelopmentEndpoint("https://user:pass@sync.example"))
        assertFalse(AndroidSyncServerUrlValidator.isLocalDevelopmentEndpoint(""))
    }
}
