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

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/web-sessions/upload", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement item = json.RootElement.GetProperty("items")[0];
        Assert.Equal("unknown-device-web-session", item.GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Error, item.GetProperty("status").GetInt32());
        Assert.Contains("device", item.GetProperty("errorMessage").GetString(), StringComparison.OrdinalIgnoreCase);

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
        Guid deviceId = await SeedRegisteredDeviceAsync(factory);
        var request = new UploadWebSessionsRequest(
            deviceId.ToString("N"),
            [Web("orphan-web-session", "missing-focus-session")]);

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
        Guid deviceId = Guid.NewGuid();
        await SeedDeviceFocusAndExistingWebSessionAsync(factory, deviceId);
        var request = new UploadWebSessionsRequest(
            deviceId.ToString("N"),
            [
                Web("existing-web-session", "focus-parent-session"),
                Web("new-web-session", "focus-parent-session"),
                Web("new-web-session", "focus-parent-session"),
                Web("orphan-web-session", "missing-focus-parent")
            ]);

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

    private static async Task<Guid> SeedRegisteredDeviceAsync(RelationalServerFactory factory)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Guid deviceId = Guid.NewGuid();
        dbContext.Devices.Add(new DeviceEntity
        {
            Id = deviceId,
            UserId = "user-1",
            Platform = Platform.Windows,
            DeviceKey = "windows-web-relational-key",
            DeviceName = "Windows Workstation",
            TimezoneId = "Asia/Seoul",
            CreatedAtUtc = new DateTimeOffset(2026, 4, 27, 0, 0, 0, TimeSpan.Zero),
            LastSeenAtUtc = new DateTimeOffset(2026, 4, 27, 0, 0, 0, TimeSpan.Zero)
        });
        await dbContext.SaveChangesAsync();

        return deviceId;
    }

    private static async Task SeedDeviceFocusAndExistingWebSessionAsync(RelationalServerFactory factory, Guid deviceId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        dbContext.Devices.Add(new DeviceEntity
        {
            Id = deviceId,
            UserId = "user-1",
            Platform = Platform.Windows,
            DeviceKey = "windows-web-batch-key",
            DeviceName = "Windows Workstation",
            TimezoneId = "Asia/Seoul",
            CreatedAtUtc = new DateTimeOffset(2026, 4, 27, 0, 0, 0, TimeSpan.Zero),
            LastSeenAtUtc = new DateTimeOffset(2026, 4, 27, 0, 0, 0, TimeSpan.Zero)
        });
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
