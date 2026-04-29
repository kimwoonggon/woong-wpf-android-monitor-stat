using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public sealed class AcceptanceTrackingScenarioClock : IDashboardClock
{
    public AcceptanceTrackingScenarioClock()
        : this(CreateLocalNoonTodayUtc())
    {
    }

    public AcceptanceTrackingScenarioClock(DateTimeOffset scenarioStartedAtUtc)
    {
        ScenarioStartedAtUtc = scenarioStartedAtUtc.ToUniversalTime();
        UtcNow = ScenarioStartedAtUtc;
    }

    public DateTimeOffset ScenarioStartedAtUtc { get; }

    public DateTimeOffset UtcNow { get; set; }

    private static DateTimeOffset CreateLocalNoonTodayUtc()
    {
        DateTime localNoonToday = DateTime.Today.AddHours(12);

        return new DateTimeOffset(localNoonToday).ToUniversalTime();
    }
}
