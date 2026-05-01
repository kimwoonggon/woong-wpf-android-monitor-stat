package com.woong.monitorstack.dashboard

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.woong.monitorstack.R
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.databinding.FragmentDashboardBinding
import com.woong.monitorstack.databinding.ItemFocusSessionBinding
import com.woong.monitorstack.display.AppDisplayNameFormatter

class DashboardFragment : Fragment() {
    private lateinit var binding: FragmentDashboardBinding
    private lateinit var viewModel: DashboardViewModel
    private val sessionAdapter = SessionsAdapter()

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
                locationDao = database.locationContextSnapshotDao()
            )
        )

        binding.recentSessionsRecyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.recentSessionsRecyclerView.adapter = sessionAdapter
        binding.todayFilterButton.setOnClickListener { loadPeriod(DashboardPeriod.Today) }
        loadPeriod(DashboardPeriod.Today)
    }

    fun refreshFromDatabase() {
        loadPeriod(DashboardPeriod.Today)
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
        val latestSession = state.recentSessions.firstOrNull()
        val currentForegroundPackageName = arguments?.getString(ArgumentCurrentForegroundPackageName)
        val currentAppName = currentForegroundPackageName
            ?.let(AppDisplayNameFormatter::format)
            ?: latestSession?.appName
            ?: topApp
        val currentPackageName = currentForegroundPackageName
            ?: latestSession?.packageName
            ?: getString(R.string.no_package)

        binding.currentFocusAppIconPlaceholder.text = currentAppName.firstOrNull()
            ?.uppercaseChar()
            ?.toString()
            ?: "A"
        binding.currentAppText.text = currentAppName
        binding.currentPackageText.text = currentPackageName
        binding.currentSessionDurationText.text = formatClockDuration(state.totalActiveMs)
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
        sessionAdapter.submit(state.recentSessions)
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

    companion object {
        private const val ArgumentCurrentForegroundPackageName = "current_foreground_package_name"

        fun newInstance(currentForegroundPackageName: String): DashboardFragment {
            return DashboardFragment().apply {
                arguments = Bundle().apply {
                    putString(ArgumentCurrentForegroundPackageName, currentForegroundPackageName)
                }
            }
        }
    }
}
