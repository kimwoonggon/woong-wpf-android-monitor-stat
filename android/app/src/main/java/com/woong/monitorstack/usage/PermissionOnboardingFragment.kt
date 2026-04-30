package com.woong.monitorstack.usage

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import com.woong.monitorstack.databinding.FragmentPermissionOnboardingBinding

class PermissionOnboardingFragment : Fragment() {
    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        return FragmentPermissionOnboardingBinding.inflate(inflater, container, false).root
    }
}
