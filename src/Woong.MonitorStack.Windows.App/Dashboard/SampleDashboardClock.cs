using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public sealed class SampleDashboardClock(DateTimeOffset utcNow) : IDashboardClock
{
    public DateTimeOffset UtcNow { get; } = utcNow.ToUniversalTime();
}
