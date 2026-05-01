package com.woong.monitorstack.usage

class UsageSessionizer(
    private val sameAppMergeGapMs: Long = 5_000
) {
    init {
        require(sameAppMergeGapMs >= 0) { "sameAppMergeGapMs must not be negative." }
    }

    fun sessionize(
        events: List<UsageEventSnapshot>,
        collectionStartUtcMillis: Long? = null,
        collectionEndUtcMillis: Long? = null
    ): List<UsageAppSession> {
        val sessions = mutableListOf<UsageAppSession>()
        var activePackageName: String? = null
        var activeStartedAtUtcMillis: Long? = null

        for (event in events.sortedBy { it.occurredAtUtcMillis }) {
            when (event.eventType) {
                UsageEventType.ACTIVITY_RESUMED -> {
                    val packageName = activePackageName
                    val startedAtUtcMillis = activeStartedAtUtcMillis
                    if (
                        packageName != null &&
                        packageName != event.packageName &&
                        startedAtUtcMillis != null
                    ) {
                        sessions.addClampedSession(
                            packageName = packageName,
                            startedAtUtcMillis = startedAtUtcMillis,
                            endedAtUtcMillis = event.occurredAtUtcMillis,
                            collectionStartUtcMillis = collectionStartUtcMillis,
                            collectionEndUtcMillis = collectionEndUtcMillis
                        )
                    }
                    activePackageName = event.packageName
                    activeStartedAtUtcMillis = event.occurredAtUtcMillis
                }

                UsageEventType.ACTIVITY_PAUSED -> {
                    val packageName = activePackageName
                    val startedAtUtcMillis = activeStartedAtUtcMillis
                    if (packageName == event.packageName && startedAtUtcMillis != null) {
                        sessions.addClampedSession(
                            packageName = packageName,
                            startedAtUtcMillis = startedAtUtcMillis,
                            endedAtUtcMillis = event.occurredAtUtcMillis,
                            collectionStartUtcMillis = collectionStartUtcMillis,
                            collectionEndUtcMillis = collectionEndUtcMillis
                        )
                        activePackageName = null
                        activeStartedAtUtcMillis = null
                    }
                }
            }
        }

        val packageName = activePackageName
        val startedAtUtcMillis = activeStartedAtUtcMillis
        if (
            collectionEndUtcMillis != null &&
            packageName != null &&
            startedAtUtcMillis != null
        ) {
            sessions.addClampedSession(
                packageName = packageName,
                startedAtUtcMillis = startedAtUtcMillis,
                endedAtUtcMillis = collectionEndUtcMillis,
                collectionStartUtcMillis = collectionStartUtcMillis,
                collectionEndUtcMillis = collectionEndUtcMillis
            )
        }

        return mergeCloseSameAppSessions(sessions)
    }

    private fun MutableList<UsageAppSession>.addClampedSession(
        packageName: String,
        startedAtUtcMillis: Long,
        endedAtUtcMillis: Long,
        collectionStartUtcMillis: Long?,
        collectionEndUtcMillis: Long?
    ) {
        val clampedStart = maxOf(startedAtUtcMillis, collectionStartUtcMillis ?: startedAtUtcMillis)
        val clampedEnd = minOf(endedAtUtcMillis, collectionEndUtcMillis ?: endedAtUtcMillis)

        if (clampedEnd > clampedStart) {
            add(
                UsageAppSession(
                    packageName,
                    clampedStart,
                    clampedEnd
                )
            )
        }
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
