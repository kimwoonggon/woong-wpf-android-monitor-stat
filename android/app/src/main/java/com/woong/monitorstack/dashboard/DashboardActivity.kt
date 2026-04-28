package com.woong.monitorstack.dashboard

import android.os.Bundle
import android.view.ViewGroup
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.woong.monitorstack.databinding.ActivityDashboardBinding
import com.woong.monitorstack.usage.UsageAccessSettingsIntentFactory

class DashboardActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val binding = ActivityDashboardBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.recentSessionsList.layoutManager = LinearLayoutManager(this)
        binding.recentSessionsList.adapter = EmptySessionsAdapter()

        val usageAccessSettings = UsageAccessSettingsIntentFactory()
        binding.usageAccessSettingsButton.setOnClickListener {
            startActivity(usageAccessSettings.createIntent())
        }
    }

    private class EmptySessionsAdapter : RecyclerView.Adapter<EmptySessionViewHolder>() {
        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): EmptySessionViewHolder {
            return EmptySessionViewHolder(TextView(parent.context))
        }

        override fun onBindViewHolder(holder: EmptySessionViewHolder, position: Int) = Unit

        override fun getItemCount(): Int = 0
    }

    private class EmptySessionViewHolder(view: TextView) : RecyclerView.ViewHolder(view)
}
