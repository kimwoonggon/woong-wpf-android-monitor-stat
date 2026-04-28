using CommunityToolkit.Mvvm.ComponentModel;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed partial class DashboardSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isCollectionVisible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SyncModeLabel))]
    private bool _isSyncEnabled;

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
