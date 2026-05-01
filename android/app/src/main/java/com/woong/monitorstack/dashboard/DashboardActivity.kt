package com.woong.monitorstack.dashboard

import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.view.View
import android.view.ViewGroup
import android.view.LayoutInflater
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.github.mikephil.charting.data.BarData
import com.github.mikephil.charting.data.BarDataSet
import com.github.mikephil.charting.data.LineData
import com.github.mikephil.charting.data.LineDataSet
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.databinding.ActivityDashboardBinding
import com.woong.monitorstack.databinding.ItemFocusSessionBinding
import com.woong.monitorstack.usage.UsageAccessSettingsIntentFactory

class DashboardActivity : AppCompatActivity() {
    companion object {
        fun createIntent(context: Context): Intent {
            return Intent(context, DashboardActivity::class.java)
        }
    }

    private lateinit var viewModel: DashboardViewModel
    private val sessionAdapter = SessionsAdapter()
    private val chartMapper = DashboardChartMapper()
    private val chartConfigurator = DashboardChartConfigurator()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val binding = ActivityDashboardBinding.inflate(layoutInflater)
        setContentView(binding.root)
        val database = MonitorDatabase.getInstance(this)
        viewModel = DashboardViewModel(
            RoomDashboardRepository(
                dao = database.focusSessionDao(),
                locationDao = database.locationContextSnapshotDao()
            )
        )

        binding.recentSessionsList.layoutManager = LinearLayoutManager(this)
        binding.recentSessionsList.adapter = sessionAdapter
        chartConfigurator.configureHourlyChart(binding.activityLineChart)
        chartConfigurator.configureAppUsageChart(binding.appUsageBarChart)

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
            DashboardPeriod.LastHour -> com.woong.monitorstack.R.string.filter_1h
            DashboardPeriod.LastSixHours -> com.woong.monitorstack.R.string.filter_6h
            DashboardPeriod.LastTwentyFourHours -> com.woong.monitorstack.R.string.filter_24h
            DashboardPeriod.Recent7Days -> com.woong.monitorstack.R.string.filter_recent_7_days
        }
    }

    private fun render(binding: ActivityDashboardBinding, state: DashboardUiState) {
        binding.totalActiveText.text = formatDuration(state.totalActiveMs)
        binding.topAppText.text = state.topAppName ?: getString(com.woong.monitorstack.R.string.no_top_app)
        binding.idleText.text = formatDuration(state.idleMs)
        binding.locationStatusText.text = state.locationContext.statusText
        binding.locationLatitudeText.text = getString(
            com.woong.monitorstack.R.string.location_latitude_value,
            state.locationContext.latitudeText
        )
        binding.locationLongitudeText.text = getString(
            com.woong.monitorstack.R.string.location_longitude_value,
            state.locationContext.longitudeText
        )
        binding.locationAccuracyText.text = getString(
            com.woong.monitorstack.R.string.location_accuracy_value,
            state.locationContext.accuracyText
        )
        binding.locationCapturedAtText.text = getString(
            com.woong.monitorstack.R.string.location_captured_at_value,
            state.locationContext.capturedAtLocalText
        )
        binding.emptySessionsText.visibility = if (state.recentSessions.isEmpty()) {
            View.VISIBLE
        } else {
            View.GONE
        }
        sessionAdapter.submit(state.recentSessions)
        renderCharts(binding, state.chartData)
    }

    private fun renderCharts(binding: ActivityDashboardBinding, chartData: DashboardChartData) {
        val chartEntries = chartMapper.map(chartData)
        chartConfigurator.configureAppUsageChart(binding.appUsageBarChart, chartEntries.appLabels)

        if (chartEntries.activityEntries.isEmpty()) {
            binding.activityLineChart.clear()
        } else {
            binding.activityLineChart.data = LineData(
                LineDataSet(chartEntries.activityEntries, "Activity")
            )
        }

        if (chartEntries.appEntries.isEmpty()) {
            binding.appUsageBarChart.clear()
        } else {
            binding.appUsageBarChart.data = BarData(
                BarDataSet(chartEntries.appEntries, "Apps")
            )
        }

        binding.activityLineChart.invalidate()
        binding.appUsageBarChart.invalidate()
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
            val binding = ItemFocusSessionBinding.inflate(
                LayoutInflater.from(parent.context),
                parent,
                false
            )

            return SessionViewHolder(binding)
        }

        override fun onBindViewHolder(holder: SessionViewHolder, position: Int) {
            holder.bind(sessions[position])
        }

        override fun getItemCount(): Int = sessions.size
    }

    private class SessionViewHolder(
        private val binding: ItemFocusSessionBinding
    ) : RecyclerView.ViewHolder(binding.root) {
        fun bind(row: DashboardSessionRow) {
            binding.sessionAppIconPlaceholder.text = row.appName.firstOrNull()
                ?.uppercaseChar()
                ?.toString()
                ?: "A"
            binding.sessionAppNameText.text = row.appName
            binding.sessionPackageText.text = row.packageName
            binding.sessionTimeRangeText.text = row.startedAtLocalText
            binding.sessionDurationText.text = row.durationText
            binding.sessionStateText.text = "Active"
        }
    }
}
