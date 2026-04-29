using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.Presentation.Tests.Dashboard;

public sealed class DashboardSettingsViewModelTests
{
    [Fact]
    public void Constructor_DefaultsToVisibleCollectionAndSyncOptOut()
    {
        var viewModel = new DashboardSettingsViewModel();

        Assert.True(viewModel.IsCollectionVisible);
        Assert.False(viewModel.IsPageTitleCaptureEnabled);
        Assert.False(viewModel.IsFullUrlCaptureEnabled);
        Assert.True(viewModel.IsDomainOnlyBrowserStorageEnabled);
        Assert.False(viewModel.IsSyncEnabled);
        Assert.Equal("Local only", viewModel.SyncModeLabel);
        Assert.Equal("No sync endpoint configured", viewModel.SyncEndpointText);
        Assert.False(viewModel.CanClearLocalData);
    }

    [Fact]
    public void ReportSyncFailure_ShowsVisibleRetryableStatus()
    {
        var viewModel = new DashboardSettingsViewModel();
        viewModel.IsSyncEnabled = true;

        viewModel.ReportSyncFailure("server unavailable");

        Assert.True(viewModel.HasSyncFailure);
        Assert.Equal("Sync failed: server unavailable", viewModel.SyncStatusLabel);
    }
}
