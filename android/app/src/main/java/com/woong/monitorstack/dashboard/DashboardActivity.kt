package com.woong.monitorstack.dashboard

import android.os.Bundle
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.github.mikephil.charting.charts.BarChart
import com.github.mikephil.charting.charts.LineChart
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.databinding.ActivityDashboardBinding
import com.woong.monitorstack.usage.UsageAccessSettingsIntentFactory

class DashboardActivity : AppCompatActivity() {
    private lateinit var viewModel: DashboardViewModel
    private val sessionAdapter = SessionsAdapter()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val binding = ActivityDashboardBinding.inflate(layoutInflater)
        setContentView(binding.root)
        val database = MonitorDatabase.getInstance(this)
        viewModel = DashboardViewModel(RoomDashboardRepository(database.focusSessionDao()))

        binding.recentSessionsList.layoutManager = LinearLayoutManager(this)
        binding.recentSessionsList.adapter = sessionAdapter
        configureChart(binding.activityLineChart)
        configureChart(binding.appUsageBarChart)

        val usageAccessSettings = UsageAccessSettingsIntentFactory()
        binding.usageAccessSettingsButton.setOnClickListener {
            startActivity(usageAccessSettings.createIntent())
        }
        binding.todayFilterButton.setOnClickListener { loadPeriod(binding, DashboardPeriod.Today) }
        binding.yesterdayFilterButton.setOnClickListener { loadPeriod(binding, DashboardPeriod.Yesterday) }
        binding.recent7DaysFilterButton.setOnClickListener {
            loadPeriod(binding, DashboardPeriod.Recent7Days)
        }

        loadPeriod(binding, DashboardPeriod.Today)
    }

    private fun configureChart(chart: LineChart) {
        chart.description.isEnabled = false
        chart.setNoDataText(getString(com.woong.monitorstack.R.string.empty_sessions))
    }

    private fun configureChart(chart: BarChart) {
        chart.description.isEnabled = false
        chart.setNoDataText(getString(com.woong.monitorstack.R.string.empty_sessions))
    }

    private fun loadPeriod(binding: ActivityDashboardBinding, period: DashboardPeriod) {
        binding.selectedPeriodText.setText(period.toTextResId())
        Thread {
            viewModel.selectPeriod(period)
            runOnUiThread { render(binding, viewModel.state) }
        }.start()
    }

    private fun DashboardPeriod.toTextResId(): Int {
        return when (this) {
            DashboardPeriod.Today -> com.woong.monitorstack.R.string.filter_today
            DashboardPeriod.Yesterday -> com.woong.monitorstack.R.string.filter_yesterday
            DashboardPeriod.Recent7Days -> com.woong.monitorstack.R.string.filter_recent_7_days
        }
    }

    private fun render(binding: ActivityDashboardBinding, state: DashboardUiState) {
        binding.totalActiveText.text = formatDuration(state.totalActiveMs)
        binding.topAppText.text = state.topAppPackageName ?: getString(com.woong.monitorstack.R.string.no_top_app)
        binding.idleText.text = formatDuration(state.idleMs)
        binding.emptySessionsText.visibility = if (state.recentSessions.isEmpty()) {
            View.VISIBLE
        } else {
            View.GONE
        }
        sessionAdapter.submit(state.recentSessions)
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

    private class SessionsAdapter : RecyclerView.Adapter<SessionViewHolder>() {
        private val sessions = mutableListOf<DashboardSessionRow>()

        fun submit(rows: List<DashboardSessionRow>) {
            sessions.clear()
            sessions.addAll(rows)
            notifyDataSetChanged()
        }

        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): SessionViewHolder {
            val textView = TextView(parent.context)
            textView.setPadding(0, 12, 0, 12)

            return SessionViewHolder(textView)
        }

        override fun onBindViewHolder(holder: SessionViewHolder, position: Int) {
            holder.bind(sessions[position])
        }

        override fun getItemCount(): Int = sessions.size
    }

    private class SessionViewHolder(
        private val textView: TextView
    ) : RecyclerView.ViewHolder(textView) {
        fun bind(row: DashboardSessionRow) {
            textView.text = "${row.startedAtLocalText}  ${row.packageName}  ${row.durationText}"
        }
    }
}
