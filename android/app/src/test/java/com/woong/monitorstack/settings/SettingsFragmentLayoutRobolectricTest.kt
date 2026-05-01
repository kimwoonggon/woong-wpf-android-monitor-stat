package com.woong.monitorstack.settings

import android.content.Context
import android.text.InputType
import android.view.ContextThemeWrapper
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.TextView
import androidx.core.content.ContextCompat
import androidx.test.core.app.ApplicationProvider
import com.google.android.material.switchmaterial.SwitchMaterial
import com.woong.monitorstack.R
import com.woong.monitorstack.databinding.FragmentSettingsBinding
import kotlin.math.roundToInt
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertTrue
import org.junit.Assert.fail
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class SettingsFragmentLayoutRobolectricTest {
    @Test
    fun settingsFragmentGroupsControlsAndPrivacyCopyIntoCompactFigmaRows() {
        val context = ContextThemeWrapper(
            ApplicationProvider.getApplicationContext<Context>(),
            R.style.Theme_WoongMonitor
        )
        val binding = FragmentSettingsBinding.inflate(LayoutInflater.from(context))
        val primaryColor = ContextCompat.getColor(context, R.color.wms_text_primary)
        val secondaryColor = ContextCompat.getColor(context, R.color.wms_text_secondary)

        assertSectionHeading(
            context,
            binding.root,
            "permissionsSectionHeading",
            context.getString(R.string.permissions_title)
        )
        assertSectionHeading(
            context,
            binding.root,
            "collectionSectionHeading",
            context.getString(R.string.collection_title)
        )
        assertSectionHeading(
            context,
            binding.root,
            "syncSectionHeading",
            context.getString(R.string.sync_title)
        )
        assertSectionHeading(
            context,
            binding.root,
            "privacyStorageSectionHeading",
            context.getString(R.string.privacy_storage_title)
        )

        assertEquals(dp(context, 48), binding.usageAccessStatusRow.layoutParams.height)
        assertEquals(primaryColor, binding.usageAccessStatusRow.currentTextColor)

        assertToggleRow(
            context = context,
            root = binding.root,
            rowName = "backgroundCollectionRow",
            labelText = context.getString(R.string.background_collection),
            switchId = R.id.backgroundCollectionSwitch,
            primaryColor = primaryColor
        )
        assertToggleRow(
            context = context,
            root = binding.root,
            rowName = "autoSyncRow",
            labelText = context.getString(R.string.auto_sync),
            switchId = R.id.autoSyncSwitch,
            primaryColor = primaryColor
        )
        assertEquals(primaryColor, binding.syncStatusText.currentTextColor)
        assertNotNull(binding.syncServerUrlEditText)
        assertNotNull(binding.syncDeviceIdEditText)
        assertEquals(
            context.getString(R.string.sync_server_url_hint),
            binding.syncServerUrlEditText.hint.toString()
        )
        assertEquals(
            context.getString(R.string.sync_device_id_hint),
            binding.syncDeviceIdEditText.hint.toString()
        )
        assertEquals(
            InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_URI,
            binding.syncServerUrlEditText.inputType
        )
        assertEquals(
            InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_NORMAL,
            binding.syncDeviceIdEditText.inputType
        )
        val syncInputMetadata = binding.syncSettingsCard.descendantEditTexts()
            .map { editText ->
                val name = context.resources.getResourceEntryName(editText.id)
                "$name ${editText.hint}"
            }
            .joinToString(" ")
            .lowercase()
        assertTrue("Sync settings must not expose secret input fields.", "password" !in syncInputMetadata)
        assertTrue("Sync settings must not expose token input fields.", "token" !in syncInputMetadata)
        assertTrue("Sync settings must not expose secret input fields.", "secret" !in syncInputMetadata)

        assertTextColor(
            binding.usageAccessGuidanceText,
            context.getString(R.string.usage_access_guidance),
            secondaryColor
        )
        assertTextColor(
            binding.sensitiveDataBoundaryText,
            context.getString(R.string.sensitive_data_boundary),
            secondaryColor
        )
        assertTextColor(
            binding.notificationPermissionGuidanceText,
            context.getString(R.string.notification_permission_guidance),
            secondaryColor
        )
        assertDescendantTextColor(
            binding.privacyStorageSettingsCard,
            context.getString(R.string.android_room_storage_status),
            primaryColor
        )
        assertDescendantTextColor(
            binding.privacyStorageSettingsCard,
            context.getString(R.string.sensitive_data_boundary),
            secondaryColor
        )
        assertDescendantTextColor(
            binding.privacyStorageSettingsCard,
            context.getString(R.string.android_storage_boundary),
            secondaryColor
        )
    }

    private fun assertSectionHeading(
        context: Context,
        root: View,
        resourceName: String,
        expectedText: String
    ) {
        val heading = root.requireViewByResourceName<TextView>(context, resourceName)
        assertEquals(expectedText, heading.text.toString())
    }

    private fun assertToggleRow(
        context: Context,
        root: View,
        rowName: String,
        labelText: String,
        switchId: Int,
        primaryColor: Int
    ) {
        val row = root.requireViewByResourceName<LinearLayout>(context, rowName)
        assertEquals(dp(context, 48), row.layoutParams.height)
        assertEquals(LinearLayout.HORIZONTAL, row.orientation)

        val label = row.requireDescendantTextView(labelText)
        val labelLayoutParams = label.layoutParams as LinearLayout.LayoutParams
        assertEquals(1f, labelLayoutParams.weight)
        assertEquals(primaryColor, label.currentTextColor)

        assertNotNull(row.findViewById<SwitchMaterial>(switchId))
    }

    private fun assertTextColor(textView: TextView, expectedText: String, expectedColor: Int) {
        assertEquals(expectedText, textView.text.toString())
        assertEquals(expectedColor, textView.currentTextColor)
    }

    private fun assertDescendantTextColor(root: View, expectedText: String, expectedColor: Int) {
        val textView = root.requireDescendantTextView(expectedText)
        assertEquals(expectedColor, textView.currentTextColor)
    }

    private inline fun <reified T : View> View.requireViewByResourceName(
        context: Context,
        resourceName: String
    ): T {
        val id = context.resources.getIdentifier(resourceName, "id", context.packageName)
        assertNotEquals("Missing view id $resourceName", 0, id)
        val view = findViewById<T>(id)
        if (view == null) {
            fail("View $resourceName was not found")
        }
        return view
    }

    private fun View.requireDescendantTextView(expectedText: String): TextView {
        val match = descendantTextViews().firstOrNull { it.text.toString() == expectedText }
        if (match == null) {
            fail("TextView with text '$expectedText' was not found")
        }
        return match!!
    }

    private fun View.descendantTextViews(): Sequence<TextView> = sequence {
        if (this@descendantTextViews is TextView) {
            yield(this@descendantTextViews)
        }
        if (this@descendantTextViews is ViewGroup) {
            for (index in 0 until childCount) {
                yieldAll(getChildAt(index).descendantTextViews())
            }
        }
    }

    private fun View.descendantEditTexts(): Sequence<EditText> = sequence {
        if (this@descendantEditTexts is EditText) {
            yield(this@descendantEditTexts)
        }
        if (this@descendantEditTexts is ViewGroup) {
            for (index in 0 until childCount) {
                yieldAll(getChildAt(index).descendantEditTexts())
            }
        }
    }

    private fun dp(context: Context, value: Int): Int =
        (value * context.resources.displayMetrics.density).roundToInt()
}
