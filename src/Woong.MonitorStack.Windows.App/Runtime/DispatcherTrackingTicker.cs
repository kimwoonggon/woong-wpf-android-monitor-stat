using System.Windows.Threading;

namespace Woong.MonitorStack.Windows.App;

public sealed class DispatcherTrackingTicker : ITrackingTicker
{
    public const string IntervalEnvironmentVariable = "WOONG_MONITOR_TRACKING_TICK_INTERVAL_MS";

    private static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(1);

    private readonly DispatcherTimer _timer;

    public DispatcherTrackingTicker()
    {
        _timer = new DispatcherTimer
        {
            Interval = ResolveInterval()
        };
        _timer.Tick += (_, _) => Tick?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Tick;

    public TimeSpan Interval
        => _timer.Interval;

    public bool IsRunning
        => _timer.IsEnabled;

    public void Start()
        => _timer.Start();

    public void Stop()
        => _timer.Stop();

    private static TimeSpan ResolveInterval()
    {
        string? configuredValue = Environment.GetEnvironmentVariable(IntervalEnvironmentVariable);
        if (!int.TryParse(configuredValue, out int milliseconds))
        {
            return DefaultInterval;
        }

        if (milliseconds < 100 || milliseconds > 600_000)
        {
            return DefaultInterval;
        }

        return TimeSpan.FromMilliseconds(milliseconds);
    }
}
