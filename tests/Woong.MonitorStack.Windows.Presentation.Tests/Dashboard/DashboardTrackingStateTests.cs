using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.Presentation.Tests.Dashboard;

public sealed class DashboardTrackingStateTests
{
    [Fact]
    public void Constructor_PublishesSafeStoppedTrackingState()
    {
        DashboardViewModel viewModel = CreateViewModel();

        Assert.Equal("Stopped", viewModel.TrackingStatusText);
        Assert.Equal("No current app", viewModel.CurrentAppNameText);
        Assert.Equal("No process", viewModel.CurrentProcessNameText);
        Assert.Equal("Window title hidden by privacy settings", viewModel.CurrentWindowTitleText);
        Assert.Equal("00:00:00", viewModel.CurrentSessionDurationText);
        Assert.Equal("No session persisted", viewModel.LastPersistedSessionText);
        Assert.Equal("Sync is off. Data stays on this Windows device.", viewModel.LastSyncStatusText);
        Assert.Equal("Browser capture unavailable", viewModel.BrowserCaptureStatusText);
        Assert.False(viewModel.Settings.IsWindowTitleVisible);
    }

    [Fact]
    public void TrackingCommands_TransitionVisibleTrackingStatus()
    {
        var coordinator = new FakeTrackingCoordinator
        {
            StartSnapshot = new DashboardTrackingSnapshot(
                AppName: "Visual Studio Code",
                ProcessName: "Code.exe",
                WindowTitle: null,
                CurrentSessionDuration: TimeSpan.FromSeconds(1),
                LastPersistedSession: null)
        };
        DashboardViewModel viewModel = CreateViewModel(coordinator);

        viewModel.StartTrackingCommand.Execute(null);
        Assert.Equal("Running", viewModel.TrackingStatusText);
        Assert.Equal(1, coordinator.StartCount);
        Assert.Equal("Visual Studio Code", viewModel.CurrentAppNameText);

        viewModel.StopTrackingCommand.Execute(null);
        Assert.Equal("Stopped", viewModel.TrackingStatusText);
        Assert.Equal(1, coordinator.StopCount);
    }

    [Fact]
    public void SyncNowCommand_WhenSyncIsOptOut_ShowsSkippedStatus()
    {
        var coordinator = new FakeTrackingCoordinator();
        DashboardViewModel viewModel = CreateViewModel(coordinator);

        viewModel.SyncNowCommand.Execute(null);

        Assert.Equal("Sync skipped. Enable sync to upload.", viewModel.LastSyncStatusText);
        Assert.Equal(1, coordinator.SyncCount);
        Assert.False(coordinator.LastSyncEnabled);
    }

    [Fact]
    public void SyncNowCommand_WhenSyncIsEnabled_ShowsRequestedStatus()
    {
        var coordinator = new FakeTrackingCoordinator
        {
            SyncResult = new DashboardSyncResult("Fake sync queued.")
        };
        DashboardViewModel viewModel = CreateViewModel(coordinator);
        viewModel.Settings.IsSyncEnabled = true;

        viewModel.SyncNowCommand.Execute(null);

        Assert.Equal("Fake sync queued.", viewModel.LastSyncStatusText);
        Assert.Equal(1, coordinator.SyncCount);
        Assert.True(coordinator.LastSyncEnabled);
    }

    [Fact]
    public void StartTrackingCommand_WhenSyncIsEnabled_AutomaticallyRequestsSync()
    {
        var coordinator = new FakeTrackingCoordinator
        {
            SyncResult = new DashboardSyncResult("Fake sync queued after start.")
        };
        DashboardViewModel viewModel = CreateViewModel(coordinator);
        viewModel.Settings.IsSyncEnabled = true;

        viewModel.StartTrackingCommand.Execute(null);

        Assert.Equal("Running", viewModel.TrackingStatusText);
        Assert.Equal("Fake sync queued after start.", viewModel.LastSyncStatusText);
        Assert.Equal(1, coordinator.StartCount);
        Assert.Equal(1, coordinator.SyncCount);
        Assert.True(coordinator.LastSyncEnabled);
    }

