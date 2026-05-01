package com.woong.monitorstack.sessions

import android.content.Context
import android.view.LayoutInflater
import android.view.ViewGroup
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import androidx.test.core.app.ApplicationProvider
import com.woong.monitorstack.R
import com.woong.monitorstack.databinding.ItemFocusSessionBinding
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.Robolectric
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35])
class SessionRowVisualTest {
    @Test
    fun focusSessionRowLayoutIsCompactAndKeepsRequiredMetadata() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val binding = ItemFocusSessionBinding.inflate(LayoutInflater.from(context))
        val density = context.resources.displayMetrics.density

        assertTrue("Session rows should fit dense scanning.", binding.root.minHeight <= 72.dp(density))
        assertTrue("Session rows should use compact vertical padding.", binding.root.paddingTop <= 6.dp(density))
        assertTrue("Session rows should use compact vertical padding.", binding.root.paddingBottom <= 6.dp(density))

        val iconLayoutParams = binding.sessionAppIconPlaceholder.layoutParams
        assertTrue("Icon placeholder should be compact.", iconLayoutParams.width <= 32.dp(density))
        assertTrue("Icon placeholder should be compact.", iconLayoutParams.height <= 32.dp(density))

        assertRequiredTextView(binding.sessionAppNameText)
        assertRequiredTextView(binding.sessionPackageText)
        assertRequiredTextView(binding.sessionTimeRangeText)
        assertRequiredTextView(binding.sessionDurationText)
        assertRequiredTextView(binding.sessionStateText)
    }

    @Test
    fun sessionRowAdapterBindsAllMetadataIntoCompactRow() {
        val activity = Robolectric.buildActivity(AppCompatActivity::class.java)
            .setup()
            .get()
        activity.setTheme(R.style.Theme_WoongMonitor)
        val parent = RecyclerView(activity)
        parent.layoutManager = LinearLayoutManager(activity)
        parent.layoutParams = ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT,
            ViewGroup.LayoutParams.WRAP_CONTENT
        )
        val adapter = SessionRowAdapter()
        val row = SessionRow(
            appName = "Chrome",
            packageName = "com.android.chrome",
            durationText = "12m 5s",
            timeRangeText = "09:00 - 09:12",
            stateText = "Active"
        )

        adapter.submitRows(listOf(row))
        val holder = adapter.onCreateViewHolder(parent, 0)
        adapter.onBindViewHolder(holder, 0)

        assertEquals("C", holder.itemView.findViewById<TextView>(R.id.sessionAppIconPlaceholder).text)
        assertEquals("Chrome", holder.itemView.findViewById<TextView>(R.id.sessionAppNameText).text)
        assertEquals("com.android.chrome", holder.itemView.findViewById<TextView>(R.id.sessionPackageText).text)
        assertEquals("09:00 - 09:12", holder.itemView.findViewById<TextView>(R.id.sessionTimeRangeText).text)
        assertEquals("12m 5s", holder.itemView.findViewById<TextView>(R.id.sessionDurationText).text)
        assertEquals("Active", holder.itemView.findViewById<TextView>(R.id.sessionStateText).text)
    }

    private fun assertRequiredTextView(textView: TextView) {
        assertEquals(ViewGroup.LayoutParams.WRAP_CONTENT, textView.layoutParams.height)
        assertTrue("Required metadata text should stay visible.", textView.visibility == android.view.View.VISIBLE)
    }

    private fun Int.dp(density: Float): Int = (this * density).toInt()
}
