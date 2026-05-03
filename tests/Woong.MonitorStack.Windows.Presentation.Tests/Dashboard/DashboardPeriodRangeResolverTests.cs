using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.Presentation.Tests.Dashboard;

public sealed class DashboardPeriodRangeResolverTests
{
    [Fact]
    public void ResolveRange_TodayStartsAtDashboardLocalMidnight()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        DashboardPeriodRangeResolver resolver = CreateResolver();

        TimeRange range = resolver.ResolveRange(DashboardPeriod.Today, now);

        Assert.Equal(new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero), range.StartedAtUtc);
        Assert.Equal(now, range.EndedAtUtc);
    }

    [Theory]
    [InlineData(DashboardPeriod.LastHour, 1)]
    [InlineData(DashboardPeriod.Last6Hours, 6)]
    [InlineData(DashboardPeriod.Last24Hours, 24)]
    public void ResolveRange_RollingPeriodsUseUtcNow(DashboardPeriod period, int expectedHours)
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        DashboardPeriodRangeResolver resolver = CreateResolver();

        TimeRange range = resolver.ResolveRange(period, now);

        Assert.Equal(now.AddHours(-expectedHours), range.StartedAtUtc);
        Assert.Equal(now, range.EndedAtUtc);
    }

    [Fact]
    public void ResolveRange_CustomUsesProvidedRangeOrLastHourFallback()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        TimeRange customRange = TimeRange.FromUtc(now.AddHours(-3), now.AddHours(-2));
        DashboardPeriodRangeResolver resolver = CreateResolver();

        TimeRange resolvedCustom = resolver.ResolveRange(DashboardPeriod.Custom, now, customRange);
        TimeRange fallbackCustom = resolver.ResolveRange(DashboardPeriod.Custom, now);

        Assert.Equal(customRange, resolvedCustom);
        Assert.Equal(now.AddHours(-1), fallbackCustom.StartedAtUtc);
        Assert.Equal(now, fallbackCustom.EndedAtUtc);
    }

    [Fact]
    public void ConvertLocalDashboardDateTimeToUtc_UsesDashboardTimezone()
    {
        DashboardPeriodRangeResolver resolver = CreateResolver();

        DateTimeOffset utcValue = resolver.ConvertLocalDashboardDateTimeToUtc(
            new DateTime(2026, 4, 28),
            new TimeSpan(9, 15, 0));

        Assert.Equal(new DateTimeOffset(2026, 4, 28, 0, 15, 0, TimeSpan.Zero), utcValue);
    }

    [Fact]
    public void FormatCustomRangeStatus_RendersDashboardLocalRange()
    {
        DashboardPeriodRangeResolver resolver = CreateResolver();
        TimeRange range = TimeRange.FromUtc(
            new DateTimeOffset(2026, 4, 28, 0, 15, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 45, 0, TimeSpan.Zero));

        string status = resolver.FormatCustomRangeStatus(range);

        Assert.Equal("2026-04-28 09:15 - 2026-04-28 10:45", status);
    }

    [Theory]
    [InlineData("09:15", true, 9, 15)]
    [InlineData("9:15", true, 9, 15)]
    [InlineData("9.15", false, 0, 0)]
    public void TryParseClockTime_AcceptsHourMinuteTextOnly(
        string value,
        bool expectedResult,
        int expectedHour,
        int expectedMinute)
    {
        DashboardPeriodRangeResolver resolver = CreateResolver();

        bool result = resolver.TryParseClockTime(value, out TimeSpan time);

        Assert.Equal(expectedResult, result);
        if (expectedResult)
        {
            Assert.Equal(new TimeSpan(expectedHour, expectedMinute, 0), time);
        }
    }

    [Fact]
    public void CreateCustomRangeDefaults_UsesDashboardLocalNowAndPreviousHour()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        DashboardPeriodRangeResolver resolver = CreateResolver();

        DashboardCustomRangeDefaults defaults = resolver.CreateCustomRangeDefaults(now);

        Assert.Equal(new DateTime(2026, 4, 28), defaults.StartDate);
        Assert.Equal(new DateTime(2026, 4, 28), defaults.EndDate);
        Assert.Equal("11:00", defaults.StartTimeText);
        Assert.Equal("12:00", defaults.EndTimeText);
    }

    private static DashboardPeriodRangeResolver CreateResolver()
        => new(TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul"));
}
