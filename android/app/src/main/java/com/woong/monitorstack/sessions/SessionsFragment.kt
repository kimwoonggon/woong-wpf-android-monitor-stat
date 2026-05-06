package com.woong.monitorstack.sessions

import android.app.DatePickerDialog
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.EditText
import androidx.fragment.app.Fragment
import androidx.recyclerview.widget.LinearLayoutManager
import com.google.android.material.button.MaterialButton
import com.woong.monitorstack.R
import com.woong.monitorstack.data.local.MonitorDatabase
import com.woong.monitorstack.databinding.FragmentSessionsBinding
import com.woong.monitorstack.ui.PeriodButtonStyler
import java.time.LocalDate
import java.time.format.DateTimeParseException

class SessionsFragment : Fragment() {
    private lateinit var binding: FragmentSessionsBinding
    private val adapter = SessionRowAdapter { row -> openAppDetail(row.packageName) }
    private var selectedPeriod: SessionsPeriod = SessionsPeriod.Today

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
        binding.sessionsTodayButton.setOnClickListener {
            loadSelectedSessions(binding.sessionsTodayButton, SessionsPeriod.Today)
        }
        binding.sessionsOneHourButton.setOnClickListener {
            loadSelectedSessions(binding.sessionsOneHourButton, SessionsPeriod.LastHour)
        }
        binding.sessionsSixHourButton.setOnClickListener {
            loadSelectedSessions(binding.sessionsSixHourButton, SessionsPeriod.LastSixHours)
        }
        binding.sessionsTwentyFourHourButton.setOnClickListener {
            loadSelectedSessions(
                binding.sessionsTwentyFourHourButton,
                SessionsPeriod.LastTwentyFourHours
            )
        }
        binding.sessionsSevenDayButton.setOnClickListener {
            loadSelectedSessions(binding.sessionsSevenDayButton, SessionsPeriod.LastSevenDays)
        }
        binding.sessionsFilterButton.setOnClickListener {
            binding.sessionsCustomRangePanel.visibility = View.VISIBLE
            binding.sessionsCustomRangeErrorText.visibility = View.GONE
            ensureCustomDateDefaults()
            selectPeriodButton(binding.sessionsFilterButton)
        }
        binding.sessionsCustomStartDateEditText.setOnClickListener {
            showDatePicker(binding.sessionsCustomStartDateEditText)
        }
        binding.sessionsCustomEndDateEditText.setOnClickListener {
            showDatePicker(binding.sessionsCustomEndDateEditText)
        }
        binding.sessionsApplyCustomRangeButton.setOnClickListener {
            applyCustomRange()
        }
        loadSelectedSessions(buttonForPeriod(selectedPeriod), selectedPeriod)
    }

    private fun loadSelectedSessions(selectedButton: MaterialButton, period: SessionsPeriod) {
        binding.sessionsCustomRangePanel.visibility = if (period is SessionsPeriod.Custom) {
            View.VISIBLE
        } else {
            View.GONE
        }
        selectedPeriod = period
        selectPeriodButton(selectedButton)
        loadSessions(period)
    }

    private fun buttonForPeriod(period: SessionsPeriod): MaterialButton {
        return when (period) {
            SessionsPeriod.Today -> binding.sessionsTodayButton
            SessionsPeriod.LastHour -> binding.sessionsOneHourButton
            SessionsPeriod.LastSixHours -> binding.sessionsSixHourButton
            SessionsPeriod.LastTwentyFourHours -> binding.sessionsTwentyFourHourButton
            SessionsPeriod.LastSevenDays -> binding.sessionsSevenDayButton
            is SessionsPeriod.Custom -> binding.sessionsFilterButton
        }
    }

    private fun selectPeriodButton(selectedButton: MaterialButton) {
        PeriodButtonStyler.select(
            selectedButton = selectedButton,
            buttons = listOf(
                binding.sessionsTodayButton,
                binding.sessionsOneHourButton,
                binding.sessionsSixHourButton,
                binding.sessionsTwentyFourHourButton,
                binding.sessionsSevenDayButton,
                binding.sessionsFilterButton
            )
        )
    }

    private fun applyCustomRange() {
        try {
            val from = LocalDate.parse(binding.sessionsCustomStartDateEditText.text.toString().trim())
            val to = LocalDate.parse(binding.sessionsCustomEndDateEditText.text.toString().trim())
            binding.sessionsCustomRangeErrorText.visibility = View.GONE
            loadSelectedSessions(
                binding.sessionsFilterButton,
                SessionsPeriod.Custom(from = from, to = to)
            )
        } catch (_: DateTimeParseException) {
            binding.sessionsCustomRangeErrorText.visibility = View.VISIBLE
        } catch (_: IllegalArgumentException) {
            binding.sessionsCustomRangeErrorText.visibility = View.VISIBLE
        }
    }

    private fun ensureCustomDateDefaults() {
        val today = LocalDate.now()
        if (binding.sessionsCustomStartDateEditText.text.isNullOrBlank()) {
            binding.sessionsCustomStartDateEditText.setText(today.toString())
        }
        if (binding.sessionsCustomEndDateEditText.text.isNullOrBlank()) {
            binding.sessionsCustomEndDateEditText.setText(today.toString())
        }
    }

    private fun showDatePicker(target: EditText) {
        val initialDate = runCatching {
            LocalDate.parse(target.text.toString())
        }.getOrDefault(LocalDate.now())

        DatePickerDialog(
            requireContext(),
            { _, year, month, dayOfMonth ->
                target.setText(LocalDate.of(year, month + 1, dayOfMonth).toString())
            },
            initialDate.year,
            initialDate.monthValue - 1,
            initialDate.dayOfMonth
        ).show()
    }

    private fun loadSessions(period: SessionsPeriod) {
        Thread {
            val repository = RoomSessionsRepository(
                MonitorDatabase.getInstance(requireContext().applicationContext).focusSessionDao()
            )
            val rows = repository.loadSessions(period)
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
}
