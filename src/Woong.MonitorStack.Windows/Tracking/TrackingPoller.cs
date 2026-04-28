namespace Woong.MonitorStack.Windows.Tracking;

public sealed class TrackingPoller
{
    private readonly ForegroundWindowCollector _foregroundWindowCollector;
    private readonly ILastInputReader _lastInputReader;
    private readonly IdleDetector _idleDetector;
    private readonly FocusSessionizer _sessionizer;

    public TrackingPoller(
        ForegroundWindowCollector foregroundWindowCollector,
        ILastInputReader lastInputReader,
        IdleDetector idleDetector,
        FocusSessionizer sessionizer)
    {
        _foregroundWindowCollector = foregroundWindowCollector ?? throw new ArgumentNullException(nameof(foregroundWindowCollector));
        _lastInputReader = lastInputReader ?? throw new ArgumentNullException(nameof(lastInputReader));
        _idleDetector = idleDetector ?? throw new ArgumentNullException(nameof(idleDetector));
        _sessionizer = sessionizer ?? throw new ArgumentNullException(nameof(sessionizer));
    }

    public FocusSessionizerResult Poll()
    {
        var snapshot = _foregroundWindowCollector.Capture();
        var lastInputAtUtc = _lastInputReader.ReadLastInputAtUtc(snapshot.TimestampUtc);
        var isIdle = _idleDetector.IsIdle(snapshot.TimestampUtc, lastInputAtUtc);

        return _sessionizer.Process(snapshot, isIdle);
    }
}
