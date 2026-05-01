package com.woong.monitorstack.sessions

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.recyclerview.widget.LinearLayoutManager
import com.github.mikephil.charting.data.BarData
import com.github.mikephil.charting.data.BarEntry
import com.woong.monitorstack.R
import com.woong.monitorstack.dashboard.DashboardChartConfigurator
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.databinding.FragmentAppDetailBinding

class AppDetailFragment : Fragment() {
    private lateinit var binding: FragmentAppDetailBinding
    private val chartConfigurator = DashboardChartConfigurator()
    private val adapter = SessionRowAdapter()

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
