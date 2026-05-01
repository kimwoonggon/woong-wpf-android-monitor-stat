package com.woong.monitorstack.sessions

import android.os.Bundle
import android.view.View
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.databinding.ActivitySessionsBinding

class SessionsActivity : AppCompatActivity() {
    private lateinit var binding: ActivitySessionsBinding
    private val adapter = SessionRowAdapter()

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
}
