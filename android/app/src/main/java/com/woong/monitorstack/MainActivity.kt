package com.woong.monitorstack

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.woong.monitorstack.databinding.ActivityMainBinding
import com.woong.monitorstack.usage.UsageAccessSettingsIntentFactory

class MainActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)
        val usageAccessSettings = UsageAccessSettingsIntentFactory()
        binding.usageAccessSettingsButton.setOnClickListener {
            startActivity(usageAccessSettings.createIntent())
        }
    }
}
