using CommunityToolkit.Mvvm.ComponentModel;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed partial class DashboardSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isCollectionVisible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SyncModeLabel))]
    private bool _isSyncEnabled;

    public string SyncModeLabel => IsSyncEnabled ? "Sync enabled" : "Local only";
}
