using System.IO;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.App.Dashboard;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class SqliteDashboardDataSourceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    [Fact]
    public void QueryFocusSessions_ReadsPersistedSqliteSessions()
    {
        SqliteFocusSessionRepository focusRepository = CreateFocusRepository();
        var dataSource = new SqliteDashboardDataSource(
            focusRepository,
            CreateWebRepository());
        FocusSession session = FocusSession.FromUtc(
            "focus-1",
            "windows-device-1",
            "chrome.exe",
            new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 0, 5, 0, TimeSpan.Zero),
            "Asia/Seoul",
            isIdle: false,
            "foreground_window");
        focusRepository.Save(session);

        IReadOnlyList<FocusSession> sessions = dataSource.QueryFocusSessions(
            new DateTimeOffset(2026, 4, 27, 23, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero));

        var saved = Assert.Single(sessions);
        Assert.Equal("chrome.exe", saved.PlatformAppKey);
    }

    [Fact]
    public void QueryWebSessions_ReadsWebSessionsLinkedToPersistedFocusSessions()
    {
        SqliteFocusSessionRepository focusRepository = CreateFocusRepository();
        SqliteWebSessionRepository webRepository = CreateWebRepository();
        var dataSource = new SqliteDashboardDataSource(focusRepository, webRepository);
        FocusSession focusSession = FocusSession.FromUtc(
            "focus-1",
            "windows-device-1",
            "chrome.exe",
            new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 0, 5, 0, TimeSpan.Zero),
            "Asia/Seoul",
            isIdle: false,
            "foreground_window");
        focusRepository.Save(focusSession);
        webRepository.Save(WebSession.FromUtc(
            "focus-1",
            "Chrome",
            "https://github.com/org/repo",
            "GitHub",
            new DateTimeOffset(2026, 4, 28, 0, 1, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 0, 4, 0, TimeSpan.Zero)));

        IReadOnlyList<WebSession> sessions = dataSource.QueryWebSessions(
            new DateTimeOffset(2026, 4, 27, 23, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero));

        var saved = Assert.Single(sessions);
        Assert.Equal("github.com", saved.Domain);
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
}
