package com.woong.monitorstack

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.woong.monitorstack.dashboard.DashboardActivity

class MainActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        startActivity(DashboardActivity.createIntent(this))
        finish()
    }
}
