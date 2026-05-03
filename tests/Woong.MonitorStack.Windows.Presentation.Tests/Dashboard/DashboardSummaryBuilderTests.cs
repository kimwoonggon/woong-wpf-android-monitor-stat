using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.Presentation.Tests.Dashboard;

public sealed class DashboardSummaryBuilderTests
{
    [Fact]
    public void Build_AggregatesRangeSummaryAndCards()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        FocusSession[] focusSessions =
        [
            Session("focus-1", "Code.exe", now.AddMinutes(-45), now.AddMinutes(-15), isIdle: false),
            Session("focus-2", "Chrome.exe", now.AddMinutes(-15), now.AddMinutes(-5), isIdle: true)
        ];
        WebSession[] webSessions =
        [
            WebSession.FromUtc(
                "focus-1",
                "Chrome",
                "https://example.com/docs",
                "Docs",
                now.AddMinutes(-30),
                now.AddMinutes(-20))
        ];
        TimeRange range = TimeRange.FromUtc(now.AddHours(-1), now);

        DashboardSummarySnapshot snapshot = DashboardSummaryBuilder.Build(
            focusSessions,
            webSessions,
            range,
            DashboardPeriod.LastHour,
            "Asia/Seoul");

        Assert.Equal(1_800_000, snapshot.TotalActiveMs);
        Assert.Equal(2_400_000, snapshot.TotalForegroundMs);
        Assert.Equal(600_000, snapshot.TotalIdleMs);
        Assert.Equal(600_000, snapshot.TotalWebMs);
        Assert.Equal("Code.exe", snapshot.TopAppName);
        Assert.Equal("example.com", snapshot.TopDomainName);
        Assert.Collection(
            snapshot.SummaryCards,
            card => Assert.Equal(("Active Focus", "30m", "Last 1h focused foreground time"), (card.Label, card.Value, card.Subtitle)),
            card => Assert.Equal(("Foreground", "40m", "Last 1h foreground time"), (card.Label, card.Value, card.Subtitle)),
            card => Assert.Equal(("Idle", "10m", "Last 1h idle foreground time"), (card.Label, card.Value, card.Subtitle)),
            card => Assert.Equal(("Web Focus", "10m", "Last 1h browser domain time"), (card.Label, card.Value, card.Subtitle)));
    }

    private static FocusSession Session(
        string clientSessionId,
        string appKey,
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc,
        bool isIdle)
        => FocusSession.FromUtc(
            clientSessionId,
            deviceId: "windows-device-1",
            platformAppKey: appKey,
            startedAtUtc,
            endedAtUtc,
            timezoneId: "Asia/Seoul",
            isIdle,
            source: "foreground_window",
            processName: appKey);
}
