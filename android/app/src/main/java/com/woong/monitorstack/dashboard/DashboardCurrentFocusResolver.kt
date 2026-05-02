package com.woong.monitorstack.dashboard

class DashboardCurrentFocusResolver(
    private val monitorPackageName: String
) {
    fun resolve(recentSessions: List<DashboardSessionRow>): DashboardCurrentFocusSelection {
        val currentSession = recentSessions.firstOrNull { session ->
            session.packageName !in NoisyForegroundPackages
        }
        val latestExternalSession = recentSessions.firstOrNull { session ->
            session.packageName != monitorPackageName &&
                session.packageName !in NoisyForegroundPackages
        }

        return DashboardCurrentFocusSelection(
            currentSession = currentSession,
            latestExternalSession = latestExternalSession
        )
    }

    companion object {
        val NoisyForegroundPackages = setOf(
            "com.google.android.apps.nexuslauncher",
            "com.android.launcher",
            "com.android.launcher2",
            "com.android.launcher3",
            "com.android.systemui",
            "com.google.android.googlequicksearchbox"
        )
    }
}

data class DashboardCurrentFocusSelection(
    val currentSession: DashboardSessionRow?,
    val latestExternalSession: DashboardSessionRow?
)
