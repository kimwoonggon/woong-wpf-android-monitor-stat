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
using Woong.MonitorStack.Server.Devices;

namespace Woong.MonitorStack.Server.Tests.Devices;

public sealed class DeviceRegistrationApiTests
{
    private const string DeviceTokenHeaderName = "X-Device-Token";

    [Fact]
    public async Task RevokeDeviceToken_InvalidatesTokenForUploadsRotationAndRepeatedRevocationWithoutDeletingRows()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        DeviceRegistrationResponse registration = await RegisterDeviceAsync(
            client,
            platform: Platform.Android,
            deviceKey: "android-revoke-token-key",
            deviceName: "Android Phone");

        await UploadFocusAsync(client, registration.DeviceId, registration.DeviceToken, "focus-before-rotate");
        await UploadWebAsync(client, registration.DeviceId, registration.DeviceToken, "web-before-rotate", "focus-before-rotate");
        await UploadRawAsync(client, registration.DeviceId, registration.DeviceToken, "raw-before-rotate");
        await UploadLocationAsync(client, registration.DeviceId, registration.DeviceToken, "location-before-rotate");
        PersistedUploadIds beforeIds = await GetPersistedUploadIdsAsync(factory);

        HttpResponseMessage revokeResponse = await RevokeTokenAsync(
            client,
            registration.DeviceId,
            registration.DeviceToken);

        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        HttpResponseMessage uploadWithRevokedTokenResponse = await PostFocusAsync(
            client,
            registration.DeviceId,
            registration.DeviceToken,
            "focus-after-revoke");
        Assert.Equal(HttpStatusCode.Unauthorized, uploadWithRevokedTokenResponse.StatusCode);

        HttpResponseMessage rotateWithRevokedTokenResponse = await RotateTokenAsync(
            client,
            registration.DeviceId,
            registration.DeviceToken);
        Assert.Equal(HttpStatusCode.Unauthorized, rotateWithRevokedTokenResponse.StatusCode);

        HttpResponseMessage repeatedRevokeResponse = await RevokeTokenAsync(
            client,
            registration.DeviceId,
            registration.DeviceToken);
        Assert.Equal(HttpStatusCode.Unauthorized, repeatedRevokeResponse.StatusCode);

