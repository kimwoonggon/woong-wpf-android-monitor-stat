package com.woong.monitorstack.usage

class UsageSessionizer {
    fun sessionize(events: List<UsageEventSnapshot>): List<UsageAppSession> {
        val sessions = mutableListOf<UsageAppSession>()
        var activePackageName: String? = null
        var activeStartedAtUtcMillis: Long? = null

        for (event in events.sortedBy { it.occurredAtUtcMillis }) {
            when (event.eventType) {
                UsageEventType.ACTIVITY_RESUMED -> {
                    activePackageName = event.packageName
                    activeStartedAtUtcMillis = event.occurredAtUtcMillis
                }

                UsageEventType.ACTIVITY_PAUSED -> {
                    val packageName = activePackageName
                    val startedAtUtcMillis = activeStartedAtUtcMillis
                    if (packageName == event.packageName && startedAtUtcMillis != null) {
                        sessions.add(
                            UsageAppSession(
                                packageName,
                                startedAtUtcMillis,
                                event.occurredAtUtcMillis
                            )
                        )
                        activePackageName = null
                        activeStartedAtUtcMillis = null
                    }
                }
            }
        }

        return sessions
    }
}
