namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed class NoopDashboardTrackingCoordinator : IDashboardTrackingCoordinator
{
    public DashboardTrackingSnapshot StartTracking()
        => DashboardTrackingSnapshot.Empty;

    public DashboardTrackingSnapshot StopTracking()
        => DashboardTrackingSnapshot.Empty;

    public DashboardTrackingSnapshot PollOnce()
        => DashboardTrackingSnapshot.Empty;

    public DashboardSyncResult SyncNow(bool syncEnabled)
        => syncEnabled
            ? new DashboardSyncResult("Sync requested. Waiting for upload worker.")
            : new DashboardSyncResult("Sync skipped. Enable sync to upload.");
}