    [Fact]
    public void StartTrackingCommand_WhenSyncIsOff_AutomaticallyReportsLocalOnlySkippedStatus()
    {
        var coordinator = new FakeTrackingCoordinator();
        DashboardViewModel viewModel = CreateViewModel(coordinator);

        viewModel.StartTrackingCommand.Execute(null);

        Assert.Equal("Running", viewModel.TrackingStatusText);
        Assert.Equal("Sync skipped. Enable sync to upload.", viewModel.LastSyncStatusText);
        Assert.Equal(1, coordinator.StartCount);
        Assert.Equal(1, coordinator.SyncCount);
        Assert.False(coordinator.LastSyncEnabled);
    }

    [Fact]
    public void StartTrackingCommand_AddsTrackingStartedAndFocusSessionStartedLiveEvents()
    {
        var coordinator = new FakeTrackingCoordinator
        {
            StartSnapshot = new DashboardTrackingSnapshot(
                AppName: "Chrome",
                ProcessName: "chrome.exe",
                WindowTitle: null,
                CurrentSessionDuration: TimeSpan.FromSeconds(1),
                LastPersistedSession: null,
                CurrentBrowserDomain: "github.com",
                BrowserCaptureStatus: DashboardBrowserCaptureStatus.UiAutomationFallbackActive)
        };
        DashboardViewModel viewModel = CreateViewModel(coordinator);

        viewModel.StartTrackingCommand.Execute(null);

        Assert.Contains(viewModel.LiveEvents, row => row.EventType == "Tracking started");
        Assert.Contains(viewModel.LiveEvents, row => row.EventType == "FocusSession started" && row.AppName == "Chrome");
        Assert.Contains(viewModel.LiveEvents, row => row.EventType == "WebSession started" && row.Domain == "github.com");
        Assert.Contains(viewModel.LiveEvents, row => row.EventType == "Sync skipped");
    }

    [Fact]
    public void PollTrackingCommand_WhenSessionsPersist_AddsClosedPersistedOutboxAndStartedLiveEvents()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var coordinator = new FakeTrackingCoordinator
        {
            StartSnapshot = new DashboardTrackingSnapshot(
                AppName: "Code.exe",
                ProcessName: "Code.exe",
                WindowTitle: null,
                CurrentSessionDuration: TimeSpan.Zero,
                LastPersistedSession: null),
            PollSnapshot = new DashboardTrackingSnapshot(
                AppName: "Chrome",
                ProcessName: "chrome.exe",
                WindowTitle: null,
                CurrentSessionDuration: TimeSpan.FromSeconds(5),
                LastPersistedSession: new DashboardPersistedSessionSnapshot(
                    "Code.exe",
                    "Code.exe",
                    now.AddMinutes(-1),
                    TimeSpan.FromMinutes(9)),
                CurrentBrowserDomain: "chatgpt.com",
                BrowserCaptureStatus: DashboardBrowserCaptureStatus.UiAutomationFallbackActive,
                LastPollAtUtc: now,
                LastDbWriteAtUtc: now,
                HasPersistedWebSession: true)
        };
        DashboardViewModel viewModel = CreateViewModel(coordinator);
        viewModel.StartTrackingCommand.Execute(null);

        viewModel.PollTrackingCommand.Execute(null);

