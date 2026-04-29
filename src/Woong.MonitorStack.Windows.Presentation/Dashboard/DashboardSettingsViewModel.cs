using CommunityToolkit.Mvvm.ComponentModel;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed partial class DashboardSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isCollectionVisible = true;

    [ObservableProperty]
    private bool _isWindowTitleVisible;

    [ObservableProperty]
    private bool _isPageTitleCaptureEnabled;

    [ObservableProperty]
    private bool _isFullUrlCaptureEnabled;

    [ObservableProperty]
    private bool _isDomainOnlyBrowserStorageEnabled = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SyncModeLabel))]
    private bool _isSyncEnabled;

    [ObservableProperty]
    private string _syncEndpointText = "No sync endpoint configured";

    [ObservableProperty]
    private bool _canClearLocalData;

    [ObservableProperty]
    private bool _hasSyncFailure;

    [ObservableProperty]
    private string _syncStatusLabel = "Sync is off. Data stays on this Windows device.";

    public string SyncModeLabel => IsSyncEnabled ? "Sync enabled" : "Local only";

    public void ReportSyncFailure(string errorMessage)
    {
        HasSyncFailure = true;
        SyncStatusLabel = $"Sync failed: {errorMessage}";
    }

    partial void OnIsSyncEnabledChanged(bool value)
    {
        if (!HasSyncFailure)
        {
            SyncStatusLabel = value
                ? "Sync is enabled. Upload failures will stay retryable."
                : "Sync is off. Data stays on this Windows device.";
        }
    }
}
