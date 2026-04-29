using System.Text;
using System.Text.Json;
using Woong.MonitorStack.Windows.Browser;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.Tests.Browser;

public sealed class ChromeNativeMessageHostRunnerTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"woong-native-host-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task RunUntilEndAsync_ProcessesNativeMessagesUntilEndOfStream()
    {
        string connectionString = $"Data Source={_dbPath};Pooling=False";
        var rawEvents = new SqliteBrowserRawEventRepository(connectionString);
        var webSessions = new SqliteWebSessionRepository(connectionString);
        var outbox = new SqliteSyncOutboxRepository(connectionString);
        rawEvents.Initialize();
        webSessions.Initialize();
        outbox.Initialize();
        var flow = new ChromeNativeMessageIngestionFlow(
            rawEvents,
            webSessions,
            outbox,
            deviceId: "windows-device-1",
            sessionizer: new BrowserWebSessionizer("native-host-focus"),
            urlSanitizer: new BrowserUrlSanitizer(),
            storagePolicy: BrowserUrlStoragePolicy.DomainOnly);
        var runner = new ChromeNativeMessageHostRunner(flow);

        using Stream stream = CreateNativeMessageStream(
            CreatePayload("https://github.com/kimwoonggon/repo", "GitHub", 7, 42, new DateTimeOffset(2026, 4, 29, 0, 0, 0, TimeSpan.Zero)),
            CreatePayload("https://chatgpt.com/c/abc", "ChatGPT", 7, 42, new DateTimeOffset(2026, 4, 29, 0, 10, 0, TimeSpan.Zero)));

        await runner.RunUntilEndAsync(stream, CancellationToken.None);

        IReadOnlyList<BrowserRawEventRecord> rawRows = rawEvents.QueryRecordsByTabId(42);
        Assert.Equal(["github.com", "chatgpt.com"], rawRows.Select(row => row.Domain));
        Assert.All(rawRows, row => Assert.Null(row.Url));

        var persistedWebSession = Assert.Single(webSessions.QueryByFocusSessionId("native-host-focus"));
        Assert.Equal("github.com", persistedWebSession.Domain);
        Assert.Null(persistedWebSession.Url);
        Assert.Equal(600_000, persistedWebSession.DurationMs);

        SyncOutboxItem outboxItem = Assert.Single(outbox.QueryAll());
        Assert.Equal("web_session", outboxItem.AggregateType);
        Assert.Contains("github.com", outboxItem.PayloadJson);
        Assert.DoesNotContain("kimwoonggon/repo", outboxItem.PayloadJson);
    }

    [Fact]
    public async Task RunUntilEndAsync_WhenStreamIsEmpty_CompletesWithoutRows()
    {
        string connectionString = $"Data Source={_dbPath};Pooling=False";
        var rawEvents = new SqliteBrowserRawEventRepository(connectionString);
        var webSessions = new SqliteWebSessionRepository(connectionString);
        rawEvents.Initialize();
        webSessions.Initialize();
        var flow = new ChromeNativeMessageIngestionFlow(
            rawEvents,
            webSessions,
            new BrowserWebSessionizer("native-host-focus"));
        var runner = new ChromeNativeMessageHostRunner(flow);

        using var stream = new MemoryStream();

        await runner.RunUntilEndAsync(stream, CancellationToken.None);

        Assert.Empty(rawEvents.QueryRecordsByTabId(42));
        Assert.Empty(webSessions.QueryByFocusSessionId("native-host-focus"));
    }

    public void Dispose()
    {
        try
        {
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
        }
    }

    private static string CreatePayload(
        string url,
        string title,
        int windowId,
        int tabId,
        DateTimeOffset observedAtUtc)
        => JsonSerializer.Serialize(new
        {
            type = "activeTabChanged",
            browserFamily = "Chrome",
            windowId,
            tabId,
            url,
            title,
            observedAtUtc
        });

    private static Stream CreateNativeMessageStream(params string[] payloads)
    {
        var stream = new MemoryStream();
        foreach (string payload in payloads)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            stream.Write(BitConverter.GetBytes(bytes.Length));
            stream.Write(bytes);
        }

        stream.Position = 0;
        return stream;
    }
}
