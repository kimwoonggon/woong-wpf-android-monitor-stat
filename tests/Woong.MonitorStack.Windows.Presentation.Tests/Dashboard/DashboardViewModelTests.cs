using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.Presentation.Tests.Dashboard;

public sealed class DashboardViewModelTests
{
    [Fact]
    public void SelectPeriod_RefreshesSummaryCards()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource(
            [
                Session("session-1", "chrome.exe", now.AddMinutes(-30), now.AddMinutes(-10), isIdle: false),
                Session("session-2", "chrome.exe", now.AddMinutes(-10), now, isIdle: true)
            ],
            webSessions: []);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), timezoneId: "Asia/Seoul");

        viewModel.SelectPeriod(DashboardPeriod.LastHour);

        Assert.Equal(DashboardPeriod.LastHour, viewModel.SelectedPeriod);
        Assert.Equal(1_200_000, viewModel.TotalActiveMs);
        Assert.Equal(600_000, viewModel.TotalIdleMs);
        Assert.Equal("chrome.exe", viewModel.TopAppName);
    }

    [Fact]
    public void SelectPeriod_PublishesSummaryCardModels()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource(
            [
                Session("session-1", "chrome.exe", now.AddMinutes(-30), now.AddMinutes(-10), isIdle: false),
                Session("session-2", "chrome.exe", now.AddMinutes(-10), now, isIdle: true)
            ],
            [
                WebSession.FromUtc(
                    "session-1",
                    "Chrome",
                    "https://example.com/docs",
                    "Docs",
                    now.AddMinutes(-25),
                    now.AddMinutes(-15))
            ]);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), timezoneId: "Asia/Seoul");

        viewModel.SelectPeriod(DashboardPeriod.LastHour);

        Assert.Collection(
            viewModel.SummaryCards,
            card =>
            {
                Assert.Equal("Active", card.Label);
                Assert.Equal("20m", card.Value);
            },
            card =>
            {
                Assert.Equal("Idle", card.Label);
                Assert.Equal("10m", card.Value);
            },
            card =>
            {
                Assert.Equal("Web", card.Label);
                Assert.Equal("10m", card.Value);
            });
    }

    [Fact]
    public void SelectDashboardPeriodCommand_RefreshesSummary()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource(
            [Session("session-1", "devenv.exe", now.AddMinutes(-5), now, isIdle: false)],
            webSessions: []);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), timezoneId: "Asia/Seoul");

        viewModel.SelectDashboardPeriodCommand.Execute(DashboardPeriod.LastHour);

        Assert.Equal(DashboardPeriod.LastHour, viewModel.SelectedPeriod);
        Assert.Equal("devenv.exe", viewModel.TopAppName);
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
            source: "foreground_window");

    private sealed class FakeDashboardDataSource(
        IReadOnlyList<FocusSession> focusSessions,
        IReadOnlyList<WebSession> webSessions) : IDashboardDataSource
    {
        public IReadOnlyList<FocusSession> QueryFocusSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => focusSessions;

        public IReadOnlyList<WebSession> QueryWebSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => webSessions;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IDashboardClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
