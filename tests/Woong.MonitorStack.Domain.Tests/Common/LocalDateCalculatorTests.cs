using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Domain.Tests.Common;

public sealed class LocalDateCalculatorTests
{
    [Fact]
    public void GetLocalDate_ConvertsUtcInstantUsingDeviceTimezone()
    {
        var utcInstant = new DateTimeOffset(2026, 4, 27, 15, 30, 0, TimeSpan.Zero);

        var localDate = LocalDateCalculator.GetLocalDate(utcInstant, "Asia/Seoul");

        Assert.Equal(new DateOnly(2026, 4, 28), localDate);
    }

    [Fact]
    public void GetLocalDate_WhenSessionCrossesMidnight_UsesDeviceTimezone()
    {
        var beforeMidnightUtc = new DateTimeOffset(2026, 4, 27, 14, 50, 0, TimeSpan.Zero);
        var afterMidnightUtc = new DateTimeOffset(2026, 4, 27, 15, 10, 0, TimeSpan.Zero);

        var startLocalDate = LocalDateCalculator.GetLocalDate(beforeMidnightUtc, "Asia/Seoul");
        var endLocalDate = LocalDateCalculator.GetLocalDate(afterMidnightUtc, "Asia/Seoul");

        Assert.Equal(new DateOnly(2026, 4, 27), startLocalDate);
        Assert.Equal(new DateOnly(2026, 4, 28), endLocalDate);
    }
}
