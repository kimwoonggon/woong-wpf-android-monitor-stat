package com.woong.monitorstack.usage

data class UsageEventSnapshot(
    val packageName: String,
    val eventType: UsageEventType,
    val occurredAtUtcMillis: Long
) {
    init {
        require(packageName.isNotBlank()) { "packageName must not be blank." }
    }
}
