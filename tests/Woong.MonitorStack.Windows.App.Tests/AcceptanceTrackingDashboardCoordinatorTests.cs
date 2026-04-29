using System.IO;
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
        DashboardTrackingSnapshot stopped = coordinator.StopTracking();

        Assert.Equal("Code.exe", started.AppName);
        Assert.Equal("chrome.exe", afterGeneratedActivity.AppName);
        Assert.Equal("Code.exe", afterGeneratedActivity.LastPersistedSession?.AppName);
        Assert.Equal("chrome.exe", stopped.LastPersistedSession?.AppName);

        var focusSessions = focusRepository.QueryByRange(
            clock.ScenarioStartedAtUtc.AddMinutes(-1),
            clock.ScenarioStartedAtUtc.AddMinutes(20));
        Assert.Contains(focusSessions, session => session.PlatformAppKey == "Code.exe" && session.DurationMs == 300_000);
        Assert.Contains(focusSessions, session => session.PlatformAppKey == "chrome.exe" && session.DurationMs == 600_000);

        var webSessions = focusSessions
            .SelectMany(session => webRepository.QueryByFocusSessionId(session.ClientSessionId))
            .ToList();
        Assert.Contains(webSessions, session => session.Domain == "github.com" && session.DurationMs == 300_000);
        Assert.Contains(webSessions, session => session.Domain == "chatgpt.com" && session.DurationMs == 300_000);

        Assert.Equal("Sync skipped. Enable sync to upload.", coordinator.SyncNow(syncEnabled: false).StatusText);

        DashboardSyncResult syncResult = coordinator.SyncNow(syncEnabled: true);

        Assert.Contains("Fake sync completed", syncResult.StatusText, StringComparison.Ordinal);
        Assert.All(outboxRepository.QueryAll(), item => Assert.Equal(SyncOutboxStatus.Synced, item.Status));
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
