using System.Net;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.Storage;
using Woong.MonitorStack.Windows.Sync;

namespace Woong.MonitorStack.Windows.Tests.Sync;

public sealed class HttpWindowsSyncApiClientTests
{
    [Fact]
    public async Task UploadAsync_PostsOutboxPayloadToMatchingEndpointWithDeviceToken()
    {
        var handler = new CapturingHandler(
            """{"items":[{"clientId":"session-1","status":1,"errorMessage":null}]}""");
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://monitor.example")
        };
        var client = new HttpWindowsSyncApiClient(
            httpClient,
            new WindowsSyncClientOptions(deviceToken: "device-token-1"));
        var item = SyncOutboxItem.Pending(
            id: "outbox-1",
            aggregateType: "focus_session",
            aggregateId: "session-1",
            payloadJson: """{"deviceId":"device-1","sessions":[]}""",
            createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));

        UploadBatchResult result = await client.UploadAsync(item);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("/api/focus-sessions/upload", handler.Request.RequestUri!.AbsolutePath);
        Assert.Equal("device-token-1", Assert.Single(handler.Request.Headers.GetValues("X-Device-Token")));
        Assert.Equal("""{"deviceId":"device-1","sessions":[]}""", handler.Body);
        UploadItemResult uploadResult = Assert.Single(result.Items);
        Assert.Equal("session-1", uploadResult.ClientId);
        Assert.Equal(UploadItemStatus.Accepted, uploadResult.Status);
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
}
