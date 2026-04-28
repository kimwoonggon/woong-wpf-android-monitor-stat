namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public interface IDashboardTrackingCoordinator
{
    DashboardTrackingSnapshot StartTracking();

    DashboardTrackingSnapshot StopTracking();

    DashboardSyncResult SyncNow(bool syncEnabled);
}
