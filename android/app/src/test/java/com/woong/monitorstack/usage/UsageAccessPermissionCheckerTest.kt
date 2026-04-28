package com.woong.monitorstack.usage

import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class UsageAccessPermissionCheckerTest {
    @Test
    fun hasUsageAccessReturnsTrueWhenReaderReportsGranted() {
        val checker = UsageAccessPermissionChecker(FakeUsageAccessPermissionReader(isGranted = true))

        assertTrue(checker.hasUsageAccess("com.woong.monitorstack"))
    }

    @Test
    fun hasUsageAccessReturnsFalseWhenReaderReportsDenied() {
        val checker = UsageAccessPermissionChecker(FakeUsageAccessPermissionReader(isGranted = false))

        assertFalse(checker.hasUsageAccess("com.woong.monitorstack"))
    }

    private class FakeUsageAccessPermissionReader(
        private val isGranted: Boolean
    ) : UsageAccessPermissionReader {
        override fun isUsageAccessGranted(packageName: String): Boolean = isGranted
    }
}
