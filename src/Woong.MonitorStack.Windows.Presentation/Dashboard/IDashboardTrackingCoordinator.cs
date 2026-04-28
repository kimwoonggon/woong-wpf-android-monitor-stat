namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public interface IDashboardTrackingCoordinator
{
    DashboardTrackingSnapshot StartTracking();

    DashboardTrackingSnapshot StopTracking();

    DashboardTrackingSnapshot PollOnce();

    DashboardSyncResult SyncNow(bool syncEnabled);
}
