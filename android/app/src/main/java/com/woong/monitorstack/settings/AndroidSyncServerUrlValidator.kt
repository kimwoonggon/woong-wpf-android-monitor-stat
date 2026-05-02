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

    fun isValidProductionEndpoint(serverBaseUrl: String): Boolean {
        val uri = runCatching { URI(serverBaseUrl.trim()) }.getOrNull() ?: return false
        val scheme = uri.scheme?.lowercase() ?: return false
        val host = uri.host?.lowercase()?.trim('[', ']') ?: return false
        if (uri.rawUserInfo != null) {
            return false
        }

        return scheme == "https" &&
            !isLoopbackHost(host) &&
            !isExampleHost(host)
    }

    fun productionEndpointOrBlank(serverBaseUrl: String): String {
        val trimmed = serverBaseUrl.trim()
        return if (isValidProductionEndpoint(trimmed)) {
            trimmed
        } else {
            ""
        }
    }

    private fun isLoopbackHost(host: String): Boolean {
        return host == "localhost" || host == "127.0.0.1" || host == "::1"
    }

    private fun isExampleHost(host: String): Boolean {
        return host == "example.com" || host.endsWith(".example.com")
    }
}
