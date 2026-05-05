package com.woong.monitorstack.dashboard

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.github.mikephil.charting.data.BarData
import com.github.mikephil.charting.data.BarEntry
import com.google.android.material.button.MaterialButton
import com.woong.monitorstack.R
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.databinding.FragmentDashboardBinding
import com.woong.monitorstack.display.AppDisplayNameFormatter
import com.woong.monitorstack.databinding.ItemAppUsageBinding
import com.woong.monitorstack.databinding.ItemFocusSessionBinding
import com.woong.monitorstack.ui.PeriodButtonStyler

class DashboardFragment : Fragment() {
    private lateinit var binding: FragmentDashboardBinding
    private lateinit var viewModel: DashboardViewModel
    private val sessionAdapter = SessionsAdapter()
    private val topAppsAdapter = TopAppsAdapter()
    private val chartConfigurator = DashboardChartConfigurator()
    private lateinit var currentFocusResolver: DashboardCurrentFocusResolver
    private lateinit var locationMapController: DashboardLocationMapController

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        binding = FragmentDashboardBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        val database = MonitorDatabase.getInstance(requireContext())
        viewModel = DashboardViewModel(
            RoomDashboardRepository(
                dao = database.focusSessionDao(),
                locationDao = database.locationContextSnapshotDao(),
                locationVisitDao = database.locationVisitDao()
            )
        )
        currentFocusResolver = DashboardCurrentFocusResolver(requireContext().packageName)
        locationMapController = DashboardLocationMapController(
            context = requireContext(),
            googleMapContainer = binding.googleLocationMapContainer,
            localPreview = binding.locationMiniMapView,
            providerStatusText = binding.locationMapProviderStatusText
        )
        locationMapController.onCreate(savedInstanceState)

