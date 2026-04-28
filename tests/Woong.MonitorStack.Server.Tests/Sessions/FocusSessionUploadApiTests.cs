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

public sealed class FocusSessionUploadApiTests
{
    [Fact]
    public async Task UploadFocusSessions_PersistsNewSessionAndMarksDuplicateRetry()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using HttpClient client = factory.CreateClient();
        string deviceId = Guid.NewGuid().ToString("N");
        var session = new FocusSessionUploadItem(
            clientSessionId: "client-session-1",
            platformAppKey: "chrome.exe",
            startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 10, 0, TimeSpan.Zero),
            durationMs: 600_000,
            localDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "foreground_window");
        var request = new UploadFocusSessionsRequest(deviceId, [session]);

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
        Assert.Equal(Guid.Parse(deviceId), persisted.DeviceId);
        Assert.Equal("client-session-1", persisted.ClientSessionId);
        Assert.Equal("chrome.exe", persisted.PlatformAppKey);
        Assert.Equal(600_000, persisted.DurationMs);
        Assert.False(persisted.IsIdle);
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
