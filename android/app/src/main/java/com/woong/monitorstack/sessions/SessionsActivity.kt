package com.woong.monitorstack.sessions

import android.os.Bundle
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.databinding.ActivitySessionsBinding

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
            return SessionViewHolder(TextView(parent.context))
        }

        override fun onBindViewHolder(holder: SessionViewHolder, position: Int) {
            holder.bind(rows[position])
        }

        override fun getItemCount(): Int = rows.size
    }

    private class SessionViewHolder(
        private val textView: TextView
    ) : RecyclerView.ViewHolder(textView) {
        fun bind(row: SessionRow) {
            textView.text = "${row.packageName}\n${row.durationText}"
        }
    }
}
