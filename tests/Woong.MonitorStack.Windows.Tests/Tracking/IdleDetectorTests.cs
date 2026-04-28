using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.Tests.Tracking;

public sealed class IdleDetectorTests
{
    [Fact]
    public void IsIdle_WhenLastInputExceedsThreshold_ReturnsTrue()
    {
        var detector = new IdleDetector(TimeSpan.FromMinutes(5));

        var isIdle = detector.IsIdle(
            nowUtc: new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
            lastInputAtUtc: new DateTimeOffset(2026, 4, 28, 0, 4, 59, TimeSpan.Zero));

        Assert.True(isIdle);
    }

    [Fact]
    public void IsIdle_WhenLastInputIsWithinThreshold_ReturnsFalse()
    {
        var detector = new IdleDetector(TimeSpan.FromMinutes(5));

        var isIdle = detector.IsIdle(
            nowUtc: new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
            lastInputAtUtc: new DateTimeOffset(2026, 4, 28, 0, 5, 1, TimeSpan.Zero));

        Assert.False(isIdle);
    }
}
