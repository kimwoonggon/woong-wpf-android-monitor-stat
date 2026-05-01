package com.woong.monitorstack.settings

import java.net.URI

object AndroidSyncServerUrlValidator {
    fun isValid(serverBaseUrl: String): Boolean {
        val uri = runCatching { URI(serverBaseUrl.trim()) }.getOrNull() ?: return false
        val scheme = uri.scheme?.lowercase() ?: return false
        val host = uri.host?.lowercase()?.trim('[', ']') ?: return false
        if (uri.rawUserInfo != null) {
            return false
        }

        return when (scheme) {
            "https" -> true
            "http" -> host == "localhost" || host == "127.0.0.1" || host == "::1"
            else -> false
        }
    }
}
