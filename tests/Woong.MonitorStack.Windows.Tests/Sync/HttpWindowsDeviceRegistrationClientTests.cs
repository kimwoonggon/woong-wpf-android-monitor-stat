using System.Net;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Sync;

namespace Woong.MonitorStack.Windows.Tests.Sync;

public sealed class HttpWindowsDeviceRegistrationClientTests
{
    [Fact]
    public async Task RegisterAsync_PostsRegistrationPayloadWithUserHeaderAndReturnsTokenWithoutEchoingIt()
    {
        var handler = new CapturingHandler(
            """{"deviceId":"server-device-1","deviceToken":"secret-device-token"}""");
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://monitor.example")
        };
        var client = new HttpWindowsDeviceRegistrationClient(httpClient);

        WindowsDeviceRegistrationResponse response = await client.RegisterAsync(
            new WindowsDeviceRegistrationRequest(
                "local-windows-user",
                Platform.Windows,
                "windows-device-key",
                "Windows device",
                "Asia/Seoul"));

        Assert.Equal("server-device-1", response.ServerDeviceId);
        Assert.Equal("secret-device-token", response.DeviceToken);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://monitor.example/api/devices/register", handler.Request.RequestUri!.AbsoluteUri);
        Assert.Equal("local-windows-user", Assert.Single(handler.Request.Headers.GetValues("X-Woong-User-Id")));
        Assert.DoesNotContain("X-Device-Token", handler.Request.Headers.Select(header => header.Key));
        Assert.Contains("\"userId\":\"local-windows-user\"", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"platform\":1", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"deviceKey\":\"windows-device-key\"", handler.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-device-token", handler.Body, StringComparison.Ordinal);
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
