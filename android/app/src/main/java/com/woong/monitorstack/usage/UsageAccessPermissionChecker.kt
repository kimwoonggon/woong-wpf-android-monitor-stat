package com.woong.monitorstack.usage

class UsageAccessPermissionChecker(
    private val reader: UsageAccessPermissionReader
) {
    fun hasUsageAccess(packageName: String): Boolean {
        require(packageName.isNotBlank()) { "packageName must not be blank." }

        return reader.isUsageAccessGranted(packageName)
    }
}
