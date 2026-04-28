package com.woong.monitorstack.usage

import android.app.AppOpsManager
import android.content.Context
import android.os.Build
import android.os.Process

class AndroidUsageAccessPermissionReader(
    private val context: Context
) : UsageAccessPermissionReader {
    override fun isUsageAccessGranted(packageName: String): Boolean {
        val appOpsManager = context.getSystemService(Context.APP_OPS_SERVICE) as AppOpsManager
        val mode = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            appOpsManager.unsafeCheckOpNoThrow(
                AppOpsManager.OPSTR_GET_USAGE_STATS,
                Process.myUid(),
                packageName
            )
        } else {
            @Suppress("DEPRECATION")
            appOpsManager.checkOpNoThrow(
                AppOpsManager.OPSTR_GET_USAGE_STATS,
                Process.myUid(),
                packageName
            )
        }

        return mode == AppOpsManager.MODE_ALLOWED
    }
}
