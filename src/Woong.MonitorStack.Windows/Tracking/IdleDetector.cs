namespace Woong.MonitorStack.Windows.Tracking;

public sealed class IdleDetector
{
    public IdleDetector(TimeSpan threshold)
    {
        Threshold = threshold > TimeSpan.Zero
            ? threshold
            : throw new ArgumentOutOfRangeException(nameof(threshold));
    }

    public TimeSpan Threshold { get; }

    public bool IsIdle(DateTimeOffset nowUtc, DateTimeOffset lastInputAtUtc)
        => nowUtc.ToUniversalTime() - lastInputAtUtc.ToUniversalTime() >= Threshold;
}
