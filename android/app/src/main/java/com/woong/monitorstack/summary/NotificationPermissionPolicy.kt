package com.woong.monitorstack.summary

import android.Manifest
import android.content.pm.PackageManager
import android.os.Build
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat

object NotificationPermissionPolicy {
    fun shouldRequestPermission(
        sdkInt: Int,
        isGranted: Boolean
    ): Boolean {
        return sdkInt >= Build.VERSION_CODES.TIRAMISU && !isGranted
    }
}

class NotificationPermissionController(
    private val activity: AppCompatActivity
) {
    fun requestIfNeeded() {
        val isGranted = if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) {
            true
        } else {
            ContextCompat.checkSelfPermission(
                activity,
                Manifest.permission.POST_NOTIFICATIONS
            ) == PackageManager.PERMISSION_GRANTED
        }

        if (NotificationPermissionPolicy.shouldRequestPermission(Build.VERSION.SDK_INT, isGranted)) {
            ActivityCompat.requestPermissions(
                activity,
                arrayOf(Manifest.permission.POST_NOTIFICATIONS),
                RequestCode
            )
        }
    }

    companion object {
        const val RequestCode = 204
    }
}
