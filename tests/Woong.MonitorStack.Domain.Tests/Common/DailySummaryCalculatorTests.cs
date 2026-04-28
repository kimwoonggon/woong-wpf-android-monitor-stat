using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Domain.Tests.Common;

public sealed class DailySummaryCalculatorTests
{
    [Fact]
    public void Calculate_ExcludesIdleSessionsFromActiveTotal()
    {
        var active = Session(
            "active-session",
            new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 27, 15, 30, 0, TimeSpan.Zero),
            isIdle: false);
        var idle = Session(
            "idle-session",
            new DateTimeOffset(2026, 4, 27, 15, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 27, 16, 0, 0, TimeSpan.Zero),
            isIdle: true);

        var summary = DailySummaryCalculator.Calculate([active, idle], new DateOnly(2026, 4, 28));

        Assert.Equal(1_800_000, summary.TotalActiveMs);
        Assert.Equal(1_800_000, summary.TotalIdleMs);
    }

    [Fact]
    public void Calculate_GroupsByLocalDate()
    {
        var previousLocalDate = Session(
            "previous-local-date",
            new DateTimeOffset(2026, 4, 27, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 27, 14, 30, 0, TimeSpan.Zero),
            isIdle: false);
        var targetLocalDate = Session(
            "target-local-date",
            new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 27, 15, 20, 0, TimeSpan.Zero),
            isIdle: false);

        var summary = DailySummaryCalculator.Calculate([previousLocalDate, targetLocalDate], new DateOnly(2026, 4, 28));

        Assert.Equal(1_200_000, summary.TotalActiveMs);
    }

    [Fact]
    public void Calculate_IncludesWebTotalsAndTopDomains()
    {
        var focus = Session(
            "focus-session",
            new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 27, 15, 10, 0, TimeSpan.Zero),
            isIdle: false);
        var web = WebSession.FromUtc(
            focusSessionId: "focus-session",
            browserFamily: "Chrome",
            url: "https://www.youtube.com/watch?v=abc",
            pageTitle: "Video",
            startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 5, 0, TimeSpan.Zero));

        var summary = DailySummaryCalculator.Calculate([focus], [web], new DateOnly(2026, 4, 28), "Asia/Seoul");

        var topDomain = Assert.Single(summary.TopDomains);
        Assert.Equal(300_000, summary.TotalWebMs);
        Assert.Equal("youtube.com", topDomain.Key);
        Assert.Equal(300_000, topDomain.DurationMs);
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
