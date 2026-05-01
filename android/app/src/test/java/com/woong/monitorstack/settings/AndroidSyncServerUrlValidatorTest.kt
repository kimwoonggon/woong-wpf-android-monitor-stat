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
        assertTrue(AndroidSyncServerUrlValidator.isValid("http://[::1]:5080"))

        assertFalse(AndroidSyncServerUrlValidator.isValid(""))
        assertFalse(AndroidSyncServerUrlValidator.isValid("   "))
        assertFalse(AndroidSyncServerUrlValidator.isValid("http://sync.example"))
        assertFalse(AndroidSyncServerUrlValidator.isValid("https://user:pass@sync.example"))
        assertFalse(AndroidSyncServerUrlValidator.isValid("not a url"))
    }
}
