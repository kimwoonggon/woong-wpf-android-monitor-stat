package com.woong.monitorstack.sessions

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.github.mikephil.charting.data.BarData
import com.github.mikephil.charting.data.BarEntry
import com.woong.monitorstack.R
import com.woong.monitorstack.dashboard.DashboardChartConfigurator
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.databinding.FragmentAppDetailBinding
import com.woong.monitorstack.databinding.ItemFocusSessionBinding

class AppDetailFragment : Fragment() {
    private lateinit var binding: FragmentAppDetailBinding
    private val chartConfigurator = DashboardChartConfigurator()
    private val adapter = DetailSessionsAdapter()

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        binding = FragmentAppDetailBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        binding.appDetailBackButton.setOnClickListener {
            parentFragmentManager.popBackStack()
        }
        binding.appDetailSessionsRecyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.appDetailSessionsRecyclerView.adapter = adapter
        configureChart()
        loadAppDetail()
    }

    private fun loadAppDetail() {
        val packageName = requireArguments().getString(ArgumentPackageName).orEmpty()
        Thread {
            val repository = RoomSessionsRepository(
                MonitorDatabase.getInstance(requireContext().applicationContext).focusSessionDao()
            )
            val detail = repository.loadAppDetail(packageName)
            activity?.runOnUiThread {
                if (isAdded) {
                    render(detail)
                }
            }
        }.start()
    }

    private fun render(detail: AppDetailState) {
        binding.detailAppNameText.text = detail.appName
        binding.detailPackageNameText.text = detail.packageName
        binding.appDetailIconPlaceholder.text = detail.appName.firstOrNull()
            ?.uppercaseChar()
            ?.toString()
            ?: "A"

        binding.appTotalDurationCard.summaryTitleText.text = getString(R.string.total_usage_time)
        binding.appTotalDurationCard.summaryValueText.text = detail.totalDurationText
        binding.appTotalDurationCard.summarySubtitleText.text = getString(R.string.selected_app_usage)

        binding.appSessionCountCard.summaryTitleText.text = getString(R.string.session_count)
        binding.appSessionCountCard.summaryValueText.text = detail.sessionCountText
        binding.appSessionCountCard.summarySubtitleText.text = getString(R.string.selected_app_sessions)

        adapter.submitRows(detail.sessions)
        renderHourlyChart(detail)
    }

    private fun configureChart() {
        chartConfigurator.configureHourlyBarChart(binding.appHourlyChart)
        binding.appHourlyChart.invalidate()
    }

    private fun renderHourlyChart(detail: AppDetailState) {
        val entries = detail.hourlyUsage.map { bucket ->
            BarEntry(bucket.hourOfDay.toFloat(), bucket.durationMs / 60_000f)
        }

        if (entries.isEmpty()) {
            binding.appHourlyChart.clear()
        } else {
            binding.appHourlyChart.data = BarData(
                chartConfigurator.createFocusBarDataSet(
                    context = requireContext(),
                    entries = entries,
                    label = getString(R.string.hourly_usage_today)
                )
            )
        }

        binding.appHourlyChart.invalidate()
    }

    private class DetailSessionsAdapter : RecyclerView.Adapter<DetailSessionViewHolder>() {
        private var rows: List<SessionRow> = emptyList()

        fun submitRows(rows: List<SessionRow>) {
            this.rows = rows
            notifyDataSetChanged()
        }

        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): DetailSessionViewHolder {
            val binding = ItemFocusSessionBinding.inflate(
                LayoutInflater.from(parent.context),
                parent,
                false
            )

            return DetailSessionViewHolder(binding)
        }

        override fun onBindViewHolder(holder: DetailSessionViewHolder, position: Int) {
            holder.bind(rows[position])
        }

        override fun getItemCount(): Int = rows.size
    }

    private class DetailSessionViewHolder(
        private val binding: ItemFocusSessionBinding
    ) : RecyclerView.ViewHolder(binding.root) {
        fun bind(row: SessionRow) {
            binding.sessionAppIconPlaceholder.text = row.appName.firstOrNull()
                ?.uppercaseChar()
                ?.toString()
                ?: "A"
            binding.sessionAppNameText.text = row.appName
            binding.sessionPackageText.text = row.packageName
            binding.sessionTimeRangeText.text = row.timeRangeText
            binding.sessionDurationText.text = row.durationText
            binding.sessionStateText.text = row.stateText
        }
    }

    companion object {
        private const val ArgumentPackageName = "package_name"

        fun newInstance(packageName: String): AppDetailFragment {
            return AppDetailFragment().apply {
                arguments = Bundle().apply {
                    putString(ArgumentPackageName, packageName)
                }
            }
        }
    }
}
