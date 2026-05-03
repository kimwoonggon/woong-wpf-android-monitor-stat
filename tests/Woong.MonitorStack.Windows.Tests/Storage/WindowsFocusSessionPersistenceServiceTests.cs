using System.Text.Json;
using Microsoft.Data.Sqlite;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.Storage;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.Tests.Storage;

public sealed class WindowsFocusSessionPersistenceServiceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    [Fact]
    public void SaveFocusSession_StoresSqliteSessionAndQueuesPrivacySafeOutboxPayload()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 28, 0, 15, 0, TimeSpan.Zero));
        var focusRepository = new SqliteFocusSessionRepository($"Data Source={_dbPath};Pooling=False");
        var outboxRepository = new SqliteSyncOutboxRepository($"Data Source={_dbPath};Pooling=False");
        var service = new WindowsFocusSessionPersistenceService(focusRepository, outboxRepository, clock);
        var session = FocusSession.FromUtc(
            clientSessionId: "session-1",
            deviceId: "windows-device-1",
            platformAppKey: "Code.exe",
            startedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "foreground_window",
            processId: 1234,
            processName: "Code.exe",
            processPath: @"C:\Apps\Code.exe",
            windowHandle: 9876,
            windowTitle: "Sensitive planning doc - Code");

        focusRepository.Initialize();
        outboxRepository.Initialize();

        WindowsFocusSessionPersistenceResult result = service.SaveFocusSession(session);

        Assert.Equal(clock.UtcNow, result.PersistedAtUtc);
        FocusSession saved = Assert.Single(focusRepository.QueryByRange(
            new DateTimeOffset(2026, 4, 27, 23, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero)));
        Assert.Equal("session-1", saved.ClientSessionId);
        Assert.Equal("windows-device-1", saved.DeviceId);
        Assert.Equal("Code.exe", saved.PlatformAppKey);
        Assert.Equal(1234, saved.ProcessId);
        Assert.Equal("Code.exe", saved.ProcessName);
        Assert.Equal(@"C:\Apps\Code.exe", saved.ProcessPath);
        Assert.Equal(9876, saved.WindowHandle);
        Assert.Null(saved.WindowTitle);

        SyncOutboxItem item = Assert.Single(outboxRepository.QueryAll());
        Assert.Equal("focus-session:session-1", item.Id);
        Assert.Equal("focus_session", item.AggregateType);
        Assert.Equal("session-1", item.AggregateId);
        Assert.Equal(SyncOutboxStatus.Pending, item.Status);
        Assert.Equal(0, item.RetryCount);
        Assert.Equal(clock.UtcNow, item.CreatedAtUtc);
        Assert.Null(item.SyncedAtUtc);

        var request = JsonSerializer.Deserialize<UploadFocusSessionsRequest>(
            item.PayloadJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(request);
        Assert.Equal("windows-device-1", request.DeviceId);
        FocusSessionUploadItem payload = Assert.Single(request.Sessions);
        Assert.Equal("session-1", payload.ClientSessionId);
        Assert.Equal("Code.exe", payload.PlatformAppKey);
        Assert.Equal(1234, payload.ProcessId);
        Assert.Equal("Code.exe", payload.ProcessName);
        Assert.Equal(@"C:\Apps\Code.exe", payload.ProcessPath);
        Assert.Equal(9876, payload.WindowHandle);
        Assert.Null(payload.WindowTitle);
    }

    [Fact]
    public void SaveFocusSession_WhenOutboxInsertFails_RollsBackLocalSession()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 28, 0, 15, 0, TimeSpan.Zero));
        var focusRepository = new SqliteFocusSessionRepository($"Data Source={_dbPath};Pooling=False");
        var outboxRepository = new SqliteSyncOutboxRepository($"Data Source={_dbPath};Pooling=False");
        var service = new WindowsFocusSessionPersistenceService(focusRepository, outboxRepository, clock);
        var session = FocusSession.FromUtc(
            clientSessionId: "session-outbox-fails",
            deviceId: "windows-device-1",
            platformAppKey: "Code.exe",
            startedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "foreground_window");

        focusRepository.Initialize();
        CreateRejectingOutboxTable();

        Assert.Throws<InvalidOperationException>(() => service.SaveFocusSession(session));

        Assert.Empty(focusRepository.QueryByRange(
            new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero)));
        Assert.Empty(outboxRepository.QueryAll());
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private void CreateRejectingOutboxTable()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath};Pooling=False");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE sync_outbox (
                id TEXT NOT NULL PRIMARY KEY,
                aggregate_type TEXT NOT NULL,
                aggregate_id TEXT NOT NULL,
                payload_json TEXT NOT NULL,
                status INTEGER NOT NULL,
                retry_count INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL,
                synced_at_utc TEXT NULL,
                last_error TEXT NULL,
                required_marker TEXT NOT NULL
            );
            """;
        _ = command.ExecuteNonQuery();
    }
}
