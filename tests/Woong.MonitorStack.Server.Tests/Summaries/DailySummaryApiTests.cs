using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Tests.Summaries;

public sealed class DailySummaryApiTests
{
    [Fact]
    public async Task GetDailySummary_CombinesUserDevicesAndExcludesIdleFromActiveTotal()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        await SeedUserSessionsAsync(factory);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/daily-summaries/2026-04-28?userId=user-1&timezoneId=Asia%2FSeoul");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement root = json.RootElement;
        Assert.Equal("2026-04-28", root.GetProperty("summaryDate").GetString());
        Assert.Equal(900_000, root.GetProperty("totalActiveMs").GetInt64());
        Assert.Equal(120_000, root.GetProperty("totalIdleMs").GetInt64());
        Assert.Equal(240_000, root.GetProperty("totalWebMs").GetInt64());

        JsonElement topApps = root.GetProperty("topApps");
        Assert.Equal("Chrome", topApps[0].GetProperty("key").GetString());
        Assert.Equal(900_000, topApps[0].GetProperty("durationMs").GetInt64());

        JsonElement topDomain = root.GetProperty("topDomains")[0];
        Assert.Equal("example.com", topDomain.GetProperty("key").GetString());
        Assert.Equal(240_000, topDomain.GetProperty("durationMs").GetInt64());
    }

    private static async Task SeedUserSessionsAsync(WebApplicationFactory<Program> factory)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Guid windowsDeviceId = Guid.NewGuid();
        Guid androidDeviceId = Guid.NewGuid();
        Guid otherDeviceId = Guid.NewGuid();
        var summaryDate = new DateOnly(2026, 4, 28);

        dbContext.Devices.AddRange(
            Device(windowsDeviceId, "user-1", Platform.Windows, "windows-key", "Windows PC"),
            Device(androidDeviceId, "user-1", Platform.Android, "android-key", "Android Phone"),
            Device(otherDeviceId, "user-2", Platform.Windows, "other-key", "Other PC"));
        dbContext.FocusSessions.AddRange(
            Focus(windowsDeviceId, "windows-active", "chrome.exe", summaryDate, 600_000, isIdle: false),
            Focus(androidDeviceId, "android-active", "com.android.chrome", summaryDate, 300_000, isIdle: false),
            Focus(windowsDeviceId, "windows-idle", "chrome.exe", summaryDate, 120_000, isIdle: true),
            Focus(otherDeviceId, "other-active", "notepad.exe", summaryDate, 999_000, isIdle: false));
        dbContext.WebSessions.AddRange(
            Web(windowsDeviceId, "windows-active", "example.com", 240_000),
            Web(otherDeviceId, "other-active", "ignored.example", 999_000));

        await dbContext.SaveChangesAsync();
    }

    private static DeviceEntity Device(
        Guid id,
        string userId,
        Platform platform,
        string deviceKey,
        string deviceName)
        => new()
        {
            Id = id,
            UserId = userId,
            Platform = platform,
            DeviceKey = deviceKey,
            DeviceName = deviceName,
            TimezoneId = "Asia/Seoul",
            CreatedAtUtc = new DateTimeOffset(2026, 4, 27, 0, 0, 0, TimeSpan.Zero),
            LastSeenAtUtc = new DateTimeOffset(2026, 4, 27, 0, 0, 0, TimeSpan.Zero)
        };

    private static FocusSessionEntity Focus(
        Guid deviceId,
        string clientSessionId,
        string platformAppKey,
        DateOnly localDate,
        long durationMs,
        bool isIdle)
        => new()
        {
            DeviceId = deviceId,
            ClientSessionId = clientSessionId,
            PlatformAppKey = platformAppKey,
            StartedAtUtc = new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            EndedAtUtc = new DateTimeOffset(2026, 4, 27, 15, 10, 0, TimeSpan.Zero),
            DurationMs = durationMs,
            LocalDate = localDate,
            TimezoneId = "Asia/Seoul",
            IsIdle = isIdle,
            Source = "test"
        };

    private static WebSessionEntity Web(
        Guid deviceId,
        string focusSessionId,
        string domain,
        long durationMs)
        => new()
        {
            DeviceId = deviceId,
            FocusSessionId = focusSessionId,
            BrowserFamily = "Chrome",
            Url = $"https://{domain}/docs",
            Domain = domain,
            PageTitle = "Docs",
            StartedAtUtc = new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            EndedAtUtc = new DateTimeOffset(2026, 4, 27, 15, 4, 0, TimeSpan.Zero),
            DurationMs = durationMs
        };

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
