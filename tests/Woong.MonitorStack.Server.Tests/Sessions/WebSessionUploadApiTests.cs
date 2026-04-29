using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Tests.Sessions;

public sealed class WebSessionUploadApiTests
{
    [Fact]
    public async Task UploadWebSessions_PersistsNewSessionAndMarksDuplicateRetry()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        string deviceId = Guid.NewGuid().ToString("N");
        var session = new WebSessionUploadItem(
            clientSessionId: "web-session-1",
            focusSessionId: "client-session-1",
            browserFamily: "Chrome",
            url: "https://example.com/docs",
            domain: "example.com",
            pageTitle: "Docs",
            startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 10, 0, TimeSpan.Zero),
            durationMs: 600_000);
        var request = new UploadWebSessionsRequest(deviceId, [session]);

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/web-sessions/upload", request);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/web-sessions/upload", request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        using JsonDocument firstJson = await JsonDocument.ParseAsync(await firstResponse.Content.ReadAsStreamAsync());
        using JsonDocument secondJson = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());
        Assert.Equal((int)UploadItemStatus.Accepted, firstJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());
        Assert.Equal((int)UploadItemStatus.Duplicate, secondJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        WebSessionEntity persisted = Assert.Single(await dbContext.WebSessions.ToListAsync());
        Assert.Equal(Guid.Parse(deviceId), persisted.DeviceId);
        Assert.Equal("web-session-1", persisted.ClientSessionId);
        Assert.Equal("client-session-1", persisted.FocusSessionId);
        Assert.Equal("example.com", persisted.Domain);
        Assert.Equal("Docs", persisted.PageTitle);
        Assert.Equal(600_000, persisted.DurationMs);
    }

    [Fact]
    public async Task UploadWebSessions_WhenUrlIsNull_PreservesCaptureMetadataAndMarksDuplicateRetry()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        string deviceId = Guid.NewGuid().ToString("N");
        var session = new WebSessionUploadItem(
            clientSessionId: "domain-only-web-session-1",
            focusSessionId: "domain-only-session-1",
            browserFamily: "Chrome",
            url: null,
            domain: "github.com",
            pageTitle: null,
            startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 10, 0, TimeSpan.Zero),
            durationMs: 600_000,
            captureMethod: "UIAutomationAddressBar",
            captureConfidence: "High",
            isPrivateOrUnknown: false);
        var request = new UploadWebSessionsRequest(deviceId, [session]);

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/web-sessions/upload", request);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/web-sessions/upload", request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        using JsonDocument firstJson = await JsonDocument.ParseAsync(await firstResponse.Content.ReadAsStreamAsync());
        using JsonDocument secondJson = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());
        Assert.Equal((int)UploadItemStatus.Accepted, firstJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());
        Assert.Equal((int)UploadItemStatus.Duplicate, secondJson.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        WebSessionEntity persisted = Assert.Single(await dbContext.WebSessions.ToListAsync());
        Assert.Equal("domain-only-web-session-1", persisted.ClientSessionId);
        Assert.Null(persisted.Url);
        Assert.Equal("github.com", persisted.Domain);
        Assert.Null(persisted.PageTitle);
        Assert.Equal("UIAutomationAddressBar", persisted.CaptureMethod);
        Assert.Equal("High", persisted.CaptureConfidence);
        Assert.False(persisted.IsPrivateOrUnknown);
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
