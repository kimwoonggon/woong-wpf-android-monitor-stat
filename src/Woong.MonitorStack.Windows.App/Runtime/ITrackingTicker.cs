namespace Woong.MonitorStack.Windows.App;

public interface ITrackingTicker
{
    event EventHandler? Tick;

    bool IsRunning { get; }

    void Start();

    void Stop();
}
