package com.woong.monitorstack.summary

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.woong.monitorstack.dashboard.DashboardPeriod
import com.woong.monitorstack.dashboard.DashboardUsageSlice
import com.woong.monitorstack.dashboard.DashboardViewModel
import com.woong.monitorstack.dashboard.RoomDashboardRepository
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.databinding.FragmentReportBinding
import com.woong.monitorstack.databinding.ItemAppUsageBinding

class ReportFragment : Fragment() {
    private lateinit var binding: FragmentReportBinding
    private lateinit var viewModel: DashboardViewModel
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
        viewModel = DashboardViewModel(
            RoomDashboardRepository(
                dao = database.focusSessionDao(),
                locationDao = database.locationContextSnapshotDao()
            )
        )

        binding.reportTopAppsRecyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.reportTopAppsRecyclerView.adapter = topAppsAdapter
        loadSevenDayReport()
    }

    private fun loadSevenDayReport() {
        Thread {
            viewModel.selectPeriod(DashboardPeriod.Recent7Days)
            activity?.runOnUiThread {
                if (isAdded) {
                    renderReport()
                }
            }
        }.start()
    }

    private fun renderReport() {
        val state = viewModel.state
        val appUsage = state.chartData.appUsage
        val topApp = state.topAppName ?: "No app"

        binding.reportTotalFocusCard.summaryTitleText.text = "Active Focus"
        binding.reportTotalFocusCard.summaryValueText.text = formatDuration(state.totalActiveMs)
        binding.reportTotalFocusCard.summarySubtitleText.text = "Recent 7 days"

        binding.reportAverageCard.summaryTitleText.text = "Daily Avg"
        binding.reportAverageCard.summaryValueText.text = formatDuration(state.totalActiveMs / 7)
        binding.reportAverageCard.summarySubtitleText.text = "Average focus time"

        binding.reportTopAppCard.summaryTitleText.text = "Top App"
        binding.reportTopAppCard.summaryValueText.text = topApp
        binding.reportTopAppCard.summarySubtitleText.text = "Most used app"

        topAppsAdapter.submit(appUsage)
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

    private class TopAppsAdapter : RecyclerView.Adapter<TopAppViewHolder>() {
        private val rows = mutableListOf<DashboardUsageSlice>()

        fun submit(items: List<DashboardUsageSlice>) {
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
        fun bind(row: DashboardUsageSlice) {
            binding.appIconPlaceholder.text = row.label.firstOrNull()
                ?.uppercaseChar()
                ?.toString()
                ?: "A"
            binding.appNameText.text = row.label
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
