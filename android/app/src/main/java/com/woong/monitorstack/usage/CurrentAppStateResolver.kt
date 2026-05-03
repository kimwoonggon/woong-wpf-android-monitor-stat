package com.woong.monitorstack.usage

import com.woong.monitorstack.data.local.CurrentAppStateStatus
import com.woong.monitorstack.display.AppDisplayNameFormatter

class CurrentAppStateResolver(
    private val ignoredPackageNames: Set<String> = AndroidForegroundNoise.PackageNames,
    private val appLabelFormatter: (String) -> String = AppDisplayNameFormatter::format
) {
    fun resolve(
        events: List<UsageEventSnapshot>,
        collectionEndUtcMillis: Long
    ): CurrentAppStateSnapshot? {
        var activePackageName: String? = null

        for (event in events
            .filter { it.occurredAtUtcMillis <= collectionEndUtcMillis }
            .filterNot { it.packageName in ignoredPackageNames }
            .sortedBy { it.occurredAtUtcMillis }
        ) {
            when (event.eventType) {
                UsageEventType.ACTIVITY_RESUMED -> activePackageName = event.packageName
                UsageEventType.ACTIVITY_PAUSED -> {
                    if (activePackageName == event.packageName) {
                        activePackageName = null
                    }
                }
            }
        }

        val packageName = activePackageName ?: return null
        return CurrentAppStateSnapshot(
            packageName = packageName,
            appLabel = appLabelFormatter(packageName),
            status = CurrentAppStateStatus.Active,
            observedAtUtcMillis = collectionEndUtcMillis
        )
    }
}

data class CurrentAppStateSnapshot(
    val packageName: String,
    val appLabel: String,
    val status: CurrentAppStateStatus,
    val observedAtUtcMillis: Long
)
