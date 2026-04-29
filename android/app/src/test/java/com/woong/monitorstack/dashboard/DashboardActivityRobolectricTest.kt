package com.woong.monitorstack.dashboard

import android.content.Context
import android.view.ContextThemeWrapper
import android.view.LayoutInflater
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.R
import com.woong.monitorstack.databinding.ActivityDashboardBinding
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class DashboardActivityRobolectricTest {
    @Test
    fun dashboardLayoutCoversPlannedAndroidSvgCoreSurface() {
        val context = ContextThemeWrapper(
            ApplicationProvider.getApplicationContext<Context>(),
            R.style.Theme_WoongMonitor
        )
        val binding = ActivityDashboardBinding.inflate(LayoutInflater.from(context))

        assertEquals("Usage OK", binding.usageAccessStatusChip.text.toString())
        assertEquals("Sync Off", binding.syncStatusChip.text.toString())
        assertEquals("Privacy Safe", binding.privacyStatusChip.text.toString())
        assertEquals("Current Focus", binding.currentFocusTitle.text.toString())
        assertEquals("Current app", binding.currentAppLabel.text.toString())
        assertEquals("Package", binding.currentPackageLabel.text.toString())
        assertEquals("Session duration", binding.currentSessionDurationLabel.text.toString())
        assertNotNull(binding.screenOnCard)
        assertNotNull(binding.webFocusCard)
        assertEquals("1h", binding.oneHourFilterButton.text.toString())
        assertEquals("6h", binding.sixHourFilterButton.text.toString())
        assertEquals("24h", binding.twentyFourHourFilterButton.text.toString())
        assertEquals("7d", binding.recent7DaysFilterButton.text.toString())
        assertNotNull(binding.topAppsCard)
        assertNotNull(binding.bottomNavigationRow)
        assertEquals("Dashboard", binding.navDashboardText.text.toString())
        assertEquals("Sessions", binding.navSessionsText.text.toString())
        assertEquals("Report", binding.navReportText.text.toString())
        assertEquals("Settings", binding.navSettingsText.text.toString())
    }

    @Test
    fun dashboardLayoutShowsLocationStatusCardWithSafeDefaults() {
        val context = ContextThemeWrapper(
            ApplicationProvider.getApplicationContext<Context>(),
            R.style.Theme_WoongMonitor
        )
        val binding = ActivityDashboardBinding.inflate(LayoutInflater.from(context))

        assertNotNull(binding.locationContextCard)
        assertEquals("Location context", binding.locationContextLabel.text.toString())
        assertEquals("Location capture off", binding.locationStatusText.text.toString())
        assertEquals("Latitude not stored", binding.locationLatitudeText.text.toString())
        assertEquals("Longitude not stored", binding.locationLongitudeText.text.toString())
        assertEquals("Accuracy unavailable", binding.locationAccuracyText.text.toString())
        assertEquals("No location captured", binding.locationCapturedAtText.text.toString())
    }
}
