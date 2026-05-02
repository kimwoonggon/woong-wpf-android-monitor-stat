using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Data;
using Woong.MonitorStack.Server.Devices;

namespace Woong.MonitorStack.Server.Tests.Devices;

public sealed class DeviceRegistrationPolicyTests
{
    private const string AuthenticatedUserHeaderName = "X-Woong-User-Id";

    [Fact]
    public async Task RegisterDevice_WhenUserSessionAuthIsMissingAndStrictModeIsEnabled_ReturnsUnauthorized()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase(requireAuthenticatedUser: true);
        using HttpClient client = factory.CreateClient();
        var request = new RegisterDeviceRequest(
            userId: "payload-user",
            platform: Platform.Android,
            deviceKey: "android-policy-device-key",
            deviceName: "Phone",
            timezoneId: "Asia/Seoul");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/devices/register", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.Devices.ToListAsync());
    }

    [Fact]
    public async Task RegisterDevice_UsesAuthenticatedUserIdInsteadOfPayloadUserId()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase(requireAuthenticatedUser: false);
        using HttpClient client = factory.CreateClient();
        var request = new RegisterDeviceRequest(
            userId: "payload-user",
            platform: Platform.Android,
            deviceKey: "android-policy-auth-device-key",
            deviceName: "Phone",
            timezoneId: "Asia/Seoul");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/devices/register")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add(AuthenticatedUserHeaderName, "authenticated-user");

        HttpResponseMessage response = await client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        DeviceRegistrationResponse body = (await response.Content.ReadFromJsonAsync<DeviceRegistrationResponse>())!;
        Assert.Equal("authenticated-user", body.UserId);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        DeviceEntity device = Assert.Single(await dbContext.Devices.ToListAsync());
        Assert.Equal("authenticated-user", device.UserId);
    }

    [Fact]
    public async Task RegisterDevice_WhenSameDeviceKeyIsRegisteredByDifferentAuthenticatedUsers_CreatesSeparateDevices()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase(requireAuthenticatedUser: true);
        using HttpClient client = factory.CreateClient();

        DeviceRegistrationResponse first = await RegisterDeviceAsync(
            client,
            authenticatedUserId: "user-a",
            payloadUserId: "payload-user-a",
            deviceKey: "shared-android-device-key");
        DeviceRegistrationResponse second = await RegisterDeviceAsync(
            client,
            authenticatedUserId: "user-b",
            payloadUserId: "payload-user-a",
            deviceKey: "shared-android-device-key");

        Assert.NotEqual(first.DeviceId, second.DeviceId);
        Assert.Equal("user-a", first.UserId);
        Assert.Equal("user-b", second.UserId);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Equal(2, await dbContext.Devices.CountAsync());
    }

    [Fact]
    public async Task RegisterDevice_WhenPayloadUserIdTargetsAnotherUser_DoesNotReturnExistingDeviceToken()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase(requireAuthenticatedUser: true);
        using HttpClient client = factory.CreateClient();

        DeviceRegistrationResponse first = await RegisterDeviceAsync(
            client,
            authenticatedUserId: "user-a",
            payloadUserId: "user-a",
            deviceKey: "shared-android-device-key");
        DeviceRegistrationResponse second = await RegisterDeviceAsync(
            client,
            authenticatedUserId: "user-b",
            payloadUserId: "user-a",
            deviceKey: "shared-android-device-key");

        Assert.NotEqual(first.DeviceId, second.DeviceId);
        Assert.NotEqual(first.DeviceToken, second.DeviceToken);
        Assert.Equal("user-b", second.UserId);
    }

    [Fact]
    public async Task UploadFocusSessions_WhenStrictModeDeviceTokenIsInvalid_ReturnsUnauthorizedAndPersistsNoRows()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase(requireAuthenticatedUser: true);
        using HttpClient client = factory.CreateClient();
        DeviceRegistrationResponse registration = await RegisterDeviceAsync(
            client,
            authenticatedUserId: "user-a",
            payloadUserId: "payload-user-a",
            deviceKey: "user-a-android-device-key");

        HttpResponseMessage response = await PostFocusUploadAsync(
            client,
            authenticatedUserId: "user-a",
            registration.DeviceId,
            "not-the-issued-device-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.FocusSessions.ToListAsync());
    }

    [Fact]
    public async Task UploadFocusSessions_WhenStrictModeAuthenticatedUserDoesNotOwnValidToken_ReturnsForbiddenAndPersistsNoRows()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase(requireAuthenticatedUser: true);
        using HttpClient client = factory.CreateClient();
        DeviceRegistrationResponse registration = await RegisterDeviceAsync(
            client,
            authenticatedUserId: "user-a",
            payloadUserId: "payload-user-a",
            deviceKey: "user-a-android-device-key-forbidden");

        HttpResponseMessage response = await PostFocusUploadAsync(
            client,
            authenticatedUserId: "user-b",
            registration.DeviceId,
            registration.DeviceToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.FocusSessions.ToListAsync());
    }

    private static async Task<DeviceRegistrationResponse> RegisterDeviceAsync(
        HttpClient client,
        string authenticatedUserId,
        string payloadUserId,
        string deviceKey)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/devices/register")
        {
            Content = JsonContent.Create(new RegisterDeviceRequest(
                userId: payloadUserId,
                platform: Platform.Android,
                deviceKey: deviceKey,
                deviceName: "Phone",
                timezoneId: "Asia/Seoul"))
        };
        httpRequest.Headers.Add(AuthenticatedUserHeaderName, authenticatedUserId);

        HttpResponseMessage response = await client.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<DeviceRegistrationResponse>())!;
    }

    private static async Task<HttpResponseMessage> PostFocusUploadAsync(
        HttpClient client,
        string authenticatedUserId,
        string deviceId,
        string deviceToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/focus-sessions/upload")
        {
            Content = JsonContent.Create(new UploadFocusSessionsRequest(
                deviceId,
                [
                    new FocusSessionUploadItem(
                        clientSessionId: "cross-user-focus-session",
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
        request.Headers.Add(AuthenticatedUserHeaderName, authenticatedUserId);
        request.Headers.Add(DeviceTokenAuthenticationService.HeaderName, deviceToken);

        return await client.SendAsync(request);
    }

    private static WebApplicationFactory<Program> CreateFactoryWithInMemoryDatabase(bool requireAuthenticatedUser)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["DeviceRegistrationAuth:RequireAuthenticatedUser"] = requireAuthenticatedUser.ToString()
                    });
                });
                builder.ConfigureServices(services =>
                {
                    string databaseName = $"server-registration-policy-tests-{Guid.NewGuid():N}";
                    services.RemoveAll<DbContextOptions<MonitorDbContext>>();
                    services.RemoveAll<DbContextOptions>();
                    services.AddDbContext<MonitorDbContext>(options =>
                        options.UseInMemoryDatabase(databaseName));
                });
            });
}
