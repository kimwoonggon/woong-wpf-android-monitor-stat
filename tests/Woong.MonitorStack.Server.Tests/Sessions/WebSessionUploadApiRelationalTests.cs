using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Data;
using Woong.MonitorStack.Server.Tests.Data;

namespace Woong.MonitorStack.Server.Tests.Sessions;

public sealed class WebSessionUploadApiRelationalTests
{
    private const string DeviceTokenHeaderName = "X-Device-Token";

    [Fact]
    public async Task UploadWebSessions_WhenDeviceIsNotRegistered_ReturnsControlledErrorAndDoesNotPersistRows()
    {
        using var factory = new RelationalServerFactory();
        using HttpClient client = factory.CreateClient();
        await factory.EnsureDatabaseCreatedAsync();
        string unknownDeviceId = Guid.NewGuid().ToString("N");
        var request = new UploadWebSessionsRequest(
            unknownDeviceId,
            [Web("unknown-device-web-session", "missing-focus-session")]);

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, "not-a-valid-device-token");
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/web-sessions/upload", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.WebSessions.ToListAsync());
    }

    [Fact]
    public async Task UploadWebSessions_WhenFocusSessionIsMissing_ReturnsControlledErrorAndDoesNotPersistRows()
    {
        using var factory = new RelationalServerFactory();
        using HttpClient client = factory.CreateClient();
        await factory.EnsureDatabaseCreatedAsync();
        DeviceRegistration registration = await RegisterDeviceAsync(client, "windows-web-relational-key");
        var request = new UploadWebSessionsRequest(
            registration.DeviceId,
            [Web("orphan-web-session", "missing-focus-session")]);

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, registration.DeviceToken);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/web-sessions/upload", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement item = json.RootElement.GetProperty("items")[0];
        Assert.Equal("orphan-web-session", item.GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Error, item.GetProperty("status").GetInt32());
        Assert.Contains("focus", item.GetProperty("errorMessage").GetString(), StringComparison.OrdinalIgnoreCase);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.WebSessions.ToListAsync());
    }

    [Fact]
    public async Task UploadWebSessions_WhenBatchContainsDuplicateAcceptedIntraBatchDuplicateAndMissingFocusParent_ReturnsIndependentStatuses()
    {
        using var factory = new RelationalServerFactory();
        using HttpClient client = factory.CreateClient();
        await factory.EnsureDatabaseCreatedAsync();
        DeviceRegistration registration = await RegisterDeviceAsync(client, "windows-web-batch-key");
        await SeedFocusAndExistingWebSessionAsync(factory, Guid.ParseExact(registration.DeviceId, "N"));
        var request = new UploadWebSessionsRequest(
            registration.DeviceId,
            [
                Web("existing-web-session", "focus-parent-session"),
                Web("new-web-session", "focus-parent-session"),
                Web("new-web-session", "focus-parent-session"),
                Web("orphan-web-session", "missing-focus-parent")
            ]);

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, registration.DeviceToken);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/web-sessions/upload", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement[] items = json.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal("existing-web-session", items[0].GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Duplicate, items[0].GetProperty("status").GetInt32());
        Assert.Equal("new-web-session", items[1].GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Accepted, items[1].GetProperty("status").GetInt32());
        Assert.Equal("new-web-session", items[2].GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Duplicate, items[2].GetProperty("status").GetInt32());
        Assert.Equal("orphan-web-session", items[3].GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Error, items[3].GetProperty("status").GetInt32());
        Assert.Contains("focus", items[3].GetProperty("errorMessage").GetString(), StringComparison.OrdinalIgnoreCase);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        List<WebSessionEntity> persisted = await dbContext.WebSessions
            .OrderBy(session => session.ClientSessionId)
            .ToListAsync();
        Assert.Equal(["existing-web-session", "new-web-session"], persisted.Select(session => session.ClientSessionId));
    }

    private static async Task SeedFocusAndExistingWebSessionAsync(RelationalServerFactory factory, Guid deviceId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        dbContext.FocusSessions.Add(new FocusSessionEntity
        {
            DeviceId = deviceId,
            ClientSessionId = "focus-parent-session",
            PlatformAppKey = "chrome.exe",
            StartedAtUtc = new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            EndedAtUtc = new DateTimeOffset(2026, 4, 27, 15, 10, 0, TimeSpan.Zero),
            DurationMs = 600_000,
            LocalDate = new DateOnly(2026, 4, 28),
            TimezoneId = "Asia/Seoul",
            IsIdle = false,
            Source = "test"
        });
        WebSessionUploadItem existing = Web("existing-web-session", "focus-parent-session");
        dbContext.WebSessions.Add(new WebSessionEntity
        {
            DeviceId = deviceId,
            ClientSessionId = existing.ClientSessionId,
            FocusSessionId = existing.FocusSessionId,
            BrowserFamily = existing.BrowserFamily,
            Url = existing.Url,
            Domain = existing.Domain,
            PageTitle = existing.PageTitle,
            StartedAtUtc = existing.StartedAtUtc,
            EndedAtUtc = existing.EndedAtUtc,
            DurationMs = existing.DurationMs,
            CaptureMethod = existing.CaptureMethod,
            CaptureConfidence = existing.CaptureConfidence,
            IsPrivateOrUnknown = existing.IsPrivateOrUnknown
        });
        await dbContext.SaveChangesAsync();
    }

    private static async Task<DeviceRegistration> RegisterDeviceAsync(HttpClient client, string deviceKey)
    {
        var registrationRequest = new RegisterDeviceRequest(
            userId: "user-1",
            platform: Platform.Windows,
            deviceKey,
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

    private static WebSessionUploadItem Web(string clientSessionId, string focusSessionId)
    {
        var startedAtUtc = new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero);

        return new WebSessionUploadItem(
            clientSessionId,
            focusSessionId,
            browserFamily: "Chrome",
            url: null,
            domain: "example.com",
            pageTitle: null,
            startedAtUtc,
            startedAtUtc.AddMinutes(5),
            durationMs: 300_000,
            captureMethod: "BrowserExtensionFuture",
            captureConfidence: "High",
            isPrivateOrUnknown: false);
    }
}
