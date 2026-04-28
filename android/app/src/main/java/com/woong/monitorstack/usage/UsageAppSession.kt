package com.woong.monitorstack.usage

data class UsageAppSession(
    val packageName: String,
    val startedAtUtcMillis: Long,
    val endedAtUtcMillis: Long
) {
    init {
        require(packageName.isNotBlank()) { "packageName must not be blank." }
        require(endedAtUtcMillis >= startedAtUtcMillis) {
            "endedAtUtcMillis must be on or after startedAtUtcMillis."
        }
    }

    val durationMs: Long = endedAtUtcMillis - startedAtUtcMillis
}
