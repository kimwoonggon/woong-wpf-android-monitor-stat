package com.woong.monitorstack.summary

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.github.mikephil.charting.data.Entry
import com.github.mikephil.charting.data.LineData
import com.github.mikephil.charting.data.LineDataSet
import com.woong.monitorstack.R
import com.woong.monitorstack.dashboard.DashboardChartConfigurator
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.databinding.FragmentReportBinding
import com.woong.monitorstack.databinding.ItemAppUsageBinding

class ReportFragment : Fragment() {
    private lateinit var binding: FragmentReportBinding
    private lateinit var repository: RoomReportRepository
    private val chartConfigurator = DashboardChartConfigurator()
    private val topAppsAdapter = TopAppsAdapter()

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        binding = FragmentReportBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        val database = MonitorDatabase.getInstance(requireContext())
        repository = RoomReportRepository(database.focusSessionDao())

        binding.reportTopAppsRecyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.reportTopAppsRecyclerView.adapter = topAppsAdapter
        chartConfigurator.configureDailyTrendChart(binding.sevenDayTrendChart, emptyList())
        binding.reportSevenDayButton.setOnClickListener {
            loadReport(ReportPeriod.Last7Days)
        }
        binding.reportThirtyDayButton.setOnClickListener {
            loadReport(ReportPeriod.Last30Days)
        }
        binding.reportNinetyDayButton.setOnClickListener {
            loadReport(ReportPeriod.Last90Days)
        }

        loadReport(ReportPeriod.Last7Days)
    }

    private fun loadReport(period: ReportPeriod) {
        Thread {
            val snapshot = repository.load(period)
            activity?.runOnUiThread {
                if (isAdded) {
                    renderReport(snapshot)
                }
            }
        }.start()
    }

    private fun renderReport(snapshot: ReportSnapshot) {
        val topApp = snapshot.topAppName ?: "No app"

        binding.reportTotalFocusCard.summaryTitleText.text = "Active Focus"
        binding.reportTotalFocusCard.summaryValueText.text = formatDuration(snapshot.totalActiveMs)
        binding.reportTotalFocusCard.summarySubtitleText.text = "${snapshot.dayCount} days"

        binding.reportAverageCard.summaryTitleText.text = "Daily Avg"
        binding.reportAverageCard.summaryValueText.text = formatDuration(
            snapshot.totalActiveMs / snapshot.dayCount
        )
        binding.reportAverageCard.summarySubtitleText.text = "Average focus time"

        binding.reportTopAppCard.summaryTitleText.text = "Top App"
        binding.reportTopAppCard.summaryValueText.text = topApp
        binding.reportTopAppCard.summarySubtitleText.text = "Most used app"
        binding.reportDateRangeText.text = snapshot.dateRangeText

        topAppsAdapter.submit(snapshot.topApps)
        renderTrendChart(snapshot.dailyActivity)
    }

    private fun renderTrendChart(dailyActivity: List<ReportDailyActivity>) {
        val labels = dailyActivity.map { bucket -> bucket.localDate.toShortDateLabel() }
        val entries = dailyActivity.mapIndexed { index, bucket ->
            Entry(index.toFloat(), bucket.durationMs / 60_000f)
        }

        chartConfigurator.configureDailyTrendChart(binding.sevenDayTrendChart, labels)

        if (entries.isEmpty()) {
            binding.sevenDayTrendChart.clear()
        } else {
            binding.sevenDayTrendChart.data = LineData(
                LineDataSet(entries, getString(R.string.daily_usage_trend))
            )
        }

        binding.sevenDayTrendChart.invalidate()
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

    private fun String.toShortDateLabel(): String {
        return if (length >= 10) {
            substring(5, 10).replace("-", "/")
        } else {
            this
        }
    }

    private class TopAppsAdapter : RecyclerView.Adapter<TopAppViewHolder>() {
        private val rows = mutableListOf<ReportTopApp>()

        fun submit(items: List<ReportTopApp>) {
            rows.clear()
            rows.addAll(items)
            notifyDataSetChanged()
        }

        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): TopAppViewHolder {
            val binding = ItemAppUsageBinding.inflate(
                LayoutInflater.from(parent.context),
                parent,
                false
            )

            return TopAppViewHolder(binding)
        }

        override fun onBindViewHolder(holder: TopAppViewHolder, position: Int) {
            holder.bind(rows[position])
        }

        override fun getItemCount(): Int = rows.size
    }

    private class TopAppViewHolder(
        private val binding: ItemAppUsageBinding
    ) : RecyclerView.ViewHolder(binding.root) {
        fun bind(row: ReportTopApp) {
            binding.appIconPlaceholder.text = row.appName.firstOrNull()
                ?.uppercaseChar()
                ?.toString()
                ?: "A"
            binding.appNameText.text = row.appName
            binding.packageNameText.text = "Focus time"
            binding.appDurationText.text = formatDuration(row.durationMs)
            binding.appDetailText.text = ""
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
    }
}
