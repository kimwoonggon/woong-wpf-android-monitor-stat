package com.woong.monitorstack

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.fragment.app.Fragment
import com.woong.monitorstack.dashboard.DashboardFragment
import com.woong.monitorstack.databinding.ActivityMainBinding
import com.woong.monitorstack.sessions.SessionsFragment
import com.woong.monitorstack.settings.SettingsFragment
import com.woong.monitorstack.summary.ReportFragment

class MainActivity : AppCompatActivity() {
    private lateinit var binding: ActivityMainBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)
        setSupportActionBar(binding.topAppBar)

        binding.bottomNavigation.setOnItemSelectedListener { item ->
            when (item.itemId) {
                R.id.navDashboard -> {
                    showScreen(DashboardFragment())
                    true
                }
                R.id.navSessions -> {
                    showScreen(SessionsFragment())
                    true
                }
                R.id.navReport -> {
                    showScreen(ReportFragment())
                    true
                }
                R.id.navSettings -> {
                    showScreen(SettingsFragment())
                    true
                }
                else -> false
            }
        }

        if (savedInstanceState == null) {
            binding.bottomNavigation.selectedItemId = R.id.navDashboard
        }
    }

    private fun showScreen(fragment: Fragment) {
        supportFragmentManager
            .beginTransaction()
            .replace(R.id.mainFragmentContainer, fragment)
            .commit()
    }
}
