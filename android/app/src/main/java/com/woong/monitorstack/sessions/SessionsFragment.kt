package com.woong.monitorstack.sessions

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import com.woong.monitorstack.databinding.FragmentSessionsBinding

class SessionsFragment : Fragment() {
    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        return FragmentSessionsBinding.inflate(inflater, container, false).root
    }
}
