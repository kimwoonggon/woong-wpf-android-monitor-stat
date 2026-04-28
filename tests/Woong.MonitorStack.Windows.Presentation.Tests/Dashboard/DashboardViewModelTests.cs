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
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

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
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

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
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        viewModel.SelectDashboardPeriodCommand.Execute(DashboardPeriod.LastHour);

        Assert.Equal(DashboardPeriod.LastHour, viewModel.SelectedPeriod);
        Assert.Equal("devenv.exe", viewModel.TopAppName);
    }

    [Fact]
    public void SelectPeriod_TodayQueriesCurrentLocalDayRange()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource([], []);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        viewModel.SelectPeriod(DashboardPeriod.Today);

        Assert.Equal(new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero), dataSource.LastFocusQueryStartedAtUtc);
        Assert.Equal(now, dataSource.LastFocusQueryEndedAtUtc);
    }

    [Theory]
    [InlineData(DashboardPeriod.LastHour, 1)]
    [InlineData(DashboardPeriod.Last6Hours, 6)]
    [InlineData(DashboardPeriod.Last24Hours, 24)]
    public void SelectPeriod_RollingPeriodsQueryExpectedUtcRange(DashboardPeriod period, int expectedHours)
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource([], []);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        viewModel.SelectPeriod(period);

        Assert.Equal(now.AddHours(-expectedHours), dataSource.LastFocusQueryStartedAtUtc);
        Assert.Equal(now, dataSource.LastFocusQueryEndedAtUtc);
    }

    [Fact]
    public void SelectCustomRange_QueriesProvidedUtcRange()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var customStart = now.AddHours(-3);
        var customEnd = now.AddHours(-2);
        var dataSource = new FakeDashboardDataSource([], []);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        viewModel.SelectCustomRange(customStart, customEnd);

        Assert.Equal(DashboardPeriod.Custom, viewModel.SelectedPeriod);
        Assert.Equal(customStart, dataSource.LastFocusQueryStartedAtUtc);
        Assert.Equal(customEnd, dataSource.LastFocusQueryEndedAtUtc);
    }

    [Fact]
    public void SelectPeriod_PublishesChartPoints()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource(
            [Session("session-1", "chrome.exe", now.AddMinutes(-30), now.AddMinutes(-10), isIdle: false)],
            [
                WebSession.FromUtc(
                    "session-1",
                    "Chrome",
                    "https://example.com/docs",
                    "Docs",
                    now.AddMinutes(-25),
                    now.AddMinutes(-15))
            ]);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        viewModel.SelectPeriod(DashboardPeriod.LastHour);

        Assert.NotEmpty(viewModel.HourlyActivityPoints);
        var appPoint = Assert.Single(viewModel.AppUsagePoints);
        Assert.Equal("chrome.exe", appPoint.Label);
        Assert.Equal(1_200_000, appPoint.ValueMs);
        var domainPoint = Assert.Single(viewModel.DomainUsagePoints);
        Assert.Equal("example.com", domainPoint.Label);
        Assert.Equal(600_000, domainPoint.ValueMs);
        Assert.Equal("example.com", viewModel.TopDomainName);
    }

    [Fact]
    public void SelectPeriod_PublishesLiveChartsSeries()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource(
            [Session("session-1", "chrome.exe", now.AddMinutes(-30), now.AddMinutes(-10), isIdle: false)],
            [
                WebSession.FromUtc(
                    "session-1",
                    "Chrome",
                    "https://example.com/docs",
                    "Docs",
                    now.AddMinutes(-25),
                    now.AddMinutes(-15))
            ]);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        viewModel.SelectPeriod(DashboardPeriod.LastHour);

        Assert.NotEmpty(viewModel.HourlyActivityChart.Series);
        Assert.Equal(["chrome.exe"], viewModel.AppUsageChart.Labels);
        Assert.NotEmpty(viewModel.AppUsageChart.Series);
        Assert.NotEmpty(viewModel.DomainUsageSeries);
    }

    [Fact]
    public void SelectPeriod_PublishesRecentSessionRows()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource(
            [
                Session("session-1", "chrome.exe", now.AddMinutes(-30), now.AddMinutes(-20), isIdle: false),
                Session("session-2", "devenv.exe", now.AddMinutes(-10), now, isIdle: true)
            ],
            webSessions: []);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        viewModel.SelectPeriod(DashboardPeriod.LastHour);

        Assert.Collection(
            viewModel.RecentSessions,
            row =>
            {
                Assert.Equal("devenv.exe", row.AppName);
                Assert.Equal("11:50", row.StartedAtLocal);
                Assert.Equal("10m", row.Duration);
                Assert.True(row.IsIdle);
            },
            row =>
            {
                Assert.Equal("chrome.exe", row.AppName);
                Assert.Equal("11:30", row.StartedAtLocal);
                Assert.Equal("10m", row.Duration);
                Assert.False(row.IsIdle);
            });
    }

    [Fact]
    public void SelectPeriod_PublishesRecentWebSessionRows()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource(
            [Session("session-1", "chrome.exe", now.AddMinutes(-30), now, isIdle: false)],
            [
                WebSession.FromUtc(
                    "session-1",
                    "Chrome",
                    "https://docs.example.com/start",
                    "Getting Started",
                    now.AddMinutes(-20),
                    now.AddMinutes(-5)),
                WebSession.FromUtc(
                    "session-2",
                    "Chrome",
                    "https://openai.com/news",
                    "News",
                    now.AddMinutes(-10),
                    now.AddMinutes(-1))
            ]);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        viewModel.SelectPeriod(DashboardPeriod.LastHour);

        Assert.Collection(
            viewModel.RecentWebSessions,
            row =>
            {
                Assert.Equal("openai.com", row.Domain);
                Assert.Equal("News", row.PageTitle);
                Assert.Equal("11:50", row.StartedAtLocal);
                Assert.Equal("9m", row.Duration);
            },
            row =>
            {
                Assert.Equal("example.com", row.Domain);
                Assert.Equal("Getting Started", row.PageTitle);
                Assert.Equal("11:40", row.StartedAtLocal);
                Assert.Equal("15m", row.Duration);
            });
    }

    [Fact]
    public void SelectPeriod_PublishesLiveEventLogRows()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource(
            [Session("session-1", "chrome.exe", now.AddMinutes(-30), now, isIdle: false)],
            [
                WebSession.FromUtc(
                    "session-1",
                    "Chrome",
                    "https://example.com/article",
                    "Article",
                    now.AddMinutes(-20),
                    now.AddMinutes(-5))
            ]);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        viewModel.SelectPeriod(DashboardPeriod.LastHour);

        Assert.Collection(
            viewModel.LiveEvents,
            row =>
            {
                Assert.Equal("Web", row.Kind);
                Assert.Equal("11:40", row.OccurredAtLocal);
                Assert.Equal("example.com", row.Message);
            },
            row =>
            {
                Assert.Equal("Focus", row.Kind);
                Assert.Equal("11:30", row.OccurredAtLocal);
                Assert.Equal("chrome.exe", row.Message);
            });
    }

    [Fact]
    public void Constructor_ExposesSettings()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource([], []);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        Assert.True(viewModel.Settings.IsCollectionVisible);
        Assert.False(viewModel.Settings.IsSyncEnabled);
    }

    [Fact]
    public void SelectPeriod_WithEmptyDataPublishesSafeZeroState()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource([], []);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        viewModel.SelectPeriod(DashboardPeriod.LastHour);

        Assert.Equal(0, viewModel.TotalActiveMs);
        Assert.Equal(0, viewModel.TotalIdleMs);
        Assert.Equal(0, viewModel.TotalWebMs);
        Assert.Equal("", viewModel.TopAppName);
        Assert.Equal("", viewModel.TopDomainName);
        Assert.All(viewModel.SummaryCards, card => Assert.Equal("0m", card.Value));
        Assert.Empty(viewModel.RecentSessions);
        Assert.Empty(viewModel.RecentWebSessions);
        Assert.Empty(viewModel.LiveEvents);
    }

    [Fact]
    public void DashboardOptions_WhenTimezoneIsInvalid_ThrowsTimeZoneNotFoundException()
    {
        Assert.Throws<TimeZoneNotFoundException>(() => new DashboardOptions("Invalid/Timezone"));
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
        public DateTimeOffset LastFocusQueryStartedAtUtc { get; private set; }

        public DateTimeOffset LastFocusQueryEndedAtUtc { get; private set; }

        public IReadOnlyList<FocusSession> QueryFocusSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
        {
            LastFocusQueryStartedAtUtc = startedAtUtc;
            LastFocusQueryEndedAtUtc = endedAtUtc;

            return focusSessions;
        }

        public IReadOnlyList<WebSession> QueryWebSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => webSessions;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IDashboardClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
