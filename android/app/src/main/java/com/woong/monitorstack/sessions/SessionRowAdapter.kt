package com.woong.monitorstack.sessions

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.woong.monitorstack.databinding.ItemFocusSessionBinding

class SessionRowAdapter(
    private val onRowClicked: ((SessionRow) -> Unit)? = null
) : RecyclerView.Adapter<SessionRowViewHolder>() {
    private var rows: List<SessionRow> = emptyList()

    fun submitRows(rows: List<SessionRow>) {
        this.rows = rows
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): SessionRowViewHolder {
        val binding = ItemFocusSessionBinding.inflate(
            LayoutInflater.from(parent.context),
            parent,
            false
        )

        return SessionRowViewHolder(binding)
    }

    override fun onBindViewHolder(holder: SessionRowViewHolder, position: Int) {
        holder.bind(rows[position], onRowClicked)
    }

    override fun getItemCount(): Int = rows.size
}

class SessionRowViewHolder(
    private val binding: ItemFocusSessionBinding
) : RecyclerView.ViewHolder(binding.root) {
    fun bind(row: SessionRow, onRowClicked: ((SessionRow) -> Unit)? = null) {
        binding.sessionAppIconPlaceholder.text = row.appName.firstOrNull()
            ?.uppercaseChar()
            ?.toString()
            ?: "A"
        binding.sessionAppNameText.text = row.appName
        binding.sessionPackageText.text = row.packageName
        binding.sessionTimeRangeText.text = row.timeRangeText
        binding.sessionDurationText.text = row.durationText
        binding.sessionStateText.text = row.stateText
        binding.root.setOnClickListener {
            onRowClicked?.invoke(row)
        }
        binding.root.isClickable = onRowClicked != null
    }
}
