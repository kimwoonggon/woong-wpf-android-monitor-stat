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

public sealed class DateRangeStatisticsApiTests
{
    [Fact]
    public async Task GetDateRangeStatistics_CombinesUserDevicesWithinInclusiveLocalDateRange()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        await SeedSessionsAsync(factory);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/statistics/range?userId=user-1&from=2026-04-28&to=2026-04-29&timezoneId=Asia%2FSeoul");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement root = json.RootElement;
        Assert.Equal("2026-04-28", root.GetProperty("fromDate").GetString());
        Assert.Equal("2026-04-29", root.GetProperty("toDate").GetString());
        Assert.Equal(1_500_000, root.GetProperty("totalActiveMs").GetInt64());
        Assert.Equal(120_000, root.GetProperty("totalIdleMs").GetInt64());
        Assert.Equal(540_000, root.GetProperty("totalWebMs").GetInt64());

        JsonElement topApps = root.GetProperty("topApps");
        Assert.Equal("Chrome", topApps[0].GetProperty("key").GetString());
        Assert.Equal(1_500_000, topApps[0].GetProperty("durationMs").GetInt64());

        JsonElement topDomains = root.GetProperty("topDomains");
        Assert.Equal("example.com", topDomains[0].GetProperty("key").GetString());
        Assert.Equal(540_000, topDomains[0].GetProperty("durationMs").GetInt64());
    }

    [Fact]
    public async Task GetDateRangeStatistics_SplitsCrossMidnightSessionsToOnlyInRangePortion()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Guid deviceId = Guid.NewGuid();
        DateTimeOffset startedAtUtc = new(2026, 4, 28, 14, 50, 0, TimeSpan.Zero);
        dbContext.Devices.Add(Device(deviceId, "user-1", Platform.Windows, "cross-midnight-key", "Windows PC"));
        dbContext.FocusSessions.AddRange(
            FocusAt(deviceId, "active-cross-midnight", "chrome.exe", startedAtUtc, 1_200_000, isIdle: false),
            FocusAt(deviceId, "idle-cross-midnight", "chrome.exe", startedAtUtc, 1_200_000, isIdle: true));
        dbContext.WebSessions.Add(Web(deviceId, "active-cross-midnight", "example.com", startedAtUtc, 1_200_000));
        await dbContext.SaveChangesAsync();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/statistics/range?userId=user-1&from=2026-04-29&to=2026-04-29&timezoneId=Asia%2FSeoul");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement root = json.RootElement;
        Assert.Equal(600_000, root.GetProperty("totalActiveMs").GetInt64());
        Assert.Equal(600_000, root.GetProperty("totalIdleMs").GetInt64());
        Assert.Equal(600_000, root.GetProperty("totalWebMs").GetInt64());
        Assert.Equal(600_000, root.GetProperty("topApps")[0].GetProperty("durationMs").GetInt64());
        Assert.Equal(600_000, root.GetProperty("topDomains")[0].GetProperty("durationMs").GetInt64());
    }

    [Theory]
    [InlineData("/api/statistics/range?userId=user-1&from=not-a-date&to=2026-04-29&timezoneId=Asia%2FSeoul")]
    [InlineData("/api/statistics/range?userId=user-1&from=2026-04-28&to=not-a-date&timezoneId=Asia%2FSeoul")]
    [InlineData("/api/statistics/range?userId=user-1&from=2026-04-30&to=2026-04-29&timezoneId=Asia%2FSeoul")]
    [InlineData("/api/statistics/range?userId=user-1&from=2026-04-28&to=2026-04-29&timezoneId=Invalid%2FZone")]
    [InlineData("/api/statistics/range?from=2026-04-28&to=2026-04-29&timezoneId=Asia%2FSeoul")]
    public async Task GetDateRangeStatistics_WhenQueryIsInvalid_ReturnsBadRequest(string url)
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        await SeedSessionsAsync(factory);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task SeedSessionsAsync(WebApplicationFactory<Program> factory)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Guid windowsDeviceId = Guid.NewGuid();
        Guid androidDeviceId = Guid.NewGuid();
        Guid otherDeviceId = Guid.NewGuid();

        dbContext.Devices.AddRange(
            Device(windowsDeviceId, "user-1", Platform.Windows, "windows-key", "Windows PC"),
            Device(androidDeviceId, "user-1", Platform.Android, "android-key", "Android Phone"),
            Device(otherDeviceId, "user-2", Platform.Windows, "other-key", "Other PC"));
        dbContext.FocusSessions.AddRange(
            Focus(windowsDeviceId, "windows-day-1", "chrome.exe", new DateOnly(2026, 4, 28), 600_000, isIdle: false),
            Focus(androidDeviceId, "android-day-1", "com.android.chrome", new DateOnly(2026, 4, 28), 300_000, isIdle: false),
            Focus(windowsDeviceId, "windows-day-2", "chrome.exe", new DateOnly(2026, 4, 29), 300_000, isIdle: false),
            Focus(androidDeviceId, "android-day-2", "com.android.chrome", new DateOnly(2026, 4, 29), 300_000, isIdle: false),
            Focus(windowsDeviceId, "windows-idle", "chrome.exe", new DateOnly(2026, 4, 28), 120_000, isIdle: true),
            Focus(windowsDeviceId, "outside-range", "chrome.exe", new DateOnly(2026, 4, 30), 999_000, isIdle: false),
            Focus(otherDeviceId, "other-user", "notepad.exe", new DateOnly(2026, 4, 28), 999_000, isIdle: false));
        dbContext.WebSessions.AddRange(
            Web(windowsDeviceId, "windows-day-1", "example.com", new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero), 240_000),
            Web(androidDeviceId, "android-day-2", "example.com", new DateTimeOffset(2026, 4, 28, 15, 0, 0, TimeSpan.Zero), 300_000),
            Web(windowsDeviceId, "outside-range", "example.com", new DateTimeOffset(2026, 4, 29, 15, 0, 0, TimeSpan.Zero), 999_000),
            Web(otherDeviceId, "other-user", "ignored.example", new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero), 999_000));

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
    {
        DateTimeOffset startedAtUtc = new DateTimeOffset(
            localDate.ToDateTime(new TimeOnly(9, 0)),
            TimeSpan.FromHours(9)).ToUniversalTime();

        return new()
        {
            DeviceId = deviceId,
            ClientSessionId = clientSessionId,
            PlatformAppKey = platformAppKey,
            StartedAtUtc = startedAtUtc,
            EndedAtUtc = startedAtUtc.AddMilliseconds(durationMs),
            DurationMs = durationMs,
            LocalDate = localDate,
            TimezoneId = "Asia/Seoul",
            IsIdle = isIdle,
            Source = "test"
        };
    }

    private static FocusSessionEntity FocusAt(
        Guid deviceId,
        string clientSessionId,
        string platformAppKey,
        DateTimeOffset startedAtUtc,
        long durationMs,
        bool isIdle)
        => new()
        {
            DeviceId = deviceId,
            ClientSessionId = clientSessionId,
            PlatformAppKey = platformAppKey,
            StartedAtUtc = startedAtUtc,
            EndedAtUtc = startedAtUtc.AddMilliseconds(durationMs),
            DurationMs = durationMs,
            LocalDate = new DateOnly(2026, 4, 28),
            TimezoneId = "Asia/Seoul",
            IsIdle = isIdle,
            Source = "test"
        };

    private static WebSessionEntity Web(
        Guid deviceId,
        string focusSessionId,
        string domain,
        DateTimeOffset startedAtUtc,
        long durationMs)
        => new()
        {
            DeviceId = deviceId,
            FocusSessionId = focusSessionId,
            BrowserFamily = "Chrome",
            Url = $"https://{domain}/docs/{focusSessionId}",
            Domain = domain,
            PageTitle = "Docs",
            StartedAtUtc = startedAtUtc,
            EndedAtUtc = startedAtUtc.AddMilliseconds(durationMs),
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
