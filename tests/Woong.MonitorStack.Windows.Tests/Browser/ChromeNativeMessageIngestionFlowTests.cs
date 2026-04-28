using System.Text;
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
        int tabId)
    {
        var json = $$"""
            {
              "type": "activeTabChanged",
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
}
