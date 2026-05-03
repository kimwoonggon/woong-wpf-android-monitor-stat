using System.Text.Json;
using Microsoft.Data.Sqlite;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.Storage;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.Tests.Storage;

public sealed class WindowsWebSessionPersistenceServiceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    [Fact]
    public void SaveWebSession_StoresSqliteSessionAndQueuesPrivacySafeOutboxPayload()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 28, 0, 15, 0, TimeSpan.Zero));
        var webRepository = new SqliteWebSessionRepository($"Data Source={_dbPath};Pooling=False");
        var outboxRepository = new SqliteSyncOutboxRepository($"Data Source={_dbPath};Pooling=False");
        var service = new WindowsWebSessionPersistenceService(webRepository, outboxRepository, clock);
        var session = new WebSession(
            focusSessionId: "focus-1",
            browserFamily: "Chrome",
            url: "https://github.com/org/repo?secret=1",
            domain: "github.com",
            pageTitle: "Private repo issue",
            range: TimeRange.FromUtc(
                new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 28, 0, 5, 0, TimeSpan.Zero)),
            captureMethod: "UIAutomationAddressBar",
            captureConfidence: "High",
            isPrivateOrUnknown: false);

        webRepository.Initialize();
        outboxRepository.Initialize();

        WindowsWebSessionPersistenceResult result = service.SaveWebSession(session, "windows-device-1");

        Assert.Equal(clock.UtcNow, result.PersistedAtUtc);
        WebSession saved = Assert.Single(webRepository.QueryByFocusSessionId("focus-1"));
        Assert.Equal("focus-1", saved.FocusSessionId);
        Assert.Equal("Chrome", saved.BrowserFamily);
        Assert.Null(saved.Url);
        Assert.Equal("github.com", saved.Domain);
        Assert.Null(saved.PageTitle);
        Assert.Equal(300_000, saved.DurationMs);
        Assert.Equal("UIAutomationAddressBar", saved.CaptureMethod);
        Assert.Equal("High", saved.CaptureConfidence);
        Assert.False(saved.IsPrivateOrUnknown);

        SyncOutboxItem item = Assert.Single(outboxRepository.QueryAll());
        Assert.StartsWith("web-session:focus-1:20260428000000", item.Id, StringComparison.Ordinal);
        Assert.Equal("web_session", item.AggregateType);
        Assert.Equal(SyncOutboxStatus.Pending, item.Status);
        Assert.Equal(0, item.RetryCount);
        Assert.Equal(clock.UtcNow, item.CreatedAtUtc);
        Assert.Null(item.SyncedAtUtc);

        var request = JsonSerializer.Deserialize<UploadWebSessionsRequest>(
            item.PayloadJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(request);
        Assert.Equal("windows-device-1", request.DeviceId);
        WebSessionUploadItem payload = Assert.Single(request.Sessions);
        Assert.Equal(item.AggregateId, payload.ClientSessionId);
        Assert.Equal("focus-1", payload.FocusSessionId);
        Assert.Equal("Chrome", payload.BrowserFamily);
        Assert.Null(payload.Url);
        Assert.Equal("github.com", payload.Domain);
        Assert.Null(payload.PageTitle);
        Assert.Equal(300_000, payload.DurationMs);
        Assert.Equal("UIAutomationAddressBar", payload.CaptureMethod);
        Assert.Equal("High", payload.CaptureConfidence);
        Assert.False(payload.IsPrivateOrUnknown);
    }

    [Fact]
    public void SaveWebSession_WhenSameFocusAndStartIsSavedTwice_DoesNotDuplicateSqliteOrOutboxRows()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 28, 0, 15, 0, TimeSpan.Zero));
        var webRepository = new SqliteWebSessionRepository($"Data Source={_dbPath};Pooling=False");
        var outboxRepository = new SqliteSyncOutboxRepository($"Data Source={_dbPath};Pooling=False");
        var service = new WindowsWebSessionPersistenceService(webRepository, outboxRepository, clock);
        var session = new WebSession(
            focusSessionId: "focus-duplicate",
            browserFamily: "Chrome",
            url: "https://github.com/org/private-repo/issues/1?token=secret",
            domain: "github.com",
            pageTitle: "Sensitive issue title",
            range: TimeRange.FromUtc(
                new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 28, 0, 5, 0, TimeSpan.Zero)),
            captureMethod: "UIAutomationAddressBar",
            captureConfidence: "High",
            isPrivateOrUnknown: false);

        webRepository.Initialize();
        outboxRepository.Initialize();

        WindowsWebSessionPersistenceResult first = service.SaveWebSession(session, "windows-device-1");
        WindowsWebSessionPersistenceResult second = service.SaveWebSession(session, "windows-device-1");

        Assert.Equal(first.AggregateId, second.AggregateId);
        WebSession saved = Assert.Single(webRepository.QueryByFocusSessionId("focus-duplicate"));
        Assert.Null(saved.Url);
        Assert.Null(saved.PageTitle);
        Assert.Equal("github.com", saved.Domain);
        Assert.Equal(300_000, saved.DurationMs);

        SyncOutboxItem item = Assert.Single(outboxRepository.QueryAll());
        Assert.Equal($"web-session:{first.AggregateId}", item.Id);
        Assert.Equal(first.AggregateId, item.AggregateId);
        Assert.Equal("web_session", item.AggregateType);
        Assert.Equal(SyncOutboxStatus.Pending, item.Status);
        Assert.Equal(0, item.RetryCount);
        Assert.Null(item.SyncedAtUtc);
    }

    [Fact]
    public void SaveWebSession_WhenOutboxInsertFails_RollsBackLocalSession()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 28, 0, 15, 0, TimeSpan.Zero));
        var webRepository = new SqliteWebSessionRepository($"Data Source={_dbPath};Pooling=False");
        var outboxRepository = new SqliteSyncOutboxRepository($"Data Source={_dbPath};Pooling=False");
        var service = new WindowsWebSessionPersistenceService(webRepository, outboxRepository, clock);
        var session = new WebSession(
            focusSessionId: "focus-outbox-fails",
            browserFamily: "Chrome",
            url: "https://github.com/org/private-repo/issues/1?token=secret",
            domain: "github.com",
            pageTitle: "Sensitive issue title",
            range: TimeRange.FromUtc(
                new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 28, 0, 5, 0, TimeSpan.Zero)),
            captureMethod: "UIAutomationAddressBar",
            captureConfidence: "High",
            isPrivateOrUnknown: false);

        webRepository.Initialize();
        CreateRejectingOutboxTable();

        Assert.Throws<InvalidOperationException>(() => service.SaveWebSession(session, "windows-device-1"));

        Assert.Empty(webRepository.QueryByFocusSessionId("focus-outbox-fails"));
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
