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

public sealed class FocusSessionUploadApiRelationalTests
{
    private const string DeviceTokenHeaderName = "X-Device-Token";

    [Fact]
    public async Task UploadFocusSessions_WhenDeviceIsNotRegistered_ReturnsControlledErrorAndDoesNotPersistRows()
    {
        using var factory = new RelationalServerFactory();
        using HttpClient client = factory.CreateClient();
        await factory.EnsureDatabaseCreatedAsync();
        string unknownDeviceId = Guid.NewGuid().ToString("N");
        var request = new UploadFocusSessionsRequest(
            unknownDeviceId,
            [Focus("orphan-focus-session")]);

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, "not-a-valid-device-token");
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/focus-sessions/upload", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.FocusSessions.ToListAsync());
    }

    [Fact]
    public async Task UploadFocusSessions_WhenBatchContainsExistingAndIntraBatchDuplicate_ReturnsIndependentStatuses()
    {
        using var factory = new RelationalServerFactory();
        using HttpClient client = factory.CreateClient();
        await factory.EnsureDatabaseCreatedAsync();
        DeviceRegistration registration = await RegisterDeviceAsync(client);
        await SeedExistingFocusSessionAsync(factory, Guid.ParseExact(registration.DeviceId, "N"));
        var request = new UploadFocusSessionsRequest(
            registration.DeviceId,
            [
                Focus("existing-focus-session"),
                Focus("new-focus-session"),
                Focus("new-focus-session")
            ]);

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, registration.DeviceToken);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/focus-sessions/upload", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement[] items = json.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal("existing-focus-session", items[0].GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Duplicate, items[0].GetProperty("status").GetInt32());
        Assert.Equal("new-focus-session", items[1].GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Accepted, items[1].GetProperty("status").GetInt32());
        Assert.Equal("new-focus-session", items[2].GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Duplicate, items[2].GetProperty("status").GetInt32());

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        List<FocusSessionEntity> persisted = await dbContext.FocusSessions
            .OrderBy(session => session.ClientSessionId)
            .ToListAsync();
        Assert.Equal(["existing-focus-session", "new-focus-session"], persisted.Select(session => session.ClientSessionId));
    }

    private static async Task SeedExistingFocusSessionAsync(RelationalServerFactory factory, Guid deviceId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        FocusSessionUploadItem existing = Focus("existing-focus-session");
        dbContext.FocusSessions.Add(new FocusSessionEntity
        {
            DeviceId = deviceId,
            ClientSessionId = existing.ClientSessionId,
            PlatformAppKey = existing.PlatformAppKey,
            StartedAtUtc = existing.StartedAtUtc,
            EndedAtUtc = existing.EndedAtUtc,
            DurationMs = existing.DurationMs,
            LocalDate = existing.LocalDate,
            TimezoneId = existing.TimezoneId,
            IsIdle = existing.IsIdle,
            Source = existing.Source,
            ProcessId = existing.ProcessId,
            ProcessName = existing.ProcessName,
            ProcessPath = existing.ProcessPath,
            WindowHandle = existing.WindowHandle,
            WindowTitle = existing.WindowTitle
        });
        await dbContext.SaveChangesAsync();
    }

    private static async Task<DeviceRegistration> RegisterDeviceAsync(HttpClient client)
    {
        var registrationRequest = new RegisterDeviceRequest(
            userId: "user-1",
            platform: Platform.Windows,
            deviceKey: "windows-focus-batch-key",
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

    private static FocusSessionUploadItem Focus(string clientSessionId)
    {
        var startedAtUtc = new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero);

        return new FocusSessionUploadItem(
            clientSessionId,
            platformAppKey: "chrome.exe",
            startedAtUtc,
            startedAtUtc.AddMinutes(5),
            durationMs: 300_000,
            localDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "relational-upload-test");
    }
}
