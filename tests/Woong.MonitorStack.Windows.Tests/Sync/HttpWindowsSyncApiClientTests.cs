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
        using var httpClient = new HttpClient(handler);
        var client = new HttpWindowsSyncApiClient(
            httpClient,
            new WindowsSyncClientOptions(
                new Uri("https://monitor.example"),
                deviceToken: "device-token-1"));
        var item = SyncOutboxItem.Pending(
            id: "outbox-1",
            aggregateType: "focus_session",
            aggregateId: "session-1",
            payloadJson: """{"deviceId":"device-1","sessions":[]}""",
            createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));

        UploadBatchResult result = await client.UploadAsync(item);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://monitor.example/api/focus-sessions/upload", handler.Request.RequestUri!.AbsoluteUri);
        Assert.Equal("/api/focus-sessions/upload", handler.Request.RequestUri!.AbsolutePath);
        Assert.Equal("device-token-1", Assert.Single(handler.Request.Headers.GetValues("X-Device-Token")));
        Assert.Equal("""{"deviceId":"device-1","sessions":[]}""", handler.Body);
        UploadItemResult uploadResult = Assert.Single(result.Items);
        Assert.Equal("session-1", uploadResult.ClientId);
        Assert.Equal(UploadItemStatus.Accepted, uploadResult.Status);
    }

    [Fact]
    public async Task UploadAsync_ReadsTokenAndServerDeviceIdFromStoresAtSendTime()
    {
        var handler = new CapturingHandler(
            """{"items":[{"clientId":"session-1","status":1,"errorMessage":null}]}""");
        using var httpClient = new HttpClient(handler);
        var tokenStore = new MutableTokenStore("stored-token-before-send");
        var registrationStore = new MutableRegistrationStore("server-device-before-send");
        var client = new HttpWindowsSyncApiClient(
            httpClient,
            new WindowsSyncClientOptions(
                new Uri("https://monitor.example"),
                deviceToken: "raw-ui-token"),
            tokenStore,
            registrationStore);
        tokenStore.DeviceToken = "stored-token-at-send";
        registrationStore.ServerDeviceId = "server-device-at-send";
        var item = SyncOutboxItem.Pending(
            id: "outbox-1",
            aggregateType: "focus_session",
            aggregateId: "session-1",
            payloadJson: """{"deviceId":"local-windows-device","sessions":[{"clientSessionId":"session-1"}]}""",
            createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));

        _ = await client.UploadAsync(item);

        Assert.Equal("stored-token-at-send", Assert.Single(handler.Request!.Headers.GetValues("X-Device-Token")));
        Assert.DoesNotContain("raw-ui-token", handler.Request.Headers.GetValues("X-Device-Token"));
        Assert.Contains("\"deviceId\":\"server-device-at-send\"", handler.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("local-windows-device", handler.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("stored-token-at-send", handler.Body, StringComparison.Ordinal);
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

    private sealed class MutableTokenStore : IWindowsSyncTokenStore
    {
        public MutableTokenStore(string? deviceToken)
        {
            DeviceToken = deviceToken;
        }

        public string? DeviceToken { get; set; }

        public string? GetDeviceToken()
            => DeviceToken;

        public void SaveDeviceToken(string deviceToken)
        {
            DeviceToken = deviceToken;
        }

        public void DeleteDeviceToken()
        {
            DeviceToken = null;
        }
    }

    private sealed class MutableRegistrationStore : IWindowsSyncRegistrationStore
    {
        public MutableRegistrationStore(string serverDeviceId)
        {
            ServerDeviceId = serverDeviceId;
        }

        public string? ServerDeviceId { get; set; }

        public WindowsSyncRegistration? GetRegistration()
            => string.IsNullOrWhiteSpace(ServerDeviceId)
                ? null
                : new WindowsSyncRegistration(ServerDeviceId);

        public void SaveRegistration(WindowsSyncRegistration registration)
        {
            ServerDeviceId = registration.ServerDeviceId;
        }

        public void ClearRegistration()
        {
            ServerDeviceId = null;
        }
    }
}
