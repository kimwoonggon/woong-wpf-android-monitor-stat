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
    fun manifestUsesNetworkSecurityConfigThatDeniesBroadCleartextAndNamesLoopbackDevHosts() {
        val documentBuilder = DocumentBuilderFactory.newInstance().newDocumentBuilder()
        val manifest = documentBuilder.parse(Path.of("src/main/AndroidManifest.xml").toFile())
        val application = manifest.getElementsByTagName("application").item(0)
        val networkSecurityConfig = application.attributes
            .getNamedItem("android:networkSecurityConfig")
            .nodeValue

        assertEquals("@xml/network_security_config", networkSecurityConfig)
        assertFalse(application.attributes.getNamedItem("android:usesCleartextTraffic")?.nodeValue == "true")

        val config = documentBuilder.parse(Path.of("src/main/res/xml/network_security_config.xml").toFile())
        val baseConfig = config.getElementsByTagName("base-config").item(0)
        assertEquals(
            "false",
            baseConfig.attributes.getNamedItem("cleartextTrafficPermitted").nodeValue,
        )

        val cleartextDomains = (0 until config.getElementsByTagName("domain-config").length)
            .map { index -> config.getElementsByTagName("domain-config").item(index) }
            .filter { node ->
                node.attributes.getNamedItem("cleartextTrafficPermitted")?.nodeValue == "true"
            }
            .flatMap { node ->
                (0 until node.childNodes.length)
                    .map { index -> node.childNodes.item(index) }
                    .filter { child -> child.nodeName == "domain" }
                    .map { child -> child.textContent.trim() }
            }

        assertEquals(listOf("localhost", "127.0.0.1", "::1"), cleartextDomains)
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
