package com.woong.monitorstack.privacy

import java.nio.file.Path
import javax.xml.parsers.DocumentBuilderFactory
import org.junit.Assert.assertEquals
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
}
