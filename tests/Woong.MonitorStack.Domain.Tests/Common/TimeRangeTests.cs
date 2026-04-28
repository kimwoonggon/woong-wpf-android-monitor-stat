using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Domain.Tests.Common;

public sealed class TimeRangeTests
{
    [Fact]
    public void FromUtc_NormalizesOffsetsToUtc()
    {
        var range = TimeRange.FromUtc(
            new DateTimeOffset(2026, 4, 28, 9, 0, 0, TimeSpan.FromHours(9)),
            new DateTimeOffset(2026, 4, 28, 9, 30, 0, TimeSpan.FromHours(9)));

        Assert.Equal(TimeSpan.Zero, range.StartedAtUtc.Offset);
        Assert.Equal(TimeSpan.Zero, range.EndedAtUtc.Offset);
        Assert.Equal(new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero), range.StartedAtUtc);
        Assert.Equal(new DateTimeOffset(2026, 4, 28, 0, 30, 0, TimeSpan.Zero), range.EndedAtUtc);
    }

    [Fact]
    public void FromUtc_WhenEndIsNotAfterStart_Throws()
    {
        var instant = new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => TimeRange.FromUtc(instant, instant));
    }
}
