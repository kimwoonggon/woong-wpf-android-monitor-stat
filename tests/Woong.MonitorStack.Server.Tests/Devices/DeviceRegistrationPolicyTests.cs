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
    private const string MissingServerUserAuthStack =
        "Public-release registration policy is not implemented yet: add server user/session auth first, then require registration to derive user identity from auth context instead of trusting payload userId.";

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

    [Fact(Skip = MissingServerUserAuthStack)]
    public Task RegisterDevice_WhenSameDeviceKeyIsRegisteredByDifferentUsers_CreatesSeparateDevices()
        => Task.CompletedTask;

    [Fact(Skip = MissingServerUserAuthStack)]
    public Task RegisterDevice_WhenPayloadUserIdTargetsAnotherUser_DoesNotReturnExistingDeviceToken()
        => Task.CompletedTask;

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
