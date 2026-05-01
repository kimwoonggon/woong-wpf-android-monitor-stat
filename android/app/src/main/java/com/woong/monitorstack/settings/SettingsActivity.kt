package com.woong.monitorstack.settings

import android.os.Bundle
import android.view.ViewGroup
import androidx.appcompat.app.AppCompatActivity
import androidx.fragment.app.FragmentContainerView
import com.woong.monitorstack.R

class SettingsActivity : AppCompatActivity() {
    companion object {
        const val EXTRA_SYNC_FAILED_COUNT = "com.woong.monitorstack.settings.SYNC_FAILED_COUNT"
        const val EXTRA_SYNC_FAILURE_MESSAGE = "com.woong.monitorstack.settings.SYNC_FAILURE_MESSAGE"
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        setContentView(canonicalFragmentContainer())
        if (savedInstanceState == null) {
            supportFragmentManager
                .beginTransaction()
                .replace(R.id.mainFragmentContainer, SettingsFragment())
                .commitNow()
        }
    }

    private fun canonicalFragmentContainer(): FragmentContainerView {
        return FragmentContainerView(this).apply {
            id = R.id.mainFragmentContainer
            layoutParams = ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.MATCH_PARENT
            )
        }
    }
}
