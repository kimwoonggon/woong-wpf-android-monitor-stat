using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.Tests.Storage;

public sealed class SqliteFocusSessionRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    [Fact]
    public void SaveAndQueryByRange_RoundTripsFocusSession()
    {
        var repository = new SqliteFocusSessionRepository($"Data Source={_dbPath};Pooling=False");
        var session = FocusSession.FromUtc(
            clientSessionId: "session-1",
            deviceId: "windows-device-1",
            platformAppKey: "chrome.exe",
            startedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "foreground_window");

        repository.Initialize();
        repository.Save(session);

        var sessions = repository.QueryByRange(
            new DateTimeOffset(2026, 4, 27, 23, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero));

        var saved = Assert.Single(sessions);
        Assert.Equal("session-1", saved.ClientSessionId);
        Assert.Equal("chrome.exe", saved.PlatformAppKey);
        Assert.Equal(600_000, saved.DurationMs);
    }

    [Fact]
    public void Save_WhenClientSessionIdAlreadyExists_DoesNotInsertDuplicate()
    {
        var repository = new SqliteFocusSessionRepository($"Data Source={_dbPath};Pooling=False");
        var session = FocusSession.FromUtc(
            clientSessionId: "session-1",
            deviceId: "windows-device-1",
            platformAppKey: "chrome.exe",
            startedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "foreground_window");

        repository.Initialize();
        repository.Save(session);
        repository.Save(session);

        var sessions = repository.QueryByRange(
            new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero));

        Assert.Single(sessions);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
