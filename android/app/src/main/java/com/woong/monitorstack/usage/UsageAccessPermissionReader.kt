package com.woong.monitorstack.usage

interface UsageAccessPermissionReader {
    fun isUsageAccessGranted(packageName: String): Boolean
}
