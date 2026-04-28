package com.woong.monitorstack.usage

class UsageSessionizer(
    private val sameAppMergeGapMs: Long = 5_000
) {
    init {
        require(sameAppMergeGapMs >= 0) { "sameAppMergeGapMs must not be negative." }
    }

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

        return mergeCloseSameAppSessions(sessions)
    }

    private fun mergeCloseSameAppSessions(sessions: List<UsageAppSession>): List<UsageAppSession> {
        val merged = mutableListOf<UsageAppSession>()

        for (session in sessions) {
            val previous = merged.lastOrNull()
            if (
                previous != null &&
                previous.packageName == session.packageName &&
                session.startedAtUtcMillis - previous.endedAtUtcMillis <= sameAppMergeGapMs
            ) {
                merged[merged.lastIndex] = UsageAppSession(
                    previous.packageName,
                    previous.startedAtUtcMillis,
                    session.endedAtUtcMillis
                )
            } else {
                merged.add(session)
            }
        }

        return merged
    }
}
