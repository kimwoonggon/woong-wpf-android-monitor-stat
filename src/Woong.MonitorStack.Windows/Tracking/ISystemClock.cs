namespace Woong.MonitorStack.Windows.Tracking;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}
