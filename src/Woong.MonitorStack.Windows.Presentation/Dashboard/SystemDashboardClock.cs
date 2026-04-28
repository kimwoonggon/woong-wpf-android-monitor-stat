namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed class SystemDashboardClock : IDashboardClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
