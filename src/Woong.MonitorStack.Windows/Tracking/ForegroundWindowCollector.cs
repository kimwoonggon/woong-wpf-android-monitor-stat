namespace Woong.MonitorStack.Windows.Tracking;

public sealed class ForegroundWindowCollector
{
    private readonly IForegroundWindowReader _reader;
    private readonly ISystemClock _clock;

    public ForegroundWindowCollector(IForegroundWindowReader reader, ISystemClock clock)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public ForegroundWindowSnapshot Capture()
    {
        var info = _reader.ReadForegroundWindow();

        return new ForegroundWindowSnapshot(
            info.Hwnd,
            info.ProcessId,
            info.ProcessName,
            info.ExecutablePath,
            info.WindowTitle,
            _clock.UtcNow);
    }
}
