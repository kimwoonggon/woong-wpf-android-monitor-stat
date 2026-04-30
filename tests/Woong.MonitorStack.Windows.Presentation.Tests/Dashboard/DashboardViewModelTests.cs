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
    public void SelectPeriod_Last24HoursAggregatesPersistedSessionsAcrossLocalDateBoundary()
    {
        var now = new DateTimeOffset(2026, 4, 28, 16, 30, 0, TimeSpan.Zero);
        FocusSession previousLocalDateSession = Session(
            "session-previous-local-date",
            "Code.exe",
            new DateTimeOffset(2026, 4, 28, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 14, 30, 0, TimeSpan.Zero),
            isIdle: false);
        var dataSource = new FakeDashboardDataSource([previousLocalDateSession], []);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        viewModel.SelectPeriod(DashboardPeriod.Last24Hours);

        Assert.Equal(1_800_000, viewModel.TotalActiveMs);
        Assert.Equal(1_800_000, viewModel.TotalForegroundMs);
        Assert.Equal("Code.exe", viewModel.TopAppName);
        Assert.Equal(now.AddHours(-24), dataSource.LastFocusQueryStartedAtUtc);
        Assert.Equal(now, dataSource.LastFocusQueryEndedAtUtc);
    }

    [Fact]
    public void ApplyCustomRangeCommand_ParsesLocalDateAndTimeThenQueriesUtcRange()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource([], []);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"))
        {
            CustomStartDate = new DateTime(2026, 4, 28),
            CustomStartTimeText = "09:15",
            CustomEndDate = new DateTime(2026, 4, 28),
            CustomEndTimeText = "10:45"
        };

        viewModel.ApplyCustomRangeCommand.Execute(null);

        Assert.Equal(DashboardPeriod.Custom, viewModel.SelectedPeriod);
        Assert.True(viewModel.IsCustomRangeEditorVisible);
        Assert.Equal(new DateTimeOffset(2026, 4, 28, 0, 15, 0, TimeSpan.Zero), dataSource.LastFocusQueryStartedAtUtc);
        Assert.Equal(new DateTimeOffset(2026, 4, 28, 1, 45, 0, TimeSpan.Zero), dataSource.LastFocusQueryEndedAtUtc);
        Assert.Contains("09:15", viewModel.CustomRangeStatusText, StringComparison.Ordinal);
        Assert.Contains("10:45", viewModel.CustomRangeStatusText, StringComparison.Ordinal);
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
                Assert.Equal("Active Focus", card.Label);
                Assert.Equal("20m", card.Value);
            },
            card =>
            {
                Assert.Equal("Foreground", card.Label);
                Assert.Equal("30m", card.Value);
            },
            card =>
            {
                Assert.Equal("Idle", card.Label);
                Assert.Equal("10m", card.Value);
            },
            card =>
            {
                Assert.Equal("Web Focus", card.Label);
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
    public void ShowAppFocusDetailsCommand_SelectsAppSessionsTab()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var viewModel = new DashboardViewModel(
            new FakeDashboardDataSource([], []),
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"))
        {
            SelectedDetailsTab = DetailsTab.Settings
        };

        viewModel.ShowAppFocusDetailsCommand.Execute(null);

        Assert.Equal(DetailsTab.AppSessions, viewModel.SelectedDetailsTab);
    }

    [Fact]
    public void ShowDomainFocusDetailsCommand_SelectsWebSessionsTab()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var viewModel = new DashboardViewModel(
            new FakeDashboardDataSource([], []),
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"));

        viewModel.ShowDomainFocusDetailsCommand.Execute(null);

        Assert.Equal(DetailsTab.WebSessions, viewModel.SelectedDetailsTab);
    }

    [Fact]
    public void ExitApplicationCommand_RequestsExplicitApplicationExit()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var applicationLifetime = new RecordingApplicationLifetime();
        var viewModel = new DashboardViewModel(
            new FakeDashboardDataSource([], []),
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"),
            applicationLifetime: applicationLifetime);

        viewModel.ExitApplicationCommand.Execute(null);

        Assert.Equal(1, applicationLifetime.RequestExitCallCount);
    }

    [Fact]
    public void DefaultApplicationLifetime_DoesNothingWhenExitCommandRuns()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var viewModel = new DashboardViewModel(
            new FakeDashboardDataSource([], []),
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"));

        viewModel.ExitApplicationCommand.Execute(null);

        Assert.True(viewModel.ExitApplicationCommand.CanExecute(null));
    }

    [Fact]
    public void DetailsTabs_DefaultRowsPerPageIsTen()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var viewModel = new DashboardViewModel(
            new FakeDashboardDataSource([], []),
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"));

        Assert.Equal(10, viewModel.RowsPerPage);
        Assert.Equal([10, 25, 50], viewModel.RowsPerPageOptions);
        Assert.Equal(1, viewModel.CurrentDetailsPage);
        Assert.Equal("1 / 1", viewModel.DetailsPageText);
    }

    [Fact]
    public void DetailsTabs_NextAndPreviousPageUpdateVisibleRows()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        FocusSession[] sessions = Enumerable.Range(0, 12)
            .Select(index => Session(
                $"session-{index}",
                $"app-{index}",
                now.AddMinutes(-index - 1),
                now.AddMinutes(-index),
                isIdle: false))
            .ToArray();
        var viewModel = new DashboardViewModel(
            new FakeDashboardDataSource(sessions, []),
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"));

        viewModel.SelectPeriod(DashboardPeriod.LastHour);

        Assert.Equal(12, viewModel.RecentSessions.Count);
        Assert.Equal(10, viewModel.VisibleAppSessionRows.Count);
        Assert.Equal("app-0", viewModel.VisibleAppSessionRows[0].AppName);
        Assert.True(viewModel.NextDetailsPageCommand.CanExecute(null));

        viewModel.NextDetailsPageCommand.Execute(null);

        Assert.Equal(2, viewModel.CurrentDetailsPage);
        Assert.Equal("2 / 2", viewModel.DetailsPageText);
        Assert.Equal(2, viewModel.VisibleAppSessionRows.Count);
        Assert.Equal("app-10", viewModel.VisibleAppSessionRows[0].AppName);
        Assert.False(viewModel.NextDetailsPageCommand.CanExecute(null));
        Assert.True(viewModel.PreviousDetailsPageCommand.CanExecute(null));

        viewModel.PreviousDetailsPageCommand.Execute(null);

        Assert.Equal(1, viewModel.CurrentDetailsPage);
        Assert.Equal(10, viewModel.VisibleAppSessionRows.Count);
        Assert.Equal("app-0", viewModel.VisibleAppSessionRows[0].AppName);
    }

    [Fact]
    public void DetailsTabs_WhenSwitchingTabsFromLaterPage_UsesSelectedTabPageCount()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        FocusSession[] sessions = Enumerable.Range(0, 12)
            .Select(index => Session(
                $"session-{index}",
                $"app-{index}",
                now.AddMinutes(-index - 1),
                now.AddMinutes(-index),
                isIdle: false))
            .ToArray();
        var viewModel = new DashboardViewModel(
            new FakeDashboardDataSource(
                sessions,
                [
                    WebSession.FromUtc(
                        "session-web",
                        "Chrome",
                        "https://github.com/org/repo",
                        "GitHub",
                        now.AddMinutes(-7),
                        now.AddMinutes(-2))
                ]),
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"));

        viewModel.SelectPeriod(DashboardPeriod.LastHour);
        viewModel.NextDetailsPageCommand.Execute(null);

        Assert.Equal(2, viewModel.CurrentDetailsPage);
        Assert.Equal("2 / 2", viewModel.DetailsPageText);

        viewModel.ShowDomainFocusDetailsCommand.Execute(null);

        Assert.Equal(DetailsTab.WebSessions, viewModel.SelectedDetailsTab);
        Assert.Equal(1, viewModel.CurrentDetailsPage);
        Assert.Equal("1 / 1", viewModel.DetailsPageText);
        Assert.False(viewModel.NextDetailsPageCommand.CanExecute(null));
        DashboardWebSessionRow row = Assert.Single(viewModel.VisibleWebSessionRows);
        Assert.Equal("github.com", row.Domain);
    }

    [Fact]
    public void ShowAppFocusDetailsCommand_WhenAlreadyOnAppSessions_ResetsPagerToFirstPage()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        FocusSession[] sessions = Enumerable.Range(0, 12)
            .Select(index => Session(
                $"session-{index}",
                $"app-{index}",
                now.AddMinutes(-index - 1),
                now.AddMinutes(-index),
                isIdle: false))
            .ToArray();
        var viewModel = new DashboardViewModel(
            new FakeDashboardDataSource(sessions, []),
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"));
        viewModel.SelectPeriod(DashboardPeriod.LastHour);
        viewModel.NextDetailsPageCommand.Execute(null);

        Assert.Equal(2, viewModel.CurrentDetailsPage);
        Assert.Equal("app-10", viewModel.VisibleAppSessionRows[0].AppName);

        viewModel.ShowAppFocusDetailsCommand.Execute(null);

        Assert.Equal(DetailsTab.AppSessions, viewModel.SelectedDetailsTab);
        Assert.Equal(1, viewModel.CurrentDetailsPage);
        Assert.Equal("1 / 2", viewModel.DetailsPageText);
        Assert.Equal("app-0", viewModel.VisibleAppSessionRows[0].AppName);
    }

    [Fact]
    public void DetailsTabs_WhenRowsPerPageShrinksPageCount_ClampsCurrentPageToLastAvailablePage()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        FocusSession[] sessions = Enumerable.Range(0, 26)
            .Select(index => Session(
                $"session-{index}",
                $"app-{index}",
                now.AddMinutes(-index - 1),
                now.AddMinutes(-index),
                isIdle: false))
            .ToArray();
        var viewModel = new DashboardViewModel(
            new FakeDashboardDataSource(sessions, []),
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"));
        viewModel.SelectPeriod(DashboardPeriod.LastHour);
        viewModel.NextDetailsPageCommand.Execute(null);
        viewModel.NextDetailsPageCommand.Execute(null);

        Assert.Equal(3, viewModel.CurrentDetailsPage);
        Assert.Equal("3 / 3", viewModel.DetailsPageText);
        Assert.Equal("app-20", viewModel.VisibleAppSessionRows[0].AppName);

        viewModel.RowsPerPage = 25;

        Assert.Equal(2, viewModel.CurrentDetailsPage);
        Assert.Equal("2 / 2", viewModel.DetailsPageText);
        DashboardSessionRow row = Assert.Single(viewModel.VisibleAppSessionRows);
        Assert.Equal("app-25", row.AppName);
        Assert.False(viewModel.NextDetailsPageCommand.CanExecute(null));
        Assert.True(viewModel.PreviousDetailsPageCommand.CanExecute(null));
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
        Assert.Equal(["example.com"], viewModel.DomainUsageChart.Labels);
        Assert.NotEmpty(viewModel.DomainUsageChart.Series);
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
                Assert.Equal("Page title hidden by privacy settings", row.PageTitle);
                Assert.Equal("11:50", row.StartedAtLocal);
                Assert.Equal("9m", row.Duration);
            },
            row =>
            {
                Assert.Equal("example.com", row.Domain);
                Assert.Equal("Page title hidden by privacy settings", row.PageTitle);
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
    public void UpdateCurrentActivity_WhenLaterPollHasNoPersistedSession_KeepsLastPersistedSession()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var viewModel = new DashboardViewModel(
            new FakeDashboardDataSource([], []),
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"));

        viewModel.UpdateCurrentActivity(new DashboardTrackingSnapshot(
            AppName: "chrome.exe",
            ProcessName: "chrome.exe",
            WindowTitle: null,
            CurrentSessionDuration: TimeSpan.Zero,
            LastPersistedSession: new DashboardPersistedSessionSnapshot(
                AppName: "Code.exe",
                ProcessName: "Code.exe",
                EndedAtUtc: now,
                Duration: TimeSpan.FromMinutes(5))));

        viewModel.UpdateCurrentActivity(new DashboardTrackingSnapshot(
            AppName: "chrome.exe",
            ProcessName: "chrome.exe",
            WindowTitle: null,
            CurrentSessionDuration: TimeSpan.FromMinutes(1),
            LastPersistedSession: null));

        Assert.Contains("Code.exe", viewModel.LastPersistedSessionText);
        Assert.Contains("5m", viewModel.LastPersistedSessionText);
    }

    [Fact]
    public void Constructor_ExposesSettings()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource([], []);
        var databaseController = new FakeDatabaseController("D:\\data\\windows-local.db");
        var viewModel = new DashboardViewModel(
            dataSource,
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"),
            databaseController: databaseController);

        Assert.True(viewModel.Settings.IsCollectionVisible);
        Assert.False(viewModel.Settings.IsSyncEnabled);
        Assert.Equal("D:\\data\\windows-local.db", viewModel.Settings.CurrentDatabasePathText);
        Assert.True(viewModel.Settings.CanClearLocalData);
    }

    [Fact]
    public void CreateLocalDatabaseCommand_UpdatesDatabaseStatusAndRefreshesDashboard()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource(
            [Session("session-1", "chrome.exe", now.AddMinutes(-10), now, isIdle: false)],
            []);
        var databaseController = new FakeDatabaseController("D:\\data\\old.db")
        {
            CreateResult = new DashboardDatabaseActionResult(true, "D:\\data\\created.db", "Created local database.")
        };
        var viewModel = new DashboardViewModel(
            dataSource,
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"),
            databaseController: databaseController);

        viewModel.CreateLocalDatabaseCommand.Execute(null);

        Assert.Equal(1, databaseController.CreateCallCount);
        Assert.Equal("D:\\data\\created.db", viewModel.Settings.CurrentDatabasePathText);
        Assert.Equal("Created local database.", viewModel.Settings.DatabaseStatusLabel);
        Assert.Equal(600_000, viewModel.TotalActiveMs);
    }

    [Fact]
    public void LoadExistingLocalDatabaseCommand_UpdatesDatabaseStatusAndRefreshesDashboard()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource(
            [Session("session-1", "Code.exe", now.AddMinutes(-5), now, isIdle: false)],
            []);
        var databaseController = new FakeDatabaseController("D:\\data\\old.db")
        {
            LoadResult = new DashboardDatabaseActionResult(true, "D:\\data\\existing.db", "Loaded existing database.")
        };
        var viewModel = new DashboardViewModel(
            dataSource,
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"),
            databaseController: databaseController);

        viewModel.LoadExistingLocalDatabaseCommand.Execute(null);

        Assert.Equal(1, databaseController.LoadCallCount);
        Assert.Equal("D:\\data\\existing.db", viewModel.Settings.CurrentDatabasePathText);
        Assert.Equal("Loaded existing database.", viewModel.Settings.DatabaseStatusLabel);
        Assert.Equal("Code.exe", viewModel.TopAppName);
    }

    [Fact]
    public void DeleteLocalDatabaseCommand_UpdatesDatabaseStatusAndRefreshesEmptyDashboard()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource([], []);
        var databaseController = new FakeDatabaseController("D:\\data\\windows-local.db")
        {
            DeleteResult = new DashboardDatabaseActionResult(true, "D:\\data\\windows-local.db", "Deleted local database and recreated an empty one.")
        };
        var viewModel = new DashboardViewModel(
            dataSource,
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"),
            databaseController: databaseController);

        viewModel.DeleteLocalDatabaseCommand.Execute(null);

        Assert.Equal(1, databaseController.DeleteCallCount);
        Assert.Equal("D:\\data\\windows-local.db", viewModel.Settings.CurrentDatabasePathText);
        Assert.Equal("Deleted local database and recreated an empty one.", viewModel.Settings.DatabaseStatusLabel);
        Assert.Equal(0, viewModel.TotalActiveMs);
    }

    [Fact]
    public void PollTrackingCommand_WhenCoordinatorThrows_KeepsTrackingRunningAndWritesRuntimeLogEvent()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var runtimeLogSink = new FakeRuntimeLogSink();
        var viewModel = new DashboardViewModel(
            new FakeDashboardDataSource([], []),
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"),
            trackingCoordinator: new ThrowingPollTrackingCoordinator(),
            runtimeLogSink: runtimeLogSink);

        viewModel.StartTrackingCommand.Execute(null);

        viewModel.PollTrackingCommand.Execute(null);

        Assert.Equal("Running", viewModel.TrackingStatusText);
        Assert.True(viewModel.StopTrackingCommand.CanExecute(null));
        DashboardEventLogRow errorRow = Assert.Single(viewModel.LiveEvents, row => row.EventType == "Runtime error");
        Assert.Contains("PollTracking failed", errorRow.Message, StringComparison.Ordinal);
        Assert.Contains("tracking poll failed", errorRow.Message, StringComparison.Ordinal);
        Assert.Contains("tracking poll failed", Assert.Single(runtimeLogSink.Exceptions).Exception.Message, StringComparison.Ordinal);
        Assert.Contains(runtimeLogSink.LogPath, errorRow.Message, StringComparison.Ordinal);
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

    private sealed class RecordingApplicationLifetime : IDashboardApplicationLifetime
    {
        public int RequestExitCallCount { get; private set; }

        public void RequestExit()
            => RequestExitCallCount++;
    }

    private sealed class FakeDatabaseController(string currentDatabasePath) : IDashboardDatabaseController
    {
        public string CurrentDatabasePath { get; private set; } = currentDatabasePath;

        public bool CanDeleteCurrentDatabase => true;

        public int CreateCallCount { get; private set; }

        public int LoadCallCount { get; private set; }

        public int DeleteCallCount { get; private set; }

        public DashboardDatabaseActionResult CreateResult { get; init; } =
            new(true, currentDatabasePath, "Created local database.");

        public DashboardDatabaseActionResult LoadResult { get; init; } =
            new(true, currentDatabasePath, "Loaded existing database.");

        public DashboardDatabaseActionResult DeleteResult { get; init; } =
            new(true, currentDatabasePath, "Deleted local database.");

        public DashboardDatabaseActionResult CreateNewDatabase()
        {
            CreateCallCount++;
            CurrentDatabasePath = CreateResult.DatabasePath;
            return CreateResult;
        }

        public DashboardDatabaseActionResult LoadExistingDatabase()
        {
            LoadCallCount++;
            CurrentDatabasePath = LoadResult.DatabasePath;
            return LoadResult;
        }

        public DashboardDatabaseActionResult DeleteCurrentDatabase()
        {
            DeleteCallCount++;
            CurrentDatabasePath = DeleteResult.DatabasePath;
            return DeleteResult;
        }
    }

    private sealed class FakeRuntimeLogSink : IDashboardRuntimeLogSink
    {
        public string LogPath { get; } = "D:\\logs\\windows-runtime.log";

        public List<DashboardRuntimeLogEvent> Events { get; } = [];

        public List<(string Operation, Exception Exception)> Exceptions { get; } = [];

        public void WriteEvent(DashboardRuntimeLogEvent logEvent)
            => Events.Add(logEvent);

        public void WriteException(string operation, Exception exception)
            => Exceptions.Add((operation, exception));
    }

    private sealed class ThrowingPollTrackingCoordinator : IDashboardTrackingCoordinator
    {
        public DashboardTrackingSnapshot StartTracking()
            => new(
                AppName: "chrome.exe",
                ProcessName: "chrome.exe",
                WindowTitle: "GitHub - Chrome",
                CurrentSessionDuration: TimeSpan.Zero,
                LastPersistedSession: null,
                CurrentBrowserDomain: "github.com",
                BrowserCaptureStatus: DashboardBrowserCaptureStatus.UiAutomationFallbackActive,
                LastPollAtUtc: new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero));

        public DashboardTrackingSnapshot StopTracking()
            => DashboardTrackingSnapshot.Empty;

        public DashboardTrackingSnapshot PollOnce()
            => throw new InvalidOperationException("tracking poll failed while reading Chrome.");

        public DashboardSyncResult SyncNow(bool syncEnabled)
            => new("Sync skipped. Enable sync to upload.");
    }
}