        binding.recentSessionsRecyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.recentSessionsRecyclerView.adapter = sessionAdapter
        binding.topAppsRecyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.topAppsRecyclerView.adapter = topAppsAdapter
        chartConfigurator.configureHourlyBarChart(binding.hourlyFocusChart)
        binding.todayFilterButton.setOnClickListener {
            loadSelectedPeriod(binding.todayFilterButton, DashboardPeriod.Today)
        }
        binding.oneHourFilterButton.setOnClickListener {
            loadSelectedPeriod(binding.oneHourFilterButton, DashboardPeriod.LastHour)
        }
        binding.sixHourFilterButton.setOnClickListener {
            loadSelectedPeriod(binding.sixHourFilterButton, DashboardPeriod.LastSixHours)
        }
        binding.twentyFourHourFilterButton.setOnClickListener {
            loadSelectedPeriod(
                binding.twentyFourHourFilterButton,
                DashboardPeriod.LastTwentyFourHours
            )
        }
        binding.sevenDayFilterButton.setOnClickListener {
            loadSelectedPeriod(binding.sevenDayFilterButton, DashboardPeriod.Recent7Days)
        }
        loadSelectedPeriod(binding.todayFilterButton, DashboardPeriod.Today)
    }

    fun refreshFromDatabase() {
        loadSelectedPeriod(binding.todayFilterButton, DashboardPeriod.Today)
    }

    private fun loadSelectedPeriod(selectedButton: MaterialButton, period: DashboardPeriod) {
        selectPeriodButton(selectedButton)
        loadPeriod(period)
    }

    private fun selectPeriodButton(selectedButton: MaterialButton) {
        PeriodButtonStyler.select(
            selectedButton = selectedButton,
            buttons = listOf(
                binding.todayFilterButton,
                binding.oneHourFilterButton,
                binding.sixHourFilterButton,
                binding.twentyFourHourFilterButton,
                binding.sevenDayFilterButton
            )
        )
    }

    private fun loadPeriod(period: DashboardPeriod) {
        Thread {
            viewModel.selectPeriod(period)
            activity?.runOnUiThread {
                if (isAdded) {
                    render(viewModel.state)
                }
            }
        }.start()
    }

    private fun render(state: DashboardUiState) {
        val topApp = state.topAppName ?: getString(R.string.no_top_app)
        val currentFocusSelection = currentFocusResolver.resolve(state.recentSessions)
        val latestExternalSession = currentFocusSelection.latestExternalSession
        val monitorPackageName = requireContext().packageName
        val latestSession = currentFocusSelection.currentSession
            ?.takeIf { it.packageName == monitorPackageName }
        val currentAppName = AppDisplayNameFormatter.format(monitorPackageName)
        val currentPackageName = monitorPackageName
        val latestExternalAppName = latestExternalSession?.appName ?: getString(R.string.no_top_app)
        val latestExternalPackageName = latestExternalSession?.packageName ?: getString(R.string.no_package)

        binding.currentFocusAppIconPlaceholder.text = currentAppName.firstOrNull()
            ?.uppercaseChar()
            ?.toString()
            ?: "A"
        binding.currentAppText.text = currentAppName
        binding.currentPackageText.text = currentPackageName
        binding.latestCollectedExternalAppText.text = latestExternalAppName
        binding.latestCollectedExternalPackageText.text = latestExternalPackageName
        binding.currentSessionDurationText.text = formatClockDuration(
            latestSession?.durationMs?.takeIf { it > 0L } ?: state.totalActiveMs
        )
        binding.lastCollectedText.text = getString(
            R.string.last_collected_compact_value,
            latestSession?.startedAtLocalText ?: getString(R.string.no_poll_yet)
        )
        binding.lastDbWriteText.text = getString(
            R.string.last_db_write_compact_value,
            latestSession?.startedAtLocalText ?: getString(R.string.no_db_write_yet)
        )
        binding.activeFocusValueText.text = formatDuration(state.totalActiveMs)
        binding.screenOnValueText.text = formatDuration(state.totalActiveMs + state.idleMs)
        binding.idleValueText.text = formatDuration(state.idleMs)
        binding.syncStateValueText.text = "Local"
        binding.locationStatusText.text = state.locationContext.statusText
        binding.locationLatitudeText.text = getString(
            R.string.location_latitude_value,
            state.locationContext.latitudeText
        )
        binding.locationLongitudeText.text = getString(
            R.string.location_longitude_value,
            state.locationContext.longitudeText
        )
        binding.locationAccuracyText.text = getString(
            R.string.location_accuracy_value,
            state.locationContext.accuracyText
        )
        binding.locationCapturedAtText.text = getString(
            R.string.location_captured_at_value,
            state.locationContext.capturedAtLocalText
        )
        binding.locationVisitStatsText.text = getString(
            R.string.location_visit_stats_value,
            state.locationContext.visitStatsText
        )
        binding.locationTopVisitText.text = getString(
            R.string.location_top_visit_value,
            state.locationContext.topVisitText
        )
        locationMapController.render(state.locationContext.mapPoints)
        sessionAdapter.submit(state.recentSessions)
        topAppsAdapter.submit(state.chartData.appUsage.take(3))
        renderHourlyChart(state.chartData.hourlyActivity)
    }

    override fun onStart() {
        super.onStart()
        if (::locationMapController.isInitialized) {
            locationMapController.onStart()
        }
    }

    override fun onResume() {
        super.onResume()
        if (::locationMapController.isInitialized) {
            locationMapController.onResume()
        }
    }

    override fun onPause() {
        if (::locationMapController.isInitialized) {
            locationMapController.onPause()
        }
        super.onPause()
    }

    override fun onStop() {
        if (::locationMapController.isInitialized) {
            locationMapController.onStop()
        }
        super.onStop()
    }

    override fun onDestroyView() {
        if (::locationMapController.isInitialized) {
            locationMapController.onDestroy()
        }
        super.onDestroyView()
    }

    override fun onLowMemory() {
        super.onLowMemory()
        if (::locationMapController.isInitialized) {
            locationMapController.onLowMemory()
        }
    }

    override fun onSaveInstanceState(outState: Bundle) {
        if (::locationMapController.isInitialized) {
            locationMapController.onSaveInstanceState(outState)
        }
        super.onSaveInstanceState(outState)
    }

    private fun renderHourlyChart(hourlyActivity: List<DashboardActivityBucket>) {
        if (hourlyActivity.isEmpty()) {
            binding.hourlyFocusChart.clear()
            binding.hourlyFocusChart.invalidate()
            return
        }

        binding.hourlyFocusChart.data = BarData(
            chartConfigurator.createFocusBarDataSet(
                context = requireContext(),
                entries = hourlyActivity.map { bucket ->
                    BarEntry(bucket.hourOfDay.toFloat(), bucket.durationMs / 60_000f)
                },
                label = getString(R.string.hourly_focus_chart_title)
            )
        )
        binding.hourlyFocusChart.invalidate()
    }

    private fun formatClockDuration(durationMs: Long): String {
        val totalSeconds = durationMs / 1_000
        val hours = totalSeconds / 3_600
        val minutes = (totalSeconds % 3_600) / 60
        val seconds = totalSeconds % 60

        return "%02d:%02d:%02d".format(hours, minutes, seconds)
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

    private class TopAppsAdapter : RecyclerView.Adapter<TopAppViewHolder>() {
        private val apps = mutableListOf<DashboardUsageSlice>()
        private var maxDurationMs: Long = 0L

        fun submit(rows: List<DashboardUsageSlice>) {
            apps.clear()
            apps.addAll(rows)
            maxDurationMs = apps.maxOfOrNull { it.durationMs } ?: 0L
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
            holder.bind(apps[position], maxDurationMs)
        }

        override fun getItemCount(): Int = apps.size
    }

    private class TopAppViewHolder(
        private val binding: ItemAppUsageBinding
    ) : RecyclerView.ViewHolder(binding.root) {
        fun bind(row: DashboardUsageSlice, maxDurationMs: Long) {
            binding.appIconPlaceholder.text = row.label.firstOrNull()
                ?.uppercaseChar()
                ?.toString()
                ?: "A"
            binding.appNameText.text = row.label
            binding.packageNameText.text = ""
            binding.appDurationText.text = formatDuration(row.durationMs)
            binding.appDetailText.text = ""
            bindProportion(row.durationMs, maxDurationMs)
        }

        private fun bindProportion(durationMs: Long, maxDurationMs: Long) {
            if (durationMs <= 0L || maxDurationMs <= 0L) {
                binding.appUsageProportionBar.visibility = View.GONE
                return
            }

            binding.appUsageProportionBar.visibility = View.VISIBLE
            binding.appUsageProportionBar.progress =
                ((durationMs * 100L) / maxDurationMs).toInt().coerceIn(1, 100)
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
