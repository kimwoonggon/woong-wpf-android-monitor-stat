package com.woong.monitorstack.summary

import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class NotificationPermissionPolicyTest {
    @Test
    fun shouldRequestPermissionOnlyOnAndroid13AndNewerWhenNotGranted() {
        assertFalse(NotificationPermissionPolicy.shouldRequestPermission(sdkInt = 32, isGranted = false))
        assertFalse(NotificationPermissionPolicy.shouldRequestPermission(sdkInt = 33, isGranted = true))
        assertTrue(NotificationPermissionPolicy.shouldRequestPermission(sdkInt = 33, isGranted = false))
    }
}
