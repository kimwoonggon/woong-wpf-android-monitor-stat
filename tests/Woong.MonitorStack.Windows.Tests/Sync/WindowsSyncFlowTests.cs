using System.Net;
using Woong.MonitorStack.Windows.Storage;
using Woong.MonitorStack.Windows.Sync;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.Tests.Sync;

public sealed class WindowsSyncFlowTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    [Fact]
    public async Task ProcessPendingAsync_UploadsSqliteOutboxPayloadThroughHttpClient()
    {
        var repository = new SqliteSyncOutboxRepository($"Data Source={_dbPath};Pooling=False");
        repository.Initialize();
        var item = SyncOutboxItem.Pending(
            id: "outbox-1",
            aggregateType: "focus_session",
            aggregateId: "session-1",
            payloadJson: """{"deviceId":"device-1","sessions":[{"clientSessionId":"session-1"}]}""",
            createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));
        repository.Add(item);
        var handler = new CapturingHandler(
            """{"items":[{"clientId":"session-1","status":1,"errorMessage":null}]}""");
        using var httpClient = new HttpClient(handler);
        var apiClient = new HttpWindowsSyncApiClient(
            httpClient,
            new WindowsSyncClientOptions(
                new Uri("https://monitor.example"),
                "device-token-1"));
        var syncedAtUtc = new DateTimeOffset(2026, 4, 28, 4, 0, 0, TimeSpan.Zero);
        var worker = new WindowsSyncWorker(repository, apiClient, new FakeClock(syncedAtUtc));

        WindowsSyncResult result = await worker.ProcessPendingAsync();

        Assert.Equal(1, result.SyncedCount);
        Assert.Equal("""{"deviceId":"device-1","sessions":[{"clientSessionId":"session-1"}]}""", handler.Body);
        Assert.Equal("/api/focus-sessions/upload", handler.Request!.RequestUri!.AbsolutePath);
        SyncOutboxItem saved = Assert.Single(repository.QueryAll());
        Assert.Equal(SyncOutboxStatus.Synced, saved.Status);
        Assert.Equal(syncedAtUtc, saved.SyncedAtUtc);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public CapturingHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson)
            };
        }
    }

    private sealed class FakeClock : ISystemClock
    {
        public FakeClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}
