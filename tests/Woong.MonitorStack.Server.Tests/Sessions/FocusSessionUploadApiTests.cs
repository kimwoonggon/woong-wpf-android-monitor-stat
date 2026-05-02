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

public sealed class FocusSessionUploadApiTests
{
    private const string DeviceTokenHeaderName = "X-Device-Token";

    [Fact]
    public async Task UploadFocusSessions_WhenDeviceTokenHeaderIsMissing_ReturnsUnauthorizedAndPersistsNoRows()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        DeviceRegistration registration = await RegisterDeviceAsync(client);
        var request = new UploadFocusSessionsRequest(registration.DeviceId, [CreateFocusSession()]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/focus-sessions/upload", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.FocusSessions.ToListAsync());
    }

    [Fact]
    public async Task UploadFocusSessions_WhenDeviceTokenHeaderIsInvalid_ReturnsUnauthorizedAndPersistsNoRows()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        DeviceRegistration registration = await RegisterDeviceAsync(client);
        var request = new UploadFocusSessionsRequest(registration.DeviceId, [CreateFocusSession()]);

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, "wms_dev_invalid-token");
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/focus-sessions/upload", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.FocusSessions.ToListAsync());
    }

    [Fact]
    public async Task UploadFocusSessions_WhenDeviceTokenBelongsToAnotherDevice_ReturnsUnauthorizedAndPersistsNoRows()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        DeviceRegistration firstDevice = await RegisterDeviceAsync(client, "windows-upload-key-a");
        DeviceRegistration secondDevice = await RegisterDeviceAsync(client, "windows-upload-key-b");
        var request = new UploadFocusSessionsRequest(secondDevice.DeviceId, [CreateFocusSession()]);

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, firstDevice.DeviceToken);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/focus-sessions/upload", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.FocusSessions.ToListAsync());
    }

    [Fact]
    public async Task UploadFocusSessions_PersistsNewSessionAndMarksDuplicateRetry()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        DeviceRegistration registration = await RegisterDeviceAsync(client);
        var session = CreateFocusSession();
        var request = new UploadFocusSessionsRequest(registration.DeviceId, [session]);

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, registration.DeviceToken);
        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/focus-sessions/upload", request);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/focus-sessions/upload", request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        using JsonDocument firstJson = await JsonDocument.ParseAsync(await firstResponse.Content.ReadAsStreamAsync());
        using JsonDocument secondJson = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());
        Assert.Equal((int)UploadItemStatus.Accepted, firstJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());
        Assert.Equal((int)UploadItemStatus.Duplicate, secondJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        FocusSessionEntity persisted = Assert.Single(await dbContext.FocusSessions.ToListAsync());
        Assert.Equal(Guid.Parse(registration.DeviceId), persisted.DeviceId);
        Assert.Equal("client-session-1", persisted.ClientSessionId);
        Assert.Equal("chrome.exe", persisted.PlatformAppKey);
        Assert.Equal(600_000, persisted.DurationMs);
        Assert.False(persisted.IsIdle);
        Assert.Equal(4321, persisted.ProcessId);
        Assert.Equal("chrome.exe", persisted.ProcessName);
        Assert.Equal(@"C:\Program Files\Google\Chrome\Application\chrome.exe", persisted.ProcessPath);
        Assert.Equal(123456, persisted.WindowHandle);
        Assert.Null(persisted.WindowTitle);
    }

    [Fact]
    public async Task UploadFocusSessions_WhenAndroidUsageStatsPayloadOmitsWindowsFields_PersistsAndMarksDuplicateRetry()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        DeviceRegistration registration = await RegisterDeviceAsync(
            client,
            deviceKey: "android-focus-upload-key",
            platform: Platform.Android,
            deviceName: "Android Phone");
        var session = new FocusSessionUploadItem(
            clientSessionId: "android-session-1",
            platformAppKey: "com.android.chrome",
            startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 5, 0, TimeSpan.Zero),
            durationMs: 300_000,
            localDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "android_usage_stats");
        var request = new UploadFocusSessionsRequest(registration.DeviceId, [session]);

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, registration.DeviceToken);
        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/focus-sessions/upload", request);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/focus-sessions/upload", request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        using JsonDocument firstJson = await JsonDocument.ParseAsync(await firstResponse.Content.ReadAsStreamAsync());
        using JsonDocument secondJson = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());
        Assert.Equal((int)UploadItemStatus.Accepted, firstJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());
        Assert.Equal((int)UploadItemStatus.Duplicate, secondJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        FocusSessionEntity persisted = Assert.Single(await dbContext.FocusSessions.ToListAsync());
        Assert.Equal(Guid.Parse(registration.DeviceId), persisted.DeviceId);
        Assert.Equal("android-session-1", persisted.ClientSessionId);
        Assert.Equal("com.android.chrome", persisted.PlatformAppKey);
        Assert.Equal("android_usage_stats", persisted.Source);
        Assert.Equal(300_000, persisted.DurationMs);
        Assert.False(persisted.IsIdle);
        Assert.Null(persisted.ProcessId);
        Assert.Null(persisted.ProcessName);
        Assert.Null(persisted.ProcessPath);
        Assert.Null(persisted.WindowHandle);
        Assert.Null(persisted.WindowTitle);
    }

    private static FocusSessionUploadItem CreateFocusSession()
        => new(
            clientSessionId: "client-session-1",
            platformAppKey: "chrome.exe",
            startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 10, 0, TimeSpan.Zero),
            durationMs: 600_000,
            localDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "foreground_window",
            processId: 4321,
            processName: "chrome.exe",
            processPath: @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            windowHandle: 123456,
            windowTitle: null);

    private static async Task<DeviceRegistration> RegisterDeviceAsync(
        HttpClient client,
        string deviceKey = "windows-upload-key",
        Platform platform = Platform.Windows,
        string deviceName = "Windows Workstation")
    {
        var registrationRequest = new RegisterDeviceRequest(
            userId: "user-1",
            platform,
            deviceKey,
            deviceName,
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
