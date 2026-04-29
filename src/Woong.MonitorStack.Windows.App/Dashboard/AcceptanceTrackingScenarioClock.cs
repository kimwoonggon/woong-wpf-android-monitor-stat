using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public sealed class AcceptanceTrackingScenarioClock : IDashboardClock
{
    public AcceptanceTrackingScenarioClock()
        : this(DateTimeOffset.UtcNow.AddMinutes(-20))
    {
    }

    public AcceptanceTrackingScenarioClock(DateTimeOffset scenarioStartedAtUtc)
    {
        ScenarioStartedAtUtc = scenarioStartedAtUtc.ToUniversalTime();
        UtcNow = ScenarioStartedAtUtc;
    }

    public DateTimeOffset ScenarioStartedAtUtc { get; }

    public DateTimeOffset UtcNow { get; set; }
}
