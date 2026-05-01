using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Tests.Devices;

public sealed class UploadEndpointDeviceTokenAuthTests
{
    [Theory]
    [MemberData(nameof(ProtectedUploadRequests))]
    public async Task ProtectedUploadEndpoint_WhenDeviceTokenHeaderIsMissing_ReturnsUnauthorized(
        string route,
        object body)
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(route, body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public static TheoryData<string, object> ProtectedUploadRequests()
        => new()
        {
            { "/api/focus-sessions/upload", new UploadFocusSessionsRequest(DeviceId(), [FocusSession()]) },
            { "/api/web-sessions/upload", new UploadWebSessionsRequest(DeviceId(), [WebSession()]) },
            { "/api/raw-events/upload", new UploadRawEventsRequest(DeviceId(), [RawEvent()]) },
            { "/api/location-contexts/upload", new UploadLocationContextsRequest(DeviceId(), [LocationContext()]) }
        };

    private static string DeviceId()
        => Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa").ToString("N");

    private static FocusSessionUploadItem FocusSession()
        => new(
            clientSessionId: "focus-session-1",
            platformAppKey: "chrome.exe",
            startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 10, 0, TimeSpan.Zero),
            durationMs: 600_000,
            localDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "auth-contract-test");

    private static WebSessionUploadItem WebSession()
        => new(
            clientSessionId: "web-session-1",
            focusSessionId: "focus-session-1",
            browserFamily: "Chrome",
            url: null,
            domain: "example.com",
            pageTitle: null,
            startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 10, 0, TimeSpan.Zero),
            durationMs: 600_000,
            captureMethod: "BrowserExtensionFuture",
            captureConfidence: "High",
            isPrivateOrUnknown: false);

    private static RawEventUploadItem RawEvent()
        => new(
            clientEventId: "raw-event-1",
            eventType: "foreground_window",
            occurredAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            payloadJson: """{"processName":"Code.exe"}""");

    private static LocationContextUploadItem LocationContext()
        => new(
            clientContextId: "location-context-1",
            capturedAtUtc: new DateTimeOffset(2026, 4, 28, 1, 2, 3, TimeSpan.Zero),
            localDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul",
            latitude: null,
            longitude: null,
            accuracyMeters: null,
            captureMode: "location_unavailable",
            permissionState: "denied",
            source: "auth-contract-test");

    private static WebApplicationFactory<Program> CreateFactoryWithInMemoryDatabase()
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    string databaseName = $"server-tests-{Guid.NewGuid():N}";
                    services.RemoveAll<DbContextOptions<MonitorDbContext>>();
                    services.RemoveAll<DbContextOptions>();
                    services.AddDbContext<MonitorDbContext>(options =>
                        options.UseInMemoryDatabase(databaseName));
                });
            });
}
