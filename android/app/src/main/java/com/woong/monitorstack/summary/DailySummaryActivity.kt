package com.woong.monitorstack.summary

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.woong.monitorstack.databinding.ActivityDailySummaryBinding

class DailySummaryActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val binding = ActivityDailySummaryBinding.inflate(layoutInflater)
        setContentView(binding.root)
        render(binding, readStateFromIntent())
    }

    private fun readStateFromIntent(): DailySummaryUiState {
        val summaryDate = intent.getStringExtra(EXTRA_SUMMARY_DATE).orEmpty()
        return DailySummaryUiState(
            summaryDateText = summaryDate,
            activeTimeText = formatDuration(intent.getLongExtra(EXTRA_ACTIVE_MS, 0L)),
            idleTimeText = formatDuration(intent.getLongExtra(EXTRA_IDLE_MS, 0L)),
            webTimeText = formatDuration(intent.getLongExtra(EXTRA_WEB_MS, 0L)),
            topAppText = intent.getStringExtra(EXTRA_TOP_APP) ?: DefaultTopValue,
            topDomainText = intent.getStringExtra(EXTRA_TOP_DOMAIN) ?: DefaultTopValue
        )
    }

    private fun render(
        binding: ActivityDailySummaryBinding,
        state: DailySummaryUiState
    ) {
        binding.dailySummaryDateText.text = state.summaryDateText
        binding.dailySummaryActiveText.text = state.activeTimeText
        binding.dailySummaryIdleText.text = state.idleTimeText
        binding.dailySummaryWebText.text = state.webTimeText
        binding.dailySummaryTopAppText.text = state.topAppText
        binding.dailySummaryTopDomainText.text = state.topDomainText
    }

    private fun formatDuration(durationMs: Long): String {
        val totalMinutes = durationMs / 60_000
        val hours = totalMinutes / 60
        val minutes = totalMinutes % 60

        return if (hours > 0) {
            "${hours}h ${minutes}m"
        } else {
            "${minutes}m"
        }
    }

    companion object {
        const val EXTRA_SUMMARY_DATE = "summaryDate"
        const val EXTRA_ACTIVE_MS = "activeMs"
        const val EXTRA_IDLE_MS = "idleMs"
        const val EXTRA_WEB_MS = "webMs"
        const val EXTRA_TOP_APP = "topApp"
        const val EXTRA_TOP_DOMAIN = "topDomain"
        private const val DefaultTopValue = "None"
    }
}
