package com.woong.monitorstack.usage

import android.content.Intent
import android.provider.Settings

class UsageAccessSettingsIntentFactory {
    fun action(): String = Settings.ACTION_USAGE_ACCESS_SETTINGS

    fun createIntent(): Intent = Intent(action())
}
