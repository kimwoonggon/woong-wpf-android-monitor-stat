using System.IO;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.App.Dashboard;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class AcceptanceTrackingDashboardCoordinatorTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    [Fact]
    public void TrackingPipelineMode_StartPollStop_PersistsFocusWebSessionsAndFakeSyncsOutbox()
    {
        SqliteFocusSessionRepository focusRepository = CreateFocusRepository();
        SqliteWebSessionRepository webRepository = CreateWebRepository();
        SqliteSyncOutboxRepository outboxRepository = CreateOutboxRepository();
        var clock = new AcceptanceTrackingScenarioClock();
        var coordinator = new AcceptanceTrackingDashboardCoordinator(
            focusRepository,
            webRepository,
            outboxRepository,
            clock,
            "acceptance-device",
            "Asia/Seoul");

        DashboardTrackingSnapshot started = coordinator.StartTracking();
        DashboardTrackingSnapshot afterGeneratedActivity = coordinator.PollOnce();
        _ = coordinator.PollOnce();
        _ = coordinator.PollOnce();
        DashboardTrackingSnapshot stopped = coordinator.StopTracking();

        Assert.Equal("Code.exe", started.AppName);
        Assert.Equal("chrome.exe", afterGeneratedActivity.AppName);
        Assert.Equal("Code.exe", afterGeneratedActivity.LastPersistedSession?.AppName);
        Assert.True(
            new[] { "chrome.exe", "notepad.exe", "explorer.exe" }.Contains(stopped.LastPersistedSession?.AppName),
            $"Unexpected last persisted app: {stopped.LastPersistedSession?.AppName}");

        var focusSessions = focusRepository.QueryByRange(
            clock.ScenarioStartedAtUtc.AddMinutes(-1),
            clock.ScenarioStartedAtUtc.AddMinutes(20));
        Assert.Contains(focusSessions, session => session.PlatformAppKey == "Code.exe" && session.DurationMs == 180_000);
        Assert.Contains(focusSessions, session => session.PlatformAppKey == "chrome.exe" && session.DurationMs == 360_000);

        var webSessions = focusSessions
            .SelectMany(session => webRepository.QueryByFocusSessionId(session.ClientSessionId))
            .ToList();
        Assert.Contains(webSessions, session => session.Domain == "youtube.com" && session.DurationMs == 60_000);
        Assert.Contains(webSessions, session => session.Domain == "github.com" && session.DurationMs == 120_000);
        Assert.Contains(webSessions, session => session.Domain == "chatgpt.com" && session.DurationMs == 180_000);
        Assert.All(webSessions, session => Assert.Null(session.Url));
        Assert.All(webSessions, session => Assert.Null(session.PageTitle));

        Assert.Equal("Sync skipped. Enable sync to upload.", coordinator.SyncNow(syncEnabled: false).StatusText);

        DashboardSyncResult syncResult = coordinator.SyncNow(syncEnabled: true);

        Assert.Contains("Fake sync completed", syncResult.StatusText, StringComparison.Ordinal);
        Assert.All(outboxRepository.QueryAll(), item => Assert.Equal(SyncOutboxStatus.Synced, item.Status));
    }

    [Fact]
    public void TrackingPipelineMode_PollsAcrossChromeWindowsAndArbitraryWindows_WithTitlesAndOutboxEvidence()
    {
        SqliteFocusSessionRepository focusRepository = CreateFocusRepository();
        SqliteWebSessionRepository webRepository = CreateWebRepository();
        SqliteSyncOutboxRepository outboxRepository = CreateOutboxRepository();
        var clock = new AcceptanceTrackingScenarioClock();
        var coordinator = new AcceptanceTrackingDashboardCoordinator(
            focusRepository,
            webRepository,
            outboxRepository,
            clock,
            "acceptance-device",
            "Asia/Seoul");

        _ = coordinator.StartTracking();
        DashboardTrackingSnapshot chromeYouTube = coordinator.PollOnce();
        DashboardTrackingSnapshot chromeGithub = coordinator.PollOnce();
        DashboardTrackingSnapshot chromeChatGpt = coordinator.PollOnce();
        DashboardTrackingSnapshot chromeDocs = coordinator.PollOnce();
        DashboardTrackingSnapshot notepad = coordinator.PollOnce();
        DashboardTrackingSnapshot explorer = coordinator.PollOnce();
        _ = coordinator.StopTracking();

        Assert.Equal("youtube.com", chromeYouTube.CurrentBrowserDomain);
        Assert.Equal("YouTube - Google Chrome", chromeYouTube.WindowTitle);
        Assert.Equal("github.com", chromeGithub.CurrentBrowserDomain);
        Assert.Equal("GitHub Repo - Google Chrome", chromeGithub.WindowTitle);
        Assert.Equal("chatgpt.com", chromeChatGpt.CurrentBrowserDomain);
        Assert.Equal("ChatGPT - Google Chrome", chromeChatGpt.WindowTitle);
        Assert.Equal("learn.microsoft.com", chromeDocs.CurrentBrowserDomain);
        Assert.Equal("Learn Microsoft - Google Chrome", chromeDocs.WindowTitle);
        Assert.Equal("notepad.exe", notepad.ProcessName);
        Assert.Equal("Untitled - Notepad", notepad.WindowTitle);
        Assert.Equal("explorer.exe", explorer.ProcessName);
        Assert.Equal("Downloads - File Explorer", explorer.WindowTitle);

        IReadOnlyList<Woong.MonitorStack.Domain.Common.FocusSession> focusSessions = focusRepository.QueryByRange(
            clock.ScenarioStartedAtUtc.AddMinutes(-1),
            clock.ScenarioStartedAtUtc.AddMinutes(20));

        Assert.Contains(focusSessions, session => session.ProcessName == "chrome.exe" && session.ProcessId == 20 && session.WindowHandle == 200 && session.WindowTitle == "YouTube - Google Chrome" && session.DurationMs == 360_000);
        Assert.Contains(focusSessions, session => session.ProcessName == "chrome.exe" && session.ProcessId == 22 && session.WindowTitle == "Learn Microsoft - Google Chrome");
        Assert.Contains(focusSessions, session => session.ProcessName == "notepad.exe" && session.WindowTitle == "Untitled - Notepad");
        Assert.Contains(focusSessions, session => session.ProcessName == "explorer.exe" && session.WindowTitle == "Downloads - File Explorer");

        IReadOnlyList<WebSession> webSessions = focusSessions
            .SelectMany(session => webRepository.QueryByFocusSessionId(session.ClientSessionId))
            .ToList();
        Assert.Contains(webSessions, session => session.Domain == "youtube.com");
        Assert.Contains(webSessions, session => session.Domain == "github.com");
        Assert.Contains(webSessions, session => session.Domain == "chatgpt.com");
        Assert.Contains(webSessions, session => session.Domain == "learn.microsoft.com");
        Assert.All(webSessions, session => Assert.Null(session.Url));
        Assert.True(outboxRepository.QueryAll().Count >= focusSessions.Count + webSessions.Count);
    }

    [Fact]
    public void TrackingPipelineMode_WhenChromeNavigatesWithinSameWindow_PersistsWebSessionsWithoutClosingFocusSession()
    {
        SqliteFocusSessionRepository focusRepository = CreateFocusRepository();
        SqliteWebSessionRepository webRepository = CreateWebRepository();
        SqliteSyncOutboxRepository outboxRepository = CreateOutboxRepository();
        var clock = new AcceptanceTrackingScenarioClock();
        var coordinator = new AcceptanceTrackingDashboardCoordinator(
            focusRepository,
            webRepository,
            outboxRepository,
            clock,
            "acceptance-device",
            "Asia/Seoul");

        _ = coordinator.StartTracking();
        _ = coordinator.PollOnce();
        DashboardTrackingSnapshot chromeGithub = coordinator.PollOnce();
        DashboardTrackingSnapshot chromeChatGpt = coordinator.PollOnce();

        IReadOnlyList<Woong.MonitorStack.Domain.Common.FocusSession> focusSessionsBeforeLeavingChrome = focusRepository.QueryByRange(
            clock.ScenarioStartedAtUtc.AddMinutes(-1),
            clock.ScenarioStartedAtUtc.AddMinutes(20));
        Assert.DoesNotContain(focusSessionsBeforeLeavingChrome, session => session.ProcessName == "chrome.exe");

        IReadOnlyList<WebSession> sameWindowWebSessions = webRepository.QueryByFocusSessionId("acceptance-chrome-primary-focus");
        Assert.Contains(sameWindowWebSessions, session => session.Domain == "youtube.com" && session.DurationMs == 60_000);
        Assert.Contains(sameWindowWebSessions, session => session.Domain == "github.com" && session.DurationMs == 120_000);
        Assert.Equal("github.com", chromeGithub.CurrentBrowserDomain);
        Assert.True(chromeGithub.HasPersistedWebSession);
        Assert.Equal("chatgpt.com", chromeChatGpt.CurrentBrowserDomain);
        Assert.True(chromeChatGpt.HasPersistedWebSession);

        _ = coordinator.PollOnce();

        IReadOnlyList<Woong.MonitorStack.Domain.Common.FocusSession> focusSessionsAfterLeavingChrome = focusRepository.QueryByRange(
            clock.ScenarioStartedAtUtc.AddMinutes(-1),
            clock.ScenarioStartedAtUtc.AddMinutes(20));
        Woong.MonitorStack.Domain.Common.FocusSession chromeFocus = Assert.Single(
            focusSessionsAfterLeavingChrome,
            session => session.ClientSessionId == "acceptance-chrome-primary-focus");
        Assert.Equal(20, chromeFocus.ProcessId);
        Assert.Equal(200, chromeFocus.WindowHandle);
        Assert.Equal(360_000, chromeFocus.DurationMs);
    }

    [Fact]
    public void AcceptanceScenarioClock_DefaultStartUsesLocalNoonToAvoidMidnightFilterFlakes()
    {
        var clock = new AcceptanceTrackingScenarioClock();
        DateTimeOffset localStart = TimeZoneInfo.ConvertTime(clock.ScenarioStartedAtUtc, TimeZoneInfo.Local);
        DateOnly localStartDate = DateOnly.FromDateTime(localStart.DateTime);
        DateOnly localToday = DateOnly.FromDateTime(DateTimeOffset.Now.DateTime);

        Assert.Equal(localToday, localStartDate);
        Assert.Equal(12, localStart.Hour);
        Assert.Equal(0, localStart.Minute);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private SqliteFocusSessionRepository CreateFocusRepository()
    {
        var repository = new SqliteFocusSessionRepository($"Data Source={_dbPath};Pooling=False");
        repository.Initialize();

        return repository;
    }

    private SqliteWebSessionRepository CreateWebRepository()
    {
        var repository = new SqliteWebSessionRepository($"Data Source={_dbPath};Pooling=False");
        repository.Initialize();

        return repository;
    }

    private SqliteSyncOutboxRepository CreateOutboxRepository()
    {
        var repository = new SqliteSyncOutboxRepository($"Data Source={_dbPath};Pooling=False");
        repository.Initialize();

        return repository;
    }
}
