package com.woong.monitorstack.sessions

import android.os.Bundle
import android.view.ViewGroup
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.woong.monitorstack.databinding.ActivitySessionsBinding

class SessionsActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val binding = ActivitySessionsBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.sessionsList.layoutManager = LinearLayoutManager(this)
        binding.sessionsList.adapter = EmptySessionsAdapter()
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
