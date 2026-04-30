package com.woong.monitorstack.sessions

import android.os.Bundle
import android.view.View
import android.view.ViewGroup
import android.view.LayoutInflater
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.databinding.ActivitySessionsBinding
import com.woong.monitorstack.databinding.ItemFocusSessionBinding

class SessionsActivity : AppCompatActivity() {
    private lateinit var binding: ActivitySessionsBinding
    private val adapter = SessionsAdapter()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivitySessionsBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.sessionsList.layoutManager = LinearLayoutManager(this)
        binding.sessionsList.adapter = adapter
        loadRecentSessions()
    }

    private fun loadRecentSessions() {
        Thread {
            val repository = RoomSessionsRepository(
                MonitorDatabase.getInstance(applicationContext).focusSessionDao()
            )
            val rows = repository.loadRecentSessions()
            runOnUiThread {
                adapter.submitRows(rows)
                binding.emptySessionsText.visibility = if (rows.isEmpty()) {
                    View.VISIBLE
                } else {
                    View.GONE
                }
            }
        }.start()
    }

    private class SessionsAdapter : RecyclerView.Adapter<SessionViewHolder>() {
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
            holder.bind(rows[position])
        }

        override fun getItemCount(): Int = rows.size
    }

    private class SessionViewHolder(
        private val binding: ItemFocusSessionBinding
    ) : RecyclerView.ViewHolder(binding.root) {
        fun bind(row: SessionRow) {
            binding.sessionAppIconPlaceholder.text = row.packageName.firstOrNull()
                ?.uppercaseChar()
                ?.toString()
                ?: "A"
            binding.sessionPackageText.text = row.packageName
            binding.sessionTimeRangeText.text = row.timeRangeText
            binding.sessionDurationText.text = row.durationText
            binding.sessionStateText.text = row.stateText
        }
    }
}
