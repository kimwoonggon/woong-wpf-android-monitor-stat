namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public interface IDashboardClock
{
    DateTimeOffset UtcNow { get; }
}