        PersistedUploadIds afterIds = await GetPersistedUploadIdsAsync(factory);
        Assert.Equal(beforeIds, afterIds);
    }

    [Fact]
    public async Task RotateDeviceToken_InvalidatesOldTokenReturnsNewTokenAndPreservesUploadedRows()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        DeviceRegistrationResponse registration = await RegisterDeviceAsync(
            client,
            platform: Platform.Android,
            deviceKey: "android-rotate-token-key",
            deviceName: "Android Phone");

        await UploadFocusAsync(client, registration.DeviceId, registration.DeviceToken, "focus-before-rotate");
        await UploadWebAsync(client, registration.DeviceId, registration.DeviceToken, "web-before-rotate", "focus-before-rotate");
        await UploadRawAsync(client, registration.DeviceId, registration.DeviceToken, "raw-before-rotate");
        await UploadLocationAsync(client, registration.DeviceId, registration.DeviceToken, "location-before-rotate");
        PersistedUploadIds beforeIds = await GetPersistedUploadIdsAsync(factory);

        HttpResponseMessage rotateResponse = await RotateTokenAsync(
            client,
            registration.DeviceId,
            registration.DeviceToken);

        Assert.Equal(HttpStatusCode.OK, rotateResponse.StatusCode);
        DeviceTokenRotationResponse rotation =
            (await rotateResponse.Content.ReadFromJsonAsync<DeviceTokenRotationResponse>())!;
        Assert.Equal(registration.DeviceId, rotation.DeviceId);
        Assert.False(string.IsNullOrWhiteSpace(rotation.DeviceToken));
        Assert.NotEqual(registration.DeviceToken, rotation.DeviceToken);

        HttpResponseMessage oldTokenResponse = await PostFocusAsync(
            client,
            registration.DeviceId,
            registration.DeviceToken,
            "focus-with-old-token");
        Assert.Equal(HttpStatusCode.Unauthorized, oldTokenResponse.StatusCode);

        await UploadFocusAsync(client, registration.DeviceId, rotation.DeviceToken, "focus-after-rotate");
        PersistedUploadIds afterIds = await GetPersistedUploadIdsAsync(factory);

        Assert.Equal(beforeIds.FocusSessionId, afterIds.FocusSessionId);
        Assert.Equal(beforeIds.WebSessionId, afterIds.WebSessionId);
        Assert.Equal(beforeIds.RawEventId, afterIds.RawEventId);
        Assert.Equal(beforeIds.LocationContextId, afterIds.LocationContextId);
        Assert.Equal(2, afterIds.FocusSessionCount);
        Assert.Equal(1, afterIds.WebSessionCount);
        Assert.Equal(1, afterIds.RawEventCount);
        Assert.Equal(1, afterIds.LocationContextCount);
    }

    [Fact]
    public async Task RegisterDevice_ReturnsDeviceTokenAndPreservesItForIdempotentRegistration()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        var request = new RegisterDeviceRequest(
            userId: "user-1",
            platform: Platform.Android,
            deviceKey: "android-token-device-key",
            deviceName: "Phone",
            timezoneId: "Asia/Seoul");

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/devices/register", request);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/devices/register", request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        DeviceRegistrationResponse firstBody = (await firstResponse.Content.ReadFromJsonAsync<DeviceRegistrationResponse>())!;
        DeviceRegistrationResponse secondBody = (await secondResponse.Content.ReadFromJsonAsync<DeviceRegistrationResponse>())!;

        Assert.False(string.IsNullOrWhiteSpace(firstBody.DeviceToken));
        Assert.Equal(firstBody.DeviceId, secondBody.DeviceId);
        Assert.Equal(firstBody.DeviceToken, secondBody.DeviceToken);
        Assert.NotEqual(firstBody.DeviceToken, firstBody.DeviceId);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        DeviceEntity device = Assert.Single(await dbContext.Devices.ToListAsync());
        Assert.False(string.IsNullOrWhiteSpace(device.DeviceTokenHash));
        Assert.DoesNotContain(firstBody.DeviceToken, device.DeviceTokenHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterDevice_ReturnsStableDeviceIdForSameDeviceKey()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        var request = new RegisterDeviceRequest(
            userId: "user-1",
            platform: Platform.Windows,
            deviceKey: "windows-device-key",
            deviceName: "Workstation",
            timezoneId: "Asia/Seoul");

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/devices/register", request);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/devices/register", request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        using JsonDocument firstJson = await JsonDocument.ParseAsync(await firstResponse.Content.ReadAsStreamAsync());
        using JsonDocument secondJson = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());
        string firstDeviceId = firstJson.RootElement.GetProperty("deviceId").GetString()!;
        string secondDeviceId = secondJson.RootElement.GetProperty("deviceId").GetString()!;

        Assert.False(string.IsNullOrWhiteSpace(firstDeviceId));
        Assert.Equal(firstDeviceId, secondDeviceId);
        Assert.Equal("windows-device-key", firstJson.RootElement.GetProperty("deviceKey").GetString());
        Assert.Equal("windows", firstJson.RootElement.GetProperty("platform").GetString());
        Assert.True(firstJson.RootElement.GetProperty("isNew").GetBoolean());
        Assert.False(secondJson.RootElement.GetProperty("isNew").GetBoolean());
    }

    [Fact]
    public async Task RegisterDevice_PersistsDeviceRow()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        var request = new RegisterDeviceRequest(
            userId: "user-1",
            platform: Platform.Android,
            deviceKey: "android-device-key",
            deviceName: "Phone",
            timezoneId: "Asia/Seoul");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/devices/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        DeviceEntity device = Assert.Single(await dbContext.Devices.ToListAsync());
        Assert.Equal("user-1", device.UserId);
        Assert.Equal(Platform.Android, device.Platform);
        Assert.Equal("android-device-key", device.DeviceKey);
        Assert.Equal("Phone", device.DeviceName);
        Assert.Equal("Asia/Seoul", device.TimezoneId);
    }

    [Fact]
    public async Task RegisterDevice_WhenDeviceKeyAlreadyExists_UpdatesNameTimezoneAndLastSeen()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        var firstRequest = new RegisterDeviceRequest(
            userId: "user-1",
            platform: Platform.Android,
            deviceKey: "android-device-key",
            deviceName: "Old Phone",
            timezoneId: "UTC");
        var secondRequest = new RegisterDeviceRequest(
            userId: "user-1",
            platform: Platform.Android,
            deviceKey: "android-device-key",
            deviceName: "New Phone",
            timezoneId: "Asia/Seoul");

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/devices/register", firstRequest);
        await Task.Delay(5);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/devices/register", secondRequest);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        DeviceRegistrationResponse firstBody = (await firstResponse.Content.ReadFromJsonAsync<DeviceRegistrationResponse>())!;
        DeviceRegistrationResponse secondBody = (await secondResponse.Content.ReadFromJsonAsync<DeviceRegistrationResponse>())!;

        Assert.Equal(firstBody.DeviceId, secondBody.DeviceId);
        Assert.True(firstBody.IsNew);
        Assert.False(secondBody.IsNew);
        Assert.Equal("New Phone", secondBody.DeviceName);
        Assert.Equal("Asia/Seoul", secondBody.TimezoneId);
        Assert.True(secondBody.LastSeenAtUtc >= firstBody.LastSeenAtUtc);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        DeviceEntity device = Assert.Single(await dbContext.Devices.ToListAsync());
        Assert.Equal(Guid.ParseExact(firstBody.DeviceId, "N"), device.Id);
        Assert.Equal("New Phone", device.DeviceName);
        Assert.Equal("Asia/Seoul", device.TimezoneId);
        Assert.Equal(secondBody.LastSeenAtUtc, device.LastSeenAtUtc);
    }

    private static async Task<DeviceRegistrationResponse> RegisterDeviceAsync(
        HttpClient client,
        Platform platform,
        string deviceKey,
        string deviceName)
    {
        var request = new RegisterDeviceRequest(
            userId: "user-1",
            platform,
            deviceKey,
            deviceName,
            timezoneId: "Asia/Seoul");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/devices/register", request);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<DeviceRegistrationResponse>())!;
    }

    private static async Task<HttpResponseMessage> RotateTokenAsync(
        HttpClient client,
        string deviceId,
        string deviceToken)
    {
        using var rotateRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/devices/{deviceId}/token/rotate");
        rotateRequest.Headers.Add(DeviceTokenHeaderName, deviceToken);

        return await client.SendAsync(rotateRequest);
    }

    private static async Task<HttpResponseMessage> RevokeTokenAsync(
        HttpClient client,
        string deviceId,
        string deviceToken)
    {
        using var revokeRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/devices/{deviceId}/token/revoke");
        revokeRequest.Headers.Add(DeviceTokenHeaderName, deviceToken);

        return await client.SendAsync(revokeRequest);
    }

    private static async Task UploadFocusAsync(
        HttpClient client,
        string deviceId,
        string deviceToken,
        string clientSessionId)
    {
        HttpResponseMessage response = await PostFocusAsync(client, deviceId, deviceToken, clientSessionId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal((int)UploadItemStatus.Accepted, json.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());
    }

    private static async Task<HttpResponseMessage> PostFocusAsync(
        HttpClient client,
        string deviceId,
        string deviceToken,
        string clientSessionId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/focus-sessions/upload")
        {
            Content = JsonContent.Create(new UploadFocusSessionsRequest(
                deviceId,
                [
                    new FocusSessionUploadItem(
                        clientSessionId,
                        platformAppKey: "com.android.chrome",
                        startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
                        endedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 5, 0, TimeSpan.Zero),
                        durationMs: 300_000,
                        localDate: new DateOnly(2026, 4, 28),
                        timezoneId: "Asia/Seoul",
                        isIdle: false,
                        source: "android_usage_stats")
                ]))
        };
        request.Headers.Add(DeviceTokenHeaderName, deviceToken);

        return await client.SendAsync(request);
    }

    private static async Task UploadWebAsync(
        HttpClient client,
        string deviceId,
        string deviceToken,
        string clientSessionId,
        string focusSessionId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/web-sessions/upload")
        {
            Content = JsonContent.Create(new UploadWebSessionsRequest(
                deviceId,
                [
                    new WebSessionUploadItem(
                        clientSessionId,
                        focusSessionId,
                        browserFamily: "Chrome",
                        url: null,
                        domain: "example.com",
                        pageTitle: null,
                        startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 1, 0, TimeSpan.Zero),
                        endedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 5, 0, TimeSpan.Zero),
                        durationMs: 240_000,
                        captureMethod: "BrowserExtensionFuture",
                        captureConfidence: "High",
                        isPrivateOrUnknown: false)
                ]))
        };
        request.Headers.Add(DeviceTokenHeaderName, deviceToken);

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal((int)UploadItemStatus.Accepted, json.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());
    }

    private static async Task UploadRawAsync(
        HttpClient client,
        string deviceId,
        string deviceToken,
        string clientEventId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/raw-events/upload")
        {
            Content = JsonContent.Create(new UploadRawEventsRequest(
                deviceId,
                [
                    new RawEventUploadItem(
                        clientEventId,
                        eventType: "foreground_window",
                        occurredAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
                        payloadJson: """{"packageName":"com.android.chrome"}""")
                ]))
        };
        request.Headers.Add(DeviceTokenHeaderName, deviceToken);

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal((int)UploadItemStatus.Accepted, json.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());
    }

    private static async Task UploadLocationAsync(
        HttpClient client,
        string deviceId,
        string deviceToken,
        string clientContextId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/location-contexts/upload")
        {
            Content = JsonContent.Create(new UploadLocationContextsRequest(
                deviceId,
                [
                    new LocationContextUploadItem(
                        clientContextId,
                        capturedAtUtc: new DateTimeOffset(2026, 4, 28, 1, 2, 3, TimeSpan.Zero),
                        localDate: new DateOnly(2026, 4, 28),
                        timezoneId: "Asia/Seoul",
                        latitude: null,
                        longitude: null,
                        accuracyMeters: null,
                        captureMode: "location_unavailable",
                        permissionState: "denied",
                        source: "android_location_context")
                ]))
        };
        request.Headers.Add(DeviceTokenHeaderName, deviceToken);

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal((int)UploadItemStatus.Accepted, json.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());
    }

    private static async Task<PersistedUploadIds> GetPersistedUploadIdsAsync(WebApplicationFactory<Program> factory)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();

        FocusSessionEntity focusSession = Assert.Single(
            await dbContext.FocusSessions.Where(session => session.ClientSessionId == "focus-before-rotate").ToListAsync());
        WebSessionEntity webSession = Assert.Single(await dbContext.WebSessions.ToListAsync());
        RawEventEntity rawEvent = Assert.Single(await dbContext.RawEvents.ToListAsync());
        LocationContextEntity locationContext = Assert.Single(await dbContext.LocationContexts.ToListAsync());

        return new PersistedUploadIds(
            focusSession.Id,
            await dbContext.FocusSessions.CountAsync(),
            webSession.Id,
            await dbContext.WebSessions.CountAsync(),
            rawEvent.Id,
            await dbContext.RawEvents.CountAsync(),
            locationContext.Id,
            await dbContext.LocationContexts.CountAsync());
    }

    private sealed record PersistedUploadIds(
        long FocusSessionId,
        int FocusSessionCount,
        long WebSessionId,
        int WebSessionCount,
        long RawEventId,
        int RawEventCount,
        long LocationContextId,
        int LocationContextCount);

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
