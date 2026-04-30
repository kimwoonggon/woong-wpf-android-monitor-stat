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
