package com.woong.monitorstack.privacy

import java.nio.file.Path
import javax.xml.parsers.DocumentBuilderFactory
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class AndroidManifestPrivacyTest {
    @Test
    fun manifestDisablesBackupForLocalUsageMetadata() {
        val manifest = DocumentBuilderFactory.newInstance()
            .newDocumentBuilder()
            .parse(Path.of("src/main/AndroidManifest.xml").toFile())

        val application = manifest.getElementsByTagName("application").item(0)
        val allowBackup = application.attributes
            .getNamedItem("android:allowBackup")
            .nodeValue

        assertEquals("false", allowBackup)
    }

    @Test
    fun manifestUsesForegroundLocationPermissionsOnlyForOptionalLocationContext() {
        val manifest = DocumentBuilderFactory.newInstance()
            .newDocumentBuilder()
            .parse(Path.of("src/main/AndroidManifest.xml").toFile())
        val permissions = (0 until manifest.getElementsByTagName("uses-permission").length)
            .map { index ->
                manifest.getElementsByTagName("uses-permission")
                    .item(index)
                    .attributes
                    .getNamedItem("android:name")
                    .nodeValue
            }

        assertTrue(permissions.contains("android.permission.ACCESS_COARSE_LOCATION"))
        assertTrue(permissions.contains("android.permission.ACCESS_FINE_LOCATION"))
        assertFalse(permissions.contains("android.permission.ACCESS_BACKGROUND_LOCATION"))
    }

    @Test
    fun manifestKeepsLegacyCompatibilityActivitiesInternalAndAvoidsReportOrAppDetailForks() {
        val manifest = DocumentBuilderFactory.newInstance()
            .newDocumentBuilder()
            .parse(Path.of("src/main/AndroidManifest.xml").toFile())
        val activities = (0 until manifest.getElementsByTagName("activity").length)
            .associate { index ->
                val attributes = manifest.getElementsByTagName("activity")
                    .item(index)
                    .attributes
                attributes.getNamedItem("android:name").nodeValue to
                    attributes.getNamedItem("android:exported")?.nodeValue
            }

        assertEquals("false", activities[".dashboard.DashboardActivity"])
        assertEquals("false", activities[".sessions.SessionsActivity"])
        assertEquals("false", activities[".settings.SettingsActivity"])
        assertFalse(activities.keys.any { it.endsWith("ReportActivity") })
        assertFalse(activities.keys.any { it.endsWith("AppDetailActivity") })
    }
}
