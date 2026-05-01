package com.woong.monitorstack.usage

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import com.woong.monitorstack.R
import com.woong.monitorstack.databinding.FragmentPermissionOnboardingBinding

class PermissionOnboardingFragment : Fragment() {
    private var binding: FragmentPermissionOnboardingBinding? = null

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        val fragmentBinding = FragmentPermissionOnboardingBinding.inflate(inflater, container, false)
        binding = fragmentBinding
        return fragmentBinding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        val fragmentBinding = requireNotNull(binding)
        val usageAccessSettings = UsageAccessSettingsIntentFactory()
        fragmentBinding.permissionRuntimeStatusText.text = arguments?.getString(
            ArgumentCollectionStatusText
        ) ?: getString(R.string.usage_collection_paused_until_permission)
        fragmentBinding.openUsageAccessSettingsButton.setOnClickListener {
            startActivity(usageAccessSettings.createIntent())
        }
    }

    override fun onDestroyView() {
        binding = null
        super.onDestroyView()
    }

    companion object {
        private const val ArgumentCollectionStatusText = "collection_status_text"

        fun newInstance(collectionStatusText: String): PermissionOnboardingFragment {
            return PermissionOnboardingFragment().apply {
                arguments = Bundle().apply {
                    putString(ArgumentCollectionStatusText, collectionStatusText)
                }
            }
        }
    }
}
