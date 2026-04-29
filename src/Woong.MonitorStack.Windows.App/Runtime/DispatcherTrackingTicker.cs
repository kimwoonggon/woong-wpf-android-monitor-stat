using System.Windows.Threading;

namespace Woong.MonitorStack.Windows.App;

public sealed class DispatcherTrackingTicker : ITrackingTicker
{
    private readonly DispatcherTimer _timer;

    public DispatcherTrackingTicker()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (_, _) => Tick?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Tick;

    public bool IsRunning
        => _timer.IsEnabled;

    public void Start()
        => _timer.Start();

    public void Stop()
        => _timer.Stop();
}
