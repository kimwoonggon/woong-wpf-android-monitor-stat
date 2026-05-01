package com.woong.monitorstack.sessions

import android.os.Bundle
import android.view.ViewGroup
import androidx.appcompat.app.AppCompatActivity
import androidx.fragment.app.FragmentContainerView
import com.woong.monitorstack.R

class SessionsActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        setContentView(canonicalFragmentContainer())
        if (savedInstanceState == null) {
            supportFragmentManager
                .beginTransaction()
                .replace(R.id.mainFragmentContainer, SessionsFragment())
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
