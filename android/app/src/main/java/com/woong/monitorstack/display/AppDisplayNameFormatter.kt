package com.woong.monitorstack.display

object AppDisplayNameFormatter {
    fun format(packageName: String): String {
        val trimmedPackageName = packageName.trim()

        return KnownAppNames[trimmedPackageName]
            ?: trimmedPackageName
                .substringAfterLast('.', trimmedPackageName)
                .replaceFirstChar { char ->
                    if (char.isLowerCase()) {
                        char.titlecase()
                    } else {
                        char.toString()
                    }
                }
    }

    private val KnownAppNames = mapOf(
        "com.android.chrome" to "Chrome",
        "com.google.android.youtube" to "YouTube",
        "com.slack" to "Slack",
        "com.microsoft.teams" to "Teams",
        "com.google.android.gm" to "Gmail",
        "com.kakao.talk" to "KakaoTalk"
    )
}
