package com.woong.monitorstack.dashboard

import com.woong.monitorstack.usage.AndroidForegroundNoise

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
        val NoisyForegroundPackages = AndroidForegroundNoise.PackageNames
    }
}

data class DashboardCurrentFocusSelection(
    val currentSession: DashboardSessionRow?,
    val latestExternalSession: DashboardSessionRow?
)