        Assert.Contains(viewModel.LiveEvents, row => row.EventType == "FocusSession closed" && row.AppName == "Code.exe");
        Assert.Contains(viewModel.LiveEvents, row => row.EventType == "FocusSession persisted" && row.AppName == "Code.exe");
        Assert.Contains(viewModel.LiveEvents, row => row.EventType == "Outbox row created" && row.AppName == "Code.exe");
        Assert.Contains(viewModel.LiveEvents, row => row.EventType == "FocusSession started" && row.AppName == "Chrome");
        Assert.Contains(viewModel.LiveEvents, row => row.EventType == "WebSession closed");
        Assert.Contains(viewModel.LiveEvents, row => row.EventType == "WebSession persisted");
        Assert.Contains(viewModel.LiveEvents, row => row.EventType == "WebSession started" && row.Domain == "chatgpt.com");
    }

    [Fact]
    public void StopTrackingCommand_WhenSessionFlushes_AddsStoppedAndFlushLiveEvents()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var coordinator = new FakeTrackingCoordinator
        {
            StartSnapshot = new DashboardTrackingSnapshot(
                AppName: "Chrome",
                ProcessName: "chrome.exe",
                WindowTitle: null,
                CurrentSessionDuration: TimeSpan.Zero,
                LastPersistedSession: null),
            StopSnapshot = new DashboardTrackingSnapshot(
                AppName: "Chrome",
                ProcessName: "chrome.exe",
                WindowTitle: null,
                CurrentSessionDuration: TimeSpan.FromMinutes(3),
                LastPersistedSession: new DashboardPersistedSessionSnapshot(
                    "Chrome",
                    "chrome.exe",
                    now,
                    TimeSpan.FromMinutes(3)),
                LastPollAtUtc: now,
                LastDbWriteAtUtc: now)
        };
        DashboardViewModel viewModel = CreateViewModel(coordinator);
        viewModel.StartTrackingCommand.Execute(null);

        viewModel.StopTrackingCommand.Execute(null);

        Assert.Contains(viewModel.LiveEvents, row => row.EventType == "FocusSession closed" && row.AppName == "Chrome");
        Assert.Contains(viewModel.LiveEvents, row => row.EventType == "FocusSession persisted" && row.AppName == "Chrome");
        Assert.Contains(viewModel.LiveEvents, row => row.EventType == "Outbox row created" && row.AppName == "Chrome");
        Assert.Contains(viewModel.LiveEvents, row => row.EventType == "Tracking stopped");
    }

    [Fact]
    public void StopTrackingCommand_RefreshesCurrentDashboardPeriodAfterFlush()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new MutableDashboardDataSource();
        var coordinator = new FakeTrackingCoordinator
        {
            OnStop = () => dataSource.FocusSessions =
            [
                Domain.Common.FocusSession.FromUtc(
                    "focus-1",
                    "windows-device-1",
                    "Code.exe",
                    now.AddMinutes(-2),
                    now,
                    "Asia/Seoul",
                    isIdle: false,
                    "foreground_window")
            ]
        };
        var viewModel = new DashboardViewModel(
            dataSource,
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"),
            coordinator);
        viewModel.SelectPeriod(DashboardPeriod.LastHour);

        viewModel.StartTrackingCommand.Execute(null);
        viewModel.StopTrackingCommand.Execute(null);

        var row = Assert.Single(viewModel.RecentSessions);
        Assert.Equal("Code.exe", row.AppName);
    }

    [Fact]
    public void CurrentSessionDuration_WhenPollTicks_AdvancesBeyondZero()
    {
        var coordinator = new FakeTrackingCoordinator
        {
            StartSnapshot = new DashboardTrackingSnapshot(
                AppName: "Visual Studio Code",
                ProcessName: "Code.exe",
                WindowTitle: null,
                CurrentSessionDuration: TimeSpan.Zero,
                LastPersistedSession: null),
            PollSnapshot = new DashboardTrackingSnapshot(
                AppName: "Visual Studio Code",
                ProcessName: "Code.exe",
                WindowTitle: null,
                CurrentSessionDuration: TimeSpan.FromSeconds(12),
                LastPersistedSession: null)
        };
        DashboardViewModel viewModel = CreateViewModel(coordinator);

        viewModel.StartTrackingCommand.Execute(null);
        viewModel.PollTrackingCommand.Execute(null);

        Assert.Equal("00:00:12", viewModel.CurrentSessionDurationText);
        Assert.Equal(1, coordinator.PollCount);
    }

    [Fact]
    public void PollTrackingCommand_WhenForegroundChanged_RefreshesDashboardAfterClosedSessionPersists()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new MutableDashboardDataSource();
        var coordinator = new FakeTrackingCoordinator
        {
            OnPoll = () => dataSource.FocusSessions =
            [
                Domain.Common.FocusSession.FromUtc(
                    "focus-1",
                    "windows-device-1",
                    "Code.exe",
                    now.AddMinutes(-10),
                    now.AddMinutes(-5),
                    "Asia/Seoul",
                    isIdle: false,
                    "foreground_window")
            ],
            PollSnapshot = new DashboardTrackingSnapshot(
                AppName: "Chrome",
                ProcessName: "chrome.exe",
                WindowTitle: null,
                CurrentSessionDuration: TimeSpan.Zero,
                LastPersistedSession: new DashboardPersistedSessionSnapshot(
                    AppName: "Code.exe",
                    ProcessName: "Code.exe",
                    EndedAtUtc: now.AddMinutes(-5),
                    Duration: TimeSpan.FromMinutes(5)))
        };
        var viewModel = new DashboardViewModel(
            dataSource,
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"),
            coordinator);
        viewModel.SelectPeriod(DashboardPeriod.LastHour);

        viewModel.StartTrackingCommand.Execute(null);
        viewModel.PollTrackingCommand.Execute(null);

        var row = Assert.Single(viewModel.RecentSessions);
        Assert.Equal("Code.exe", row.AppName);
        Assert.Equal("Chrome", viewModel.CurrentAppNameText);
    }

    [Fact]
    public void PollTrackingCommand_WhenWebSessionPersistsWithoutFocusChange_RefreshesDashboard()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new MutableDashboardDataSource();
        var coordinator = new FakeTrackingCoordinator
        {
            OnPoll = () => dataSource.WebSessions =
            [
                Domain.Common.WebSession.FromUtc(
                    "focus-1",
                    "Chrome",
                    "https://github.com/org/repo",
                    "GitHub",
                    now.AddMinutes(-5),
                    now)
            ],
            PollSnapshot = new DashboardTrackingSnapshot(
                AppName: "Chrome",
                ProcessName: "chrome.exe",
                WindowTitle: null,
                CurrentSessionDuration: TimeSpan.FromMinutes(5),
                LastPersistedSession: null,
                CurrentBrowserDomain: "github.com",
                HasPersistedWebSession: true)
        };
        var viewModel = new DashboardViewModel(
            dataSource,
            new FixedClock(now),
            new DashboardOptions("Asia/Seoul"),
            coordinator);
        viewModel.SelectPeriod(DashboardPeriod.LastHour);

        viewModel.StartTrackingCommand.Execute(null);
        viewModel.PollTrackingCommand.Execute(null);

        var row = Assert.Single(viewModel.RecentWebSessions);
        Assert.Equal("github.com", row.Domain);
        Assert.Equal("github.com", viewModel.CurrentBrowserDomainText);
    }

    [Fact]
    public void UpdateCurrentActivity_WhenWindowTitleHidden_MasksWindowTitle()
    {
        DashboardViewModel viewModel = CreateViewModel();

        viewModel.UpdateCurrentActivity(new DashboardTrackingSnapshot(
            AppName: "Visual Studio Code",
            ProcessName: "Code.exe",
            WindowTitle: "Secret Project - Visual Studio Code",
            CurrentSessionDuration: TimeSpan.FromSeconds(75),
            LastPersistedSession: new DashboardPersistedSessionSnapshot(
                AppName: "Code.exe",
                ProcessName: "Code.exe",
                EndedAtUtc: new DateTimeOffset(2026, 4, 28, 12, 0, 0, TimeSpan.Zero),
                Duration: TimeSpan.FromSeconds(75))));

        Assert.Equal("Visual Studio Code", viewModel.CurrentAppNameText);
        Assert.Equal("Code.exe", viewModel.CurrentProcessNameText);
        Assert.Equal("Window title hidden by privacy settings", viewModel.CurrentWindowTitleText);
        Assert.Equal("00:01:15", viewModel.CurrentSessionDurationText);
        Assert.Equal("Code.exe persisted at 21:00 for 1m", viewModel.LastPersistedSessionText);
    }

    [Fact]
    public void UpdateCurrentActivity_WhenBrowserDomainMissing_ExplainsCaptureConnectionAndAppFocusState()
    {
        DashboardViewModel viewModel = CreateViewModel();

        viewModel.UpdateCurrentActivity(new DashboardTrackingSnapshot(
            AppName: "Chrome",
            ProcessName: "chrome.exe",
            WindowTitle: null,
            CurrentSessionDuration: TimeSpan.FromSeconds(5),
            LastPersistedSession: null,
            CurrentBrowserDomain: null));

        Assert.Equal("No browser domain yet. Connect browser capture; app focus is tracked.", viewModel.CurrentBrowserDomainText);
        Assert.Equal("Browser capture unavailable", viewModel.BrowserCaptureStatusText);
    }

    [Fact]
    public void UpdateCurrentActivity_WhenUiAutomationFallbackActive_ShowsBrowserCaptureStatus()
    {
        DashboardViewModel viewModel = CreateViewModel();

        viewModel.UpdateCurrentActivity(new DashboardTrackingSnapshot(
            AppName: "Chrome",
            ProcessName: "chrome.exe",
            WindowTitle: null,
            CurrentSessionDuration: TimeSpan.FromSeconds(5),
            LastPersistedSession: null,
            CurrentBrowserDomain: "github.com",
            BrowserCaptureStatus: DashboardBrowserCaptureStatus.UiAutomationFallbackActive));

        Assert.Equal("github.com", viewModel.CurrentBrowserDomainText);
        Assert.Equal("Domain from address bar fallback", viewModel.BrowserCaptureStatusText);
    }

    [Fact]
    public void UpdateCurrentActivity_WhenBrowserExtensionConnected_ShowsBrowserCaptureStatus()
    {
        DashboardViewModel viewModel = CreateViewModel();

        viewModel.UpdateCurrentActivity(new DashboardTrackingSnapshot(
            AppName: "Chrome",
            ProcessName: "chrome.exe",
            WindowTitle: null,
            CurrentSessionDuration: TimeSpan.FromSeconds(5),
            LastPersistedSession: null,
            CurrentBrowserDomain: "github.com",
            BrowserCaptureStatus: DashboardBrowserCaptureStatus.ExtensionConnected));

        Assert.Equal("Browser extension connected", viewModel.BrowserCaptureStatusText);
    }

    [Fact]
    public void UpdateCurrentActivity_WhenBrowserCaptureError_ShowsBrowserCaptureStatus()
    {
        DashboardViewModel viewModel = CreateViewModel();

        viewModel.UpdateCurrentActivity(new DashboardTrackingSnapshot(
            AppName: "Chrome",
            ProcessName: "chrome.exe",
            WindowTitle: null,
            CurrentSessionDuration: TimeSpan.FromSeconds(5),
            LastPersistedSession: null,
            CurrentBrowserDomain: null,
            BrowserCaptureStatus: DashboardBrowserCaptureStatus.Error));

        Assert.Equal("No browser domain yet. Connect browser capture; app focus is tracked.", viewModel.CurrentBrowserDomainText);
        Assert.Equal("Browser capture error", viewModel.BrowserCaptureStatusText);
    }

    [Fact]
    public void UpdateCurrentActivity_WhenWindowTitleVisible_ShowsWindowTitle()
    {
        DashboardViewModel viewModel = CreateViewModel();
        viewModel.Settings.IsWindowTitleVisible = true;

        viewModel.UpdateCurrentActivity(new DashboardTrackingSnapshot(
            AppName: "Chrome",
            ProcessName: "chrome.exe",
            WindowTitle: "GitHub - Chrome",
            CurrentSessionDuration: TimeSpan.FromSeconds(5),
            LastPersistedSession: null));

        Assert.Equal("GitHub - Chrome", viewModel.CurrentWindowTitleText);
        Assert.Equal("No session persisted", viewModel.LastPersistedSessionText);
    }

    [Fact]
    public void Settings_WhenWindowTitleWasCapturedWhileHidden_DoesNotRevealItLater()
    {
        DashboardViewModel viewModel = CreateViewModel();
        viewModel.UpdateCurrentActivity(new DashboardTrackingSnapshot(
            AppName: "Chrome",
            ProcessName: "chrome.exe",
            WindowTitle: "GitHub - Chrome",
            CurrentSessionDuration: TimeSpan.FromSeconds(5),
            LastPersistedSession: null));

        viewModel.Settings.IsWindowTitleVisible = true;

        Assert.Equal("No window title", viewModel.CurrentWindowTitleText);
    }

    [Fact]
    public void SelectPeriod_WhenWindowTitlesAreHidden_MasksWebPageTitles()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource(
            [],
            [
                Domain.Common.WebSession.FromUtc(
                    "focus-1",
                    "Chrome",
                    "https://github.com/org/private",
                    "Private issue title",
                    now.AddMinutes(-10),
                    now)
            ]);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        viewModel.SelectPeriod(DashboardPeriod.LastHour);

        var row = Assert.Single(viewModel.RecentWebSessions);
        Assert.Equal("github.com", row.Domain);
        Assert.Equal("Page title hidden by privacy settings", row.PageTitle);
    }

    [Fact]
    public void SelectPeriod_WhenWindowTitlesAreVisible_ShowsWebPageTitles()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource(
            [],
            [
                Domain.Common.WebSession.FromUtc(
                    "focus-1",
                    "Chrome",
                    "https://github.com/org/private",
                    "Private issue title",
                    now.AddMinutes(-10),
                    now)
            ]);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));
        viewModel.Settings.IsWindowTitleVisible = true;

        viewModel.SelectPeriod(DashboardPeriod.LastHour);

        var row = Assert.Single(viewModel.RecentWebSessions);
        Assert.Equal("Private issue title", row.PageTitle);
    }

    private static DashboardViewModel CreateViewModel(IDashboardTrackingCoordinator? coordinator = null)
        => new(
            new EmptyDataSource(),
            new FixedClock(new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero)),
            new DashboardOptions("Asia/Seoul"),
            coordinator);

    private class FakeDashboardDataSource(
        IReadOnlyList<Domain.Common.FocusSession> focusSessions,
        IReadOnlyList<Domain.Common.WebSession> webSessions) : IDashboardDataSource
    {
        public IReadOnlyList<Domain.Common.FocusSession> QueryFocusSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => focusSessions;

        public IReadOnlyList<Domain.Common.WebSession> QueryWebSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => webSessions;
    }

    private sealed class MutableDashboardDataSource : IDashboardDataSource
    {
        public IReadOnlyList<Domain.Common.FocusSession> FocusSessions { get; set; } = [];

        public IReadOnlyList<Domain.Common.WebSession> WebSessions { get; set; } = [];

        public IReadOnlyList<Domain.Common.FocusSession> QueryFocusSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => FocusSessions;

        public IReadOnlyList<Domain.Common.WebSession> QueryWebSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => WebSessions;
    }

    private sealed class EmptyDataSource : FakeDashboardDataSource
    {
        public EmptyDataSource()
            : base([], [])
        {
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IDashboardClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private sealed class FakeTrackingCoordinator : IDashboardTrackingCoordinator
    {
        public DashboardTrackingSnapshot StartSnapshot { get; set; } = DashboardTrackingSnapshot.Empty;

        public DashboardTrackingSnapshot StopSnapshot { get; set; } = DashboardTrackingSnapshot.Empty;

        public DashboardTrackingSnapshot PollSnapshot { get; set; } = DashboardTrackingSnapshot.Empty;

        public DashboardSyncResult SyncResult { get; set; } = new("Sync skipped. Enable sync to upload.");

        public Action? OnStop { get; set; }

        public Action? OnPoll { get; set; }

        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public int PollCount { get; private set; }

        public int SyncCount { get; private set; }

        public bool LastSyncEnabled { get; private set; }

        public DashboardTrackingSnapshot StartTracking()
        {
            StartCount++;

            return StartSnapshot;
        }

        public DashboardTrackingSnapshot StopTracking()
        {
            StopCount++;
            OnStop?.Invoke();

            return StopSnapshot;
        }

        public DashboardTrackingSnapshot PollOnce()
        {
            PollCount++;
            OnPoll?.Invoke();

            return PollSnapshot;
        }

        public DashboardSyncResult SyncNow(bool syncEnabled)
        {
            SyncCount++;
            LastSyncEnabled = syncEnabled;

            return syncEnabled
                ? SyncResult
                : new DashboardSyncResult("Sync skipped. Enable sync to upload.");
        }
    }
}
