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

namespace Woong.MonitorStack.Server.Tests.CurrentApps;

public sealed class CurrentAppStateUploadApiTests
{
    private const string DeviceTokenHeaderName = "X-Device-Token";

    [Fact]
    public async Task UploadCurrentAppStates_WithValidDeviceToken_UpsertsLatestStatePerDevice()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        DeviceRegistration registration = await RegisterDeviceAsync(client);
        var olderState = CurrentState(
            "current-state-1",
            "Code.exe",
            new DateTimeOffset(2026, 5, 3, 12, 0, 0, TimeSpan.Zero),
            processName: "Code.exe",
            windowTitle: "Allowed by existing privacy setting");
        var newerState = CurrentState(
            "current-state-2",
            "chrome.exe",
            new DateTimeOffset(2026, 5, 3, 12, 5, 0, TimeSpan.Zero),
            processName: "chrome.exe",
            windowTitle: null);

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, registration.DeviceToken);
        HttpResponseMessage firstResponse = await client.PostAsJsonAsync(
            "/api/current-app-states/upload",
            new UploadCurrentAppStatesRequest(registration.DeviceId, [olderState]));
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync(
            "/api/current-app-states/upload",
            new UploadCurrentAppStatesRequest(registration.DeviceId, [newerState]));

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        using JsonDocument firstJson = await JsonDocument.ParseAsync(await firstResponse.Content.ReadAsStreamAsync());
        using JsonDocument secondJson = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());
        Assert.Equal((int)UploadItemStatus.Accepted, firstJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());
        Assert.Equal((int)UploadItemStatus.Accepted, secondJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        CurrentAppStateEntity persisted = Assert.Single(await dbContext.CurrentAppStates.ToListAsync());
        Assert.Equal(Guid.ParseExact(registration.DeviceId, "N"), persisted.DeviceId);
        Assert.Equal("current-state-2", persisted.ClientStateId);
        Assert.Equal(Platform.Windows, persisted.Platform);
        Assert.Equal("chrome.exe", persisted.PlatformAppKey);
        Assert.Equal(new DateTimeOffset(2026, 5, 3, 12, 5, 0, TimeSpan.Zero), persisted.ObservedAtUtc);
        Assert.Equal(new DateOnly(2026, 5, 3), persisted.LocalDate);
        Assert.Equal("UTC", persisted.TimezoneId);
        Assert.Equal("Active", persisted.Status);
        Assert.Equal("foreground_window", persisted.Source);
        Assert.Equal("chrome.exe", persisted.ProcessName);
        Assert.Null(persisted.WindowTitle);
    }

    [Fact]
    public async Task UploadCurrentAppStates_IgnoresOlderObservedAtUtcForSameDevice()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        DeviceRegistration registration = await RegisterDeviceAsync(client);
        var newerState = CurrentState(
            "current-state-new",
            "chrome.exe",
            new DateTimeOffset(2026, 5, 3, 12, 5, 0, TimeSpan.Zero));
        var olderState = CurrentState(
            "current-state-old",
            "Code.exe",
            new DateTimeOffset(2026, 5, 3, 12, 0, 0, TimeSpan.Zero));

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, registration.DeviceToken);
        HttpResponseMessage firstResponse = await client.PostAsJsonAsync(
            "/api/current-app-states/upload",
            new UploadCurrentAppStatesRequest(registration.DeviceId, [newerState]));
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync(
            "/api/current-app-states/upload",
            new UploadCurrentAppStatesRequest(registration.DeviceId, [olderState]));

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        using JsonDocument secondJson = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());
        Assert.Equal((int)UploadItemStatus.Duplicate, secondJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        CurrentAppStateEntity persisted = Assert.Single(await dbContext.CurrentAppStates.ToListAsync());
        Assert.Equal("current-state-new", persisted.ClientStateId);
        Assert.Equal("chrome.exe", persisted.PlatformAppKey);
        Assert.Equal(new DateTimeOffset(2026, 5, 3, 12, 5, 0, TimeSpan.Zero), persisted.ObservedAtUtc);
    }

    private static CurrentAppStateUploadItem CurrentState(
        string clientStateId,
        string platformAppKey,
        DateTimeOffset observedAtUtc,
        string? processName = null,
        string? windowTitle = null)
        => new(
            clientStateId,
            Platform.Windows,
            platformAppKey,
            observedAtUtc,
            new DateOnly(2026, 5, 3),
            timezoneId: "UTC",
            status: "Active",
            source: "foreground_window",
            processId: 4321,
            processName,
            processPath: @"C:\Program Files\App\app.exe",
            windowHandle: 123456,
            windowTitle);

    private static async Task<DeviceRegistration> RegisterDeviceAsync(HttpClient client)
    {
        var registrationRequest = new RegisterDeviceRequest(
            userId: "user-1",
            platform: Platform.Windows,
            deviceKey: "windows-current-app-key",
            deviceName: "Windows Workstation",
            timezoneId: "UTC");

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
                    string databaseName = $"current-app-state-tests-{Guid.NewGuid():N}";
                    services.RemoveAll<DbContextOptions<MonitorDbContext>>();
                    services.RemoveAll<DbContextOptions>();
                    services.AddDbContext<MonitorDbContext>(options =>
                        options.UseInMemoryDatabase(databaseName));
                });
            });
}
