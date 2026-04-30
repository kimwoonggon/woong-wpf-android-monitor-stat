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
using Woong.MonitorStack.Server.Tests.Data;

namespace Woong.MonitorStack.Server.Tests.Locations;

public sealed class LocationContextUploadApiTests
{
    [Fact]
    public async Task UploadLocationContexts_PersistsNullableCoordinatesAndMarksDuplicate()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        string deviceId = Guid.NewGuid().ToString("N");
        await SeedDeviceAsync(factory, Guid.Parse(deviceId));
        var item = new LocationContextUploadItem(
            clientContextId: "location-context-1",
            capturedAtUtc: new DateTimeOffset(2026, 4, 28, 1, 2, 3, TimeSpan.Zero),
            localDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul",
            latitude: 37.5665,
            longitude: 126.9780,
            accuracyMeters: 42.5,
            captureMode: "precise_opt_in",
            permissionState: "granted_precise",
            source: "android_location_context");
        var request = new UploadLocationContextsRequest(deviceId, [item]);

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/location-contexts/upload", request);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/location-contexts/upload", request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        using JsonDocument firstJson = await JsonDocument.ParseAsync(await firstResponse.Content.ReadAsStreamAsync());
        using JsonDocument secondJson = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());
        Assert.Equal((int)UploadItemStatus.Accepted, firstJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());
        Assert.Equal((int)UploadItemStatus.Duplicate, secondJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        LocationContextEntity persisted = Assert.Single(await dbContext.LocationContexts.ToListAsync());
        Assert.Equal(Guid.Parse(deviceId), persisted.DeviceId);
        Assert.Equal("location-context-1", persisted.ClientContextId);
        Assert.Equal(new DateTimeOffset(2026, 4, 28, 1, 2, 3, TimeSpan.Zero), persisted.CapturedAtUtc);
        Assert.Equal(new DateOnly(2026, 4, 28), persisted.LocalDate);
        Assert.Equal("Asia/Seoul", persisted.TimezoneId);
        Assert.Equal(37.5665, persisted.Latitude);
        Assert.Equal(126.9780, persisted.Longitude);
        Assert.Equal(42.5, persisted.AccuracyMeters);
        Assert.Equal("precise_opt_in", persisted.CaptureMode);
        Assert.Equal("granted_precise", persisted.PermissionState);
        Assert.Equal("android_location_context", persisted.Source);
    }

    [Fact]
    public async Task UploadLocationContexts_AllowsApproximateOrUnavailableCoordinates()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        string deviceId = Guid.NewGuid().ToString("N");
        await SeedDeviceAsync(factory, Guid.Parse(deviceId));
        var item = new LocationContextUploadItem(
            clientContextId: "location-context-unavailable",
            capturedAtUtc: new DateTimeOffset(2026, 4, 28, 2, 0, 0, TimeSpan.Zero),
            localDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul",
            latitude: null,
            longitude: null,
            accuracyMeters: null,
            captureMode: "location_unavailable",
            permissionState: "denied",
            source: "android_location_context");
        var request = new UploadLocationContextsRequest(deviceId, [item]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/location-contexts/upload", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        LocationContextEntity persisted = Assert.Single(await dbContext.LocationContexts.ToListAsync());
        Assert.Null(persisted.Latitude);
        Assert.Null(persisted.Longitude);
        Assert.Null(persisted.AccuracyMeters);
        Assert.Equal("location_unavailable", persisted.CaptureMode);
        Assert.Equal("denied", persisted.PermissionState);
    }

    [Fact]
    public async Task UploadLocationContexts_WhenBatchContainsExistingAndIntraBatchDuplicate_ReturnsIndependentStatuses()
    {
        using var factory = new RelationalServerFactory();
        using HttpClient client = factory.CreateClient();
        await factory.EnsureDatabaseCreatedAsync();
        Guid deviceId = Guid.NewGuid();
        await SeedDeviceAndExistingLocationContextAsync(factory, deviceId);
        var request = new UploadLocationContextsRequest(
            deviceId.ToString("N"),
            [
                Location("existing-location-context"),
                Location("new-location-context"),
                Location("new-location-context")
            ]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/location-contexts/upload", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement[] items = json.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal("existing-location-context", items[0].GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Duplicate, items[0].GetProperty("status").GetInt32());
        Assert.Equal("new-location-context", items[1].GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Accepted, items[1].GetProperty("status").GetInt32());
        Assert.Equal("new-location-context", items[2].GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Duplicate, items[2].GetProperty("status").GetInt32());

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        List<LocationContextEntity> persisted = await dbContext.LocationContexts
            .OrderBy(context => context.ClientContextId)
            .ToListAsync();
        Assert.Equal(
            ["existing-location-context", "new-location-context"],
            persisted.Select(context => context.ClientContextId));
    }

    [Fact]
    public async Task UploadLocationContexts_WhenDeviceIsNotRegistered_ReturnsControlledErrorAndDoesNotPersistRows()
    {
        using var factory = new RelationalServerFactory();
        using HttpClient client = factory.CreateClient();
        await factory.EnsureDatabaseCreatedAsync();
        string unknownDeviceId = Guid.NewGuid().ToString("N");
        var request = new UploadLocationContextsRequest(
            unknownDeviceId,
            [Location("orphan-location-context")]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/location-contexts/upload", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement item = json.RootElement.GetProperty("items")[0];
        Assert.Equal("orphan-location-context", item.GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Error, item.GetProperty("status").GetInt32());
        Assert.Contains("device", item.GetProperty("errorMessage").GetString(), StringComparison.OrdinalIgnoreCase);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.LocationContexts.ToListAsync());
    }

    private static async Task SeedDeviceAsync(WebApplicationFactory<Program> factory, Guid deviceId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        dbContext.Devices.Add(new DeviceEntity
        {
            Id = deviceId,
            UserId = "user-1",
            Platform = Platform.Android,
            DeviceKey = "android-location-key",
            DeviceName = "Android Phone",
            TimezoneId = "Asia/Seoul",
            CreatedAtUtc = new DateTimeOffset(2026, 4, 27, 0, 0, 0, TimeSpan.Zero),
            LastSeenAtUtc = new DateTimeOffset(2026, 4, 27, 0, 0, 0, TimeSpan.Zero)
        });
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedDeviceAndExistingLocationContextAsync(WebApplicationFactory<Program> factory, Guid deviceId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        dbContext.Devices.Add(new DeviceEntity
        {
            Id = deviceId,
            UserId = "user-1",
            Platform = Platform.Android,
            DeviceKey = "android-location-batch-key",
            DeviceName = "Android Phone",
            TimezoneId = "Asia/Seoul",
            CreatedAtUtc = new DateTimeOffset(2026, 4, 27, 0, 0, 0, TimeSpan.Zero),
            LastSeenAtUtc = new DateTimeOffset(2026, 4, 27, 0, 0, 0, TimeSpan.Zero)
        });
        LocationContextUploadItem existing = Location("existing-location-context");
        dbContext.LocationContexts.Add(new LocationContextEntity
        {
            DeviceId = deviceId,
            ClientContextId = existing.ClientContextId,
            CapturedAtUtc = existing.CapturedAtUtc,
            LocalDate = existing.LocalDate,
            TimezoneId = existing.TimezoneId,
            Latitude = existing.Latitude,
            Longitude = existing.Longitude,
            AccuracyMeters = existing.AccuracyMeters,
            CaptureMode = existing.CaptureMode,
            PermissionState = existing.PermissionState,
            Source = existing.Source,
            CreatedAtUtc = new DateTimeOffset(2026, 4, 28, 1, 2, 3, TimeSpan.Zero)
        });
        await dbContext.SaveChangesAsync();
    }

    private static LocationContextUploadItem Location(string clientContextId)
        => new(
            clientContextId,
            capturedAtUtc: new DateTimeOffset(2026, 4, 28, 1, 2, 3, TimeSpan.Zero),
            localDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul",
            latitude: 37.5665,
            longitude: 126.9780,
            accuracyMeters: 42.5,
            captureMode: "precise_opt_in",
            permissionState: "granted_precise",
            source: "android_location_context");

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
