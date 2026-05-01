using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Tests.Sessions;

public sealed class WebSessionUploadApiTests
{
    private const string DeviceTokenHeaderName = "X-Device-Token";

    [Fact]
    public async Task UploadWebSessions_WhenDeviceTokenHeaderIsMissing_ReturnsUnauthorizedAndPersistsNoRows()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        DeviceRegistration registration = await RegisterDeviceAsync(client, "missing-token-web-focus");
        await SeedFocusSessionAsync(factory, Guid.ParseExact(registration.DeviceId, "N"), "missing-token-web-focus");
        var request = new UploadWebSessionsRequest(
            registration.DeviceId,
            [CreateWebSession("missing-token-web-session", "missing-token-web-focus")]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/web-sessions/upload", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.WebSessions.ToListAsync());
    }

    [Fact]
    public async Task UploadWebSessions_PersistsNewSessionAndMarksDuplicateRetry()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        DeviceRegistration registration = await RegisterDeviceAsync(client, "client-session-1");
        await SeedFocusSessionAsync(factory, Guid.ParseExact(registration.DeviceId, "N"), "client-session-1");
        var session = CreateWebSession(
            clientSessionId: "web-session-1",
            focusSessionId: "client-session-1",
            url: "https://example.com/docs",
            domain: "example.com",
            pageTitle: "Docs");
        var request = new UploadWebSessionsRequest(registration.DeviceId, [session]);

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, registration.DeviceToken);
        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/web-sessions/upload", request);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/web-sessions/upload", request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        using JsonDocument firstJson = await JsonDocument.ParseAsync(await firstResponse.Content.ReadAsStreamAsync());
        using JsonDocument secondJson = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());
        Assert.Equal((int)UploadItemStatus.Accepted, firstJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());
        Assert.Equal((int)UploadItemStatus.Duplicate, secondJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        WebSessionEntity persisted = Assert.Single(await dbContext.WebSessions.ToListAsync());
        Assert.Equal(Guid.ParseExact(registration.DeviceId, "N"), persisted.DeviceId);
        Assert.Equal("web-session-1", persisted.ClientSessionId);
        Assert.Equal("client-session-1", persisted.FocusSessionId);
        Assert.Equal("example.com", persisted.Domain);
        Assert.Equal("Docs", persisted.PageTitle);
        Assert.Equal(600_000, persisted.DurationMs);
    }

    [Fact]
    public async Task UploadWebSessions_WhenUrlIsNull_PreservesCaptureMetadataAndMarksDuplicateRetry()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        DeviceRegistration registration = await RegisterDeviceAsync(client, "domain-only-session-1");
        await SeedFocusSessionAsync(factory, Guid.ParseExact(registration.DeviceId, "N"), "domain-only-session-1");
        var session = new WebSessionUploadItem(
            clientSessionId: "domain-only-web-session-1",
            focusSessionId: "domain-only-session-1",
            browserFamily: "Chrome",
            url: null,
            domain: "github.com",
            pageTitle: null,
            startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 10, 0, TimeSpan.Zero),
            durationMs: 600_000,
            captureMethod: "UIAutomationAddressBar",
            captureConfidence: "High",
            isPrivateOrUnknown: false);
        var request = new UploadWebSessionsRequest(registration.DeviceId, [session]);

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, registration.DeviceToken);
        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/web-sessions/upload", request);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/web-sessions/upload", request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        using JsonDocument firstJson = await JsonDocument.ParseAsync(await firstResponse.Content.ReadAsStreamAsync());
        using JsonDocument secondJson = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());
        Assert.Equal((int)UploadItemStatus.Accepted, firstJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());
        Assert.Equal((int)UploadItemStatus.Duplicate, secondJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        WebSessionEntity persisted = Assert.Single(await dbContext.WebSessions.ToListAsync());
        Assert.Equal("domain-only-web-session-1", persisted.ClientSessionId);
        Assert.Null(persisted.Url);
        Assert.Equal("github.com", persisted.Domain);
        Assert.Null(persisted.PageTitle);
        Assert.Equal("UIAutomationAddressBar", persisted.CaptureMethod);
        Assert.Equal("High", persisted.CaptureConfidence);
        Assert.False(persisted.IsPrivateOrUnknown);
    }

    private static WebSessionUploadItem CreateWebSession(
        string clientSessionId,
        string focusSessionId,
        string? url = null,
        string domain = "example.com",
        string? pageTitle = null)
        => new(
            clientSessionId,
            focusSessionId,
            browserFamily: "Chrome",
            url,
            domain,
            pageTitle,
            startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 10, 0, TimeSpan.Zero),
            durationMs: 600_000);

    private static async Task SeedFocusSessionAsync(
        WebApplicationFactory<Program> factory,
        Guid deviceId,
        string focusSessionId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        dbContext.FocusSessions.Add(new FocusSessionEntity
        {
            DeviceId = deviceId,
            ClientSessionId = focusSessionId,
            PlatformAppKey = "chrome.exe",
            StartedAtUtc = new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            EndedAtUtc = new DateTimeOffset(2026, 4, 27, 15, 10, 0, TimeSpan.Zero),
            DurationMs = 600_000,
            LocalDate = new DateOnly(2026, 4, 28),
            TimezoneId = "Asia/Seoul",
            IsIdle = false,
            Source = "test"
        });
        await dbContext.SaveChangesAsync();
    }

    private static async Task<DeviceRegistration> RegisterDeviceAsync(HttpClient client, string focusSessionId)
    {
        var registrationRequest = new RegisterDeviceRequest(
            userId: "user-1",
            platform: Platform.Windows,
            deviceKey: $"windows-web-upload-key-{focusSessionId}",
            deviceName: "Windows Workstation",
            timezoneId: "Asia/Seoul");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/devices/register", registrationRequest);
        response.EnsureSuccessStatusCode();
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        return new DeviceRegistration(
            json.RootElement.GetProperty("deviceId").GetString()!,
            json.RootElement.GetProperty("deviceToken").GetString()!);
    }

    private sealed record DeviceRegistration(string DeviceId, string DeviceToken);

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
