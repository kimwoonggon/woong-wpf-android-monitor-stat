using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.Presentation.Tests.Dashboard;

public sealed class DashboardChartMapperTests
{
    [Fact]
    public void BuildHourlyActivityPoints_WithEmptyInput_ReturnsEmptyPoints()
    {
        IReadOnlyList<DashboardChartPoint> points = DashboardChartMapper.BuildHourlyActivityPoints([], "Asia/Seoul");

        Assert.Empty(points);
    }

    [Fact]
    public void BuildHourlyActivityPoints_UsesDisplayTimezoneAndExcludesIdle()
    {
        var active = Session(
            "active-session",
            new DateTimeOffset(2026, 4, 27, 15, 45, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 27, 16, 15, 0, TimeSpan.Zero),
            isIdle: false);
        var idle = Session(
            "idle-session",
            new DateTimeOffset(2026, 4, 27, 16, 15, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 27, 16, 45, 0, TimeSpan.Zero),
            isIdle: true);

        var points = DashboardChartMapper.BuildHourlyActivityPoints([active, idle], "Asia/Seoul");

        Assert.Collection(
            points,
            point =>
            {
                Assert.Equal("00", point.Label);
                Assert.Equal(900_000, point.ValueMs);
            },
            point =>
            {
                Assert.Equal("01", point.Label);
                Assert.Equal(900_000, point.ValueMs);
            });
    }

    [Fact]
    public void BuildUsagePoints_MapsAppAndDomainTotals()
    {
        var summary = new DailySummary(
            new DateOnly(2026, 4, 28),
            TotalActiveMs: 1_800_000,
            TotalIdleMs: 0,
            TotalWebMs: 600_000,
            TopApps:
            [
                new UsageTotal("Chrome", 1_200_000),
                new UsageTotal("VS Code", 600_000)
            ],
            TopDomains:
            [
                new UsageTotal("example.com", 600_000)
            ]);

        IReadOnlyList<DashboardChartPoint> appPoints = DashboardChartMapper.BuildAppUsagePoints(summary);
        IReadOnlyList<DashboardChartPoint> domainPoints = DashboardChartMapper.BuildDomainUsagePoints(summary);

        Assert.Collection(
            appPoints,
            point =>
            {
                Assert.Equal("Chrome", point.Label);
                Assert.Equal(1_200_000, point.ValueMs);
            },
            point =>
            {
                Assert.Equal("VS Code", point.Label);
                Assert.Equal(600_000, point.ValueMs);
            });
        var domainPoint = Assert.Single(domainPoints);
        Assert.Equal("example.com", domainPoint.Label);
        Assert.Equal(600_000, domainPoint.ValueMs);
    }

    private static FocusSession Session(
        string clientSessionId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc,
        bool isIdle)
        => FocusSession.FromUtc(
            clientSessionId,
            deviceId: "windows-device-1",
            platformAppKey: "chrome.exe",
            startedAtUtc,
            endedAtUtc,
            timezoneId: "Asia/Seoul",
            isIdle,
            source: "foreground_window");
}
