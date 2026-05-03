using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.Browser;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.Tests.Browser;

public sealed class ChromeNativeMessageIngestionFlowTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    [Fact]
    public async Task IngestAsync_StoresRawEventAndCompletedWebSessionInWindowsLocalDb()
    {
        var connectionString = $"Data Source={_dbPath};Pooling=False";
        var rawEvents = new SqliteBrowserRawEventRepository(connectionString);
        var webSessions = new SqliteWebSessionRepository(connectionString);
        var ingestion = new ChromeNativeMessageIngestionFlow(
            rawEvents,
            webSessions,
            new BrowserWebSessionizer("focus-1"));
        rawEvents.Initialize();
        webSessions.Initialize();

        await ingestion.IngestAsync(CreateNativeMessage(
            url: "https://www.youtube.com/watch?v=abc",
            title: "Video",
            observedAtUtc: "2026-04-28T01:00:00Z",
            tabId: 42), CancellationToken.None);
        await ingestion.IngestAsync(CreateNativeMessage(
            url: "https://docs.microsoft.com/dotnet",
            title: ".NET docs",
            observedAtUtc: "2026-04-28T01:05:00Z",
            tabId: 43), CancellationToken.None);

        var savedRawEvent = Assert.Single(rawEvents.QueryByTabId(42));
        Assert.Equal("https://www.youtube.com/watch?v=abc", savedRawEvent.Url);

        var savedWebSession = Assert.Single(webSessions.QueryByFocusSessionId("focus-1"));
        Assert.Equal("youtube.com", savedWebSession.Domain);
        Assert.Equal(300_000, savedWebSession.DurationMs);
    }

    [Fact]
    public async Task IngestAsync_WhenWebSessionCompletes_EnqueuesUploadPayload()
    {
        var connectionString = $"Data Source={_dbPath};Pooling=False";
        var rawEvents = new SqliteBrowserRawEventRepository(connectionString);
        var webSessions = new SqliteWebSessionRepository(connectionString);
        var outbox = new SqliteSyncOutboxRepository(connectionString);
        var ingestion = new ChromeNativeMessageIngestionFlow(
            new SqliteBrowserIngestionRepository(connectionString),
            outbox,
            deviceId: "device-1",
            sessionizer: new BrowserWebSessionizer("focus-1"),
            urlSanitizer: new BrowserUrlSanitizer(),
            storagePolicy: BrowserUrlStoragePolicy.FullUrl);
        rawEvents.Initialize();
        webSessions.Initialize();
        outbox.Initialize();

        await ingestion.IngestAsync(CreateNativeMessage(
            url: "https://github.com/kimwoonggon/woong-wpf-android-monitor-stat",
            title: "Repository",
            observedAtUtc: "2026-04-28T01:00:00Z",
            tabId: 42), CancellationToken.None);
        await ingestion.IngestAsync(CreateNativeMessage(
            url: "https://chatgpt.com/codex",
            title: "ChatGPT",
            observedAtUtc: "2026-04-28T01:05:00Z",
            tabId: 43), CancellationToken.None);

        SyncOutboxItem item = Assert.Single(outbox.QueryAll());
        Assert.Equal("web_session", item.AggregateType);
        Assert.Equal("focus-1:202604280100000000000", item.AggregateId);

        var request = JsonSerializer.Deserialize<UploadWebSessionsRequest>(
            item.PayloadJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(request);
        WebSessionUploadItem session = Assert.Single(request.Sessions);
        Assert.Equal("device-1", request.DeviceId);
        Assert.Equal("github.com", session.Domain);
        Assert.Equal(300_000, session.DurationMs);
        Assert.Equal("BrowserExtensionFuture", session.CaptureMethod);
        Assert.Equal("High", session.CaptureConfidence);
    }

    [Fact]
    public async Task IngestAsync_WhenOutboxInsertFails_RollsBackCurrentRawEventAndCompletedWebSession()
    {
        var connectionString = $"Data Source={_dbPath};Pooling=False";
        var rawEvents = new SqliteBrowserRawEventRepository(connectionString);
        var webSessions = new SqliteWebSessionRepository(connectionString);
        var outbox = new SqliteSyncOutboxRepository(connectionString);
        var ingestion = new ChromeNativeMessageIngestionFlow(
            new SqliteBrowserIngestionRepository(connectionString),
            outbox,
            deviceId: "device-1",
            sessionizer: new BrowserWebSessionizer("focus-1"),
            urlSanitizer: new BrowserUrlSanitizer(),
            storagePolicy: BrowserUrlStoragePolicy.FullUrl);
        rawEvents.Initialize();
        webSessions.Initialize();
        outbox.Initialize();
        CreateFailingWebSessionOutboxTrigger();

        await ingestion.IngestAsync(CreateNativeMessage(
            url: "https://github.com/org/repo",
            title: "Repository",
            observedAtUtc: "2026-04-28T01:00:00Z",
            tabId: 42,
            clientEventId: "event-1"), CancellationToken.None);

        await Assert.ThrowsAsync<SqliteException>(() => ingestion.IngestAsync(CreateNativeMessage(
            url: "https://chatgpt.com/codex",
            title: "ChatGPT",
            observedAtUtc: "2026-04-28T01:05:00Z",
            tabId: 43,
            clientEventId: "event-2"), CancellationToken.None));

        Assert.Single(rawEvents.QueryRecordsByTabId(42));
        Assert.Empty(rawEvents.QueryRecordsByTabId(43));
        Assert.Empty(webSessions.QueryByFocusSessionId("focus-1"));
        Assert.Empty(outbox.QueryAll());
    }

    [Fact]
    public async Task IngestAsync_WhenSameClientEventIdIsReplayed_DoesNotPersistDuplicateRawEventOrAdvanceSessionizer()
    {
        var connectionString = $"Data Source={_dbPath};Pooling=False";
        var rawEvents = new SqliteBrowserRawEventRepository(connectionString);
        var webSessions = new SqliteWebSessionRepository(connectionString);
        var outbox = new SqliteSyncOutboxRepository(connectionString);
        var ingestion = new ChromeNativeMessageIngestionFlow(
            new SqliteBrowserIngestionRepository(connectionString),
            outbox,
            deviceId: "device-1",
            sessionizer: new BrowserWebSessionizer("focus-1"),
            urlSanitizer: new BrowserUrlSanitizer(),
            storagePolicy: BrowserUrlStoragePolicy.FullUrl);
        rawEvents.Initialize();
        webSessions.Initialize();
        outbox.Initialize();

        await ingestion.IngestAsync(CreateNativeMessage(
            url: "https://github.com/org/repo",
            title: "Repository",
            observedAtUtc: "2026-04-28T01:00:00Z",
            tabId: 42,
            clientEventId: "event-1"), CancellationToken.None);
        await ingestion.IngestAsync(CreateNativeMessage(
            url: "https://chatgpt.com/codex",
            title: "ChatGPT",
            observedAtUtc: "2026-04-28T01:01:00Z",
            tabId: 43,
            clientEventId: "event-1"), CancellationToken.None);

        BrowserRawEventRecord savedRawEvent = Assert.Single(rawEvents.QueryRecordsByTabId(42));
        Assert.Equal("event-1", savedRawEvent.ClientEventId);
        Assert.Empty(rawEvents.QueryRecordsByTabId(43));
        Assert.Empty(webSessions.QueryByFocusSessionId("focus-1"));
        Assert.Empty(outbox.QueryAll());
    }

    [Fact]
    public async Task IngestAsync_WhenOlderDuplicateEventIsReplayedAfterTabChange_DoesNotCreateExtraWebSessionOrOutbox()
    {
        var connectionString = $"Data Source={_dbPath};Pooling=False";
        var rawEvents = new SqliteBrowserRawEventRepository(connectionString);
        var webSessions = new SqliteWebSessionRepository(connectionString);
        var outbox = new SqliteSyncOutboxRepository(connectionString);
        var ingestion = new ChromeNativeMessageIngestionFlow(
            new SqliteBrowserIngestionRepository(connectionString),
            outbox,
            deviceId: "device-1",
            sessionizer: new BrowserWebSessionizer("focus-1"),
            urlSanitizer: new BrowserUrlSanitizer(),
            storagePolicy: BrowserUrlStoragePolicy.FullUrl);
        rawEvents.Initialize();
        webSessions.Initialize();
        outbox.Initialize();

        await ingestion.IngestAsync(CreateNativeMessage(
            url: "https://github.com/org/repo",
            title: "Repository",
            observedAtUtc: "2026-04-28T01:00:00Z",
            tabId: 42,
            clientEventId: "event-1"), CancellationToken.None);
        await ingestion.IngestAsync(CreateNativeMessage(
            url: "https://chatgpt.com/codex",
            title: "ChatGPT",
            observedAtUtc: "2026-04-28T01:05:00Z",
            tabId: 43,
            clientEventId: "event-2"), CancellationToken.None);
        await ingestion.IngestAsync(CreateNativeMessage(
            url: "https://github.com/org/repo",
            title: "Repository",
            observedAtUtc: "2026-04-28T01:00:00Z",
            tabId: 42,
            clientEventId: "event-1"), CancellationToken.None);

        Assert.Single(rawEvents.QueryRecordsByTabId(42));
        Assert.Single(rawEvents.QueryRecordsByTabId(43));
        WebSession savedWebSession = Assert.Single(webSessions.QueryByFocusSessionId("focus-1"));
        Assert.Equal("github.com", savedWebSession.Domain);
        SyncOutboxItem savedOutboxItem = Assert.Single(outbox.QueryAll());
        Assert.Equal("web_session", savedOutboxItem.AggregateType);
        Assert.Equal("focus-1:202604280100000000000", savedOutboxItem.AggregateId);
    }

    [Fact]
    public async Task IngestAsync_WhenPolicyIsDomainOnly_DoesNotPersistFullUrlInRawEventOrWebSession()
    {
        var connectionString = $"Data Source={_dbPath};Pooling=False";
        var rawEvents = new SqliteBrowserRawEventRepository(connectionString);
        var webSessions = new SqliteWebSessionRepository(connectionString);
        var ingestion = new ChromeNativeMessageIngestionFlow(
            rawEvents,
            webSessions,
            outbox: null,
            deviceId: null,
            sessionizer: new BrowserWebSessionizer("focus-1"),
            urlSanitizer: new BrowserUrlSanitizer(),
            storagePolicy: BrowserUrlStoragePolicy.DomainOnly);
        rawEvents.Initialize();
        webSessions.Initialize();

        await ingestion.IngestAsync(CreateNativeMessage(
            url: "https://github.com/org/repo?secret=1#token",
            title: "Repository",
            observedAtUtc: "2026-04-28T01:00:00Z",
            tabId: 42), CancellationToken.None);
        await ingestion.IngestAsync(CreateNativeMessage(
            url: "https://chatgpt.com/codex",
            title: "ChatGPT",
            observedAtUtc: "2026-04-28T01:05:00Z",
            tabId: 43), CancellationToken.None);

        BrowserRawEventRecord savedRawEvent = Assert.Single(rawEvents.QueryRecordsByTabId(42));
        Assert.Null(savedRawEvent.Url);
        Assert.Equal("github.com", savedRawEvent.Domain);

        var savedWebSession = Assert.Single(webSessions.QueryByFocusSessionId("focus-1"));
        Assert.Null(savedWebSession.Url);
        Assert.Equal("github.com", savedWebSession.Domain);
    }

    [Fact]
    public async Task IngestAsync_PrunesExpiredRawEventsUsingRetentionPolicy()
    {
        var connectionString = $"Data Source={_dbPath};Pooling=False";
        var rawEvents = new SqliteBrowserRawEventRepository(connectionString);
        var webSessions = new SqliteWebSessionRepository(connectionString);
        var ingestion = new ChromeNativeMessageIngestionFlow(
            rawEvents,
            webSessions,
            outbox: null,
            deviceId: null,
            sessionizer: new BrowserWebSessionizer("focus-1"),
            urlSanitizer: new BrowserUrlSanitizer(),
            storagePolicy: BrowserUrlStoragePolicy.DomainOnly,
            rawEventRetention: new BrowserRawEventRetentionService(rawEvents, BrowserRawEventRetentionPolicy.Default));
        rawEvents.Initialize();
        webSessions.Initialize();
        rawEvents.Save(new BrowserRawEventRecord(
            "Chrome",
            WindowId: 7,
            TabId: 99,
            Url: null,
            Title: null,
            Domain: "expired.example",
            ObservedAtUtc: new DateTimeOffset(2026, 3, 28, 23, 59, 59, TimeSpan.Zero)));

        await ingestion.IngestAsync(CreateNativeMessage(
            url: "https://github.com/org/repo",
            title: "Repository",
            observedAtUtc: "2026-04-28T00:00:00Z",
            tabId: 42), CancellationToken.None);

        Assert.Empty(rawEvents.QueryRecordsByTabId(99));
        Assert.Single(rawEvents.QueryRecordsByTabId(42));
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private static Stream CreateNativeMessage(
        string url,
        string title,
        string observedAtUtc,
        int tabId,
        string? clientEventId = null)
    {
        clientEventId ??= $"test-event:{tabId}:{observedAtUtc}";
        var json = $$"""
            {
              "type": "activeTabChanged",
              "clientEventId": "{{clientEventId}}",
              "browserFamily": "Chrome",
              "windowId": 7,
              "tabId": {{tabId}},
              "url": "{{url}}",
              "title": "{{title}}",
              "observedAtUtc": "{{observedAtUtc}}"
            }
            """;
        var payload = Encoding.UTF8.GetBytes(json);
        var stream = new MemoryStream();
        stream.Write(BitConverter.GetBytes(payload.Length));
        stream.Write(payload);
        stream.Position = 0;
        return stream;
    }

    private void CreateFailingWebSessionOutboxTrigger()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath};Pooling=False");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TRIGGER fail_web_session_outbox_insert
            BEFORE INSERT ON sync_outbox
            WHEN NEW.aggregate_type = 'web_session'
            BEGIN
                SELECT RAISE(FAIL, 'forced web session outbox failure');
            END;
            """;
        _ = command.ExecuteNonQuery();
    }
}
