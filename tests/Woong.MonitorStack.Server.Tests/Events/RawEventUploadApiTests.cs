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

namespace Woong.MonitorStack.Server.Tests.Events;

public sealed class RawEventUploadApiTests
{
    private const string DeviceTokenHeaderName = "X-Device-Token";

    [Fact]
    public async Task UploadRawEvents_WhenDeviceTokenHeaderIsMissing_ReturnsUnauthorizedAndPersistsNoRows()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        DeviceRegistration registration = await RegisterDeviceAsync(client, "windows-raw-event-missing-token-key");
        var request = new UploadRawEventsRequest(registration.DeviceId, [Raw("missing-token-raw-event")]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/raw-events/upload", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.RawEvents.ToListAsync());
    }

    [Fact]
    public async Task UploadRawEvents_PersistsNewEventAndMarksDuplicateRetry()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        DeviceRegistration registration = await RegisterDeviceAsync(client, "windows-raw-event-key");
        var item = new RawEventUploadItem(
            clientEventId: "event-1",
            eventType: "foreground_window",
            occurredAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            payloadJson: """{"processName":"Code.exe","windowTitle":"Codex"}""");
        var request = new UploadRawEventsRequest(registration.DeviceId, [item]);

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, registration.DeviceToken);
        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/raw-events/upload", request);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/raw-events/upload", request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        using JsonDocument firstJson = await JsonDocument.ParseAsync(await firstResponse.Content.ReadAsStreamAsync());
        using JsonDocument secondJson = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());
        Assert.Equal((int)UploadItemStatus.Accepted, firstJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());
        Assert.Equal((int)UploadItemStatus.Duplicate, secondJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        RawEventEntity persisted = Assert.Single(await dbContext.RawEvents.ToListAsync());
        Assert.Equal(Guid.ParseExact(registration.DeviceId, "N"), persisted.DeviceId);
        Assert.Equal("event-1", persisted.ClientEventId);
        Assert.Equal("foreground_window", persisted.EventType);
        Assert.Equal("""{"processName":"Code.exe","windowTitle":"Codex"}""", persisted.PayloadJson);
    }

    [Fact]
    public async Task UploadRawEvents_WhenDeviceIsNotRegistered_ReturnsControlledErrorAndDoesNotPersistRows()
    {
        using var factory = new RelationalServerFactory();
        using HttpClient client = factory.CreateClient();
        await factory.EnsureDatabaseCreatedAsync();
        string unknownDeviceId = Guid.NewGuid().ToString("N");
        var request = new UploadRawEventsRequest(
            unknownDeviceId,
            [
                new RawEventUploadItem(
                    clientEventId: "orphan-raw-event",
                    eventType: "foreground_window",
                    occurredAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
                    payloadJson: """{"processName":"Code.exe"}""")
            ]);

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, "not-a-valid-device-token");
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/raw-events/upload", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.RawEvents.ToListAsync());
    }

    [Fact]
    public async Task UploadRawEvents_WhenBatchContainsExistingAndIntraBatchDuplicate_ReturnsIndependentStatuses()
    {
        using var factory = new RelationalServerFactory();
        using HttpClient client = factory.CreateClient();
        await factory.EnsureDatabaseCreatedAsync();
        DeviceRegistration registration = await RegisterDeviceAsync(client, "windows-raw-event-batch-key");
        await SeedExistingRawEventAsync(factory, Guid.ParseExact(registration.DeviceId, "N"));
        var request = new UploadRawEventsRequest(
            registration.DeviceId,
            [
                Raw("existing-raw-event"),
                Raw("new-raw-event"),
                Raw("new-raw-event")
            ]);

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, registration.DeviceToken);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/raw-events/upload", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement[] items = json.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal("existing-raw-event", items[0].GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Duplicate, items[0].GetProperty("status").GetInt32());
        Assert.Equal("new-raw-event", items[1].GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Accepted, items[1].GetProperty("status").GetInt32());
        Assert.Equal("new-raw-event", items[2].GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Duplicate, items[2].GetProperty("status").GetInt32());

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        List<RawEventEntity> persisted = await dbContext.RawEvents
            .OrderBy(rawEvent => rawEvent.ClientEventId)
            .ToListAsync();
        Assert.Equal(["existing-raw-event", "new-raw-event"], persisted.Select(rawEvent => rawEvent.ClientEventId));
    }

    [Fact]
    public async Task UploadRawEvents_WhenPayloadContainsForbiddenUserInputMetadata_ReturnsErrorAndDoesNotPersistIt()
    {
        using var factory = new RelationalServerFactory();
        using HttpClient client = factory.CreateClient();
        await factory.EnsureDatabaseCreatedAsync();
        DeviceRegistration registration = await RegisterDeviceAsync(client, "windows-raw-event-privacy-key");
        var request = new UploadRawEventsRequest(
            registration.DeviceId,
            [
                Raw("safe-metadata-event"),
                new RawEventUploadItem(
                    clientEventId: "forbidden-typed-text-event",
                    eventType: "keyboard_debug",
                    occurredAtUtc: new DateTimeOffset(2026, 4, 27, 15, 1, 0, TimeSpan.Zero),
                    payloadJson: """{"processName":"Code.exe","typedText":"secret"}""")
            ]);

        client.DefaultRequestHeaders.Add(DeviceTokenHeaderName, registration.DeviceToken);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/raw-events/upload", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement[] items = json.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal("safe-metadata-event", items[0].GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Accepted, items[0].GetProperty("status").GetInt32());
        Assert.Equal("forbidden-typed-text-event", items[1].GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Error, items[1].GetProperty("status").GetInt32());
        Assert.Contains("forbidden", items[1].GetProperty("errorMessage").GetString(), StringComparison.OrdinalIgnoreCase);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        RawEventEntity persisted = Assert.Single(await dbContext.RawEvents.ToListAsync());
        Assert.Equal("safe-metadata-event", persisted.ClientEventId);
    }

    private static async Task SeedExistingRawEventAsync(WebApplicationFactory<Program> factory, Guid deviceId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        RawEventUploadItem existing = Raw("existing-raw-event");
        dbContext.RawEvents.Add(new RawEventEntity
        {
            DeviceId = deviceId,
            ClientEventId = existing.ClientEventId,
            EventType = existing.EventType,
            OccurredAtUtc = existing.OccurredAtUtc,
            PayloadJson = existing.PayloadJson
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

    private static RawEventUploadItem Raw(string clientEventId)
        => new(
            clientEventId,
            eventType: "foreground_window",
            occurredAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            payloadJson: """{"processName":"Code.exe"}""");

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
