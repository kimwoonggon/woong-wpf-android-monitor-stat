package com.woong.monitorstack.sessions

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.woong.monitorstack.R
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.databinding.FragmentSessionsBinding
import com.woong.monitorstack.databinding.ItemFocusSessionBinding

class SessionsFragment : Fragment() {
    private lateinit var binding: FragmentSessionsBinding
    private val adapter = SessionsAdapter { row -> openAppDetail(row.packageName) }

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        binding = FragmentSessionsBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        binding.sessionsRecyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.sessionsRecyclerView.adapter = adapter
        loadRecentSessions()
    }

    private fun loadRecentSessions() {
        Thread {
            val repository = RoomSessionsRepository(
                MonitorDatabase.getInstance(requireContext().applicationContext).focusSessionDao()
            )
            val rows = repository.loadRecentSessions()
            activity?.runOnUiThread {
                if (isAdded) {
                    adapter.submitRows(rows)
                    binding.sessionsTotalCountText.text = getString(
                        R.string.sessions_total_count,
                        rows.size
                    )
                    binding.emptySessionsText.visibility = if (rows.isEmpty()) {
                        View.VISIBLE
                    } else {
                        View.GONE
                    }
                }
            }
        }.start()
    }

    private fun openAppDetail(packageName: String) {
        parentFragmentManager
            .beginTransaction()
            .replace(R.id.mainFragmentContainer, AppDetailFragment.newInstance(packageName))
            .addToBackStack("app-detail")
            .commit()
    }

    private class SessionsAdapter(
        private val onRowClicked: (SessionRow) -> Unit
    ) : RecyclerView.Adapter<SessionViewHolder>() {
        private var rows: List<SessionRow> = emptyList()

        fun submitRows(rows: List<SessionRow>) {
            this.rows = rows
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
            holder.bind(rows[position], onRowClicked)
        }

        override fun getItemCount(): Int = rows.size
    }

    private class SessionViewHolder(
        private val binding: ItemFocusSessionBinding
    ) : RecyclerView.ViewHolder(binding.root) {
        fun bind(row: SessionRow, onRowClicked: (SessionRow) -> Unit) {
            binding.sessionAppIconPlaceholder.text = row.appName.firstOrNull()
                ?.uppercaseChar()
                ?.toString()
                ?: "A"
            binding.sessionAppNameText.text = row.appName
            binding.sessionPackageText.text = row.packageName
            binding.sessionTimeRangeText.text = row.timeRangeText
            binding.sessionDurationText.text = row.durationText
            binding.sessionStateText.text = row.stateText
            binding.root.setOnClickListener { onRowClicked(row) }
        }
    }
}
