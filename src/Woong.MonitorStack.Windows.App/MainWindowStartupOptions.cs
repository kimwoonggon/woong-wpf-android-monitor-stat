namespace Woong.MonitorStack.Windows.App;

public sealed record MainWindowStartupOptions(bool AutoStartTracking)
{
    public static MainWindowStartupOptions Manual { get; } = new(AutoStartTracking: false);
}
