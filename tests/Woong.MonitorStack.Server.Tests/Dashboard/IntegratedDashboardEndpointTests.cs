using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Tests.Dashboard;

public sealed class IntegratedDashboardEndpointTests
{
    [Fact]
    public async Task GetIntegratedDashboardApi_ReturnsWindowsAndAndroidTotals()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        await SeedIntegratedDataAsync(factory);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/dashboard/integrated?userId=user-1&from=2026-04-30&to=2026-04-30&timezoneId=UTC");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement root = json.RootElement;
        Assert.Equal(3_000_000, root.GetProperty("totalActiveMs").GetInt64());
        Assert.Equal(300_000, root.GetProperty("totalIdleMs").GetInt64());
        Assert.Equal(900_000, root.GetProperty("totalWebMs").GetInt64());
        Assert.Equal("windows", root.GetProperty("platformTotals")[0].GetProperty("platform").GetString());
        Assert.Equal("android", root.GetProperty("platformTotals")[1].GetProperty("platform").GetString());
    }

    [Fact]
    public async Task GetIntegratedDashboardPage_RendersBlazorDashboardShell()
    {
        await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDatabase();
        await SeedIntegratedDataAsync(factory);
        using HttpClient client = factory.CreateClient();

        string html = await client.GetStringAsync(
            "/dashboard?userId=user-1&from=2026-04-30&to=2026-04-30&timezoneId=UTC");

        Assert.Contains("Integrated Device Dashboard", html, StringComparison.Ordinal);
        Assert.Contains("Windows + Android", html, StringComparison.Ordinal);
        Assert.Contains("Active Focus", html, StringComparison.Ordinal);
        Assert.Contains("Chrome", html, StringComparison.Ordinal);
        Assert.Contains("github.com", html, StringComparison.Ordinal);
    }

    private static async Task SeedIntegratedDataAsync(WebApplicationFactory<Program> factory)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Guid windowsDeviceId = Guid.NewGuid();
        Guid androidDeviceId = Guid.NewGuid();
        DateTimeOffset startedAtUtc = new(2026, 4, 30, 0, 0, 0, TimeSpan.Zero);

        dbContext.Devices.AddRange(
            Device(windowsDeviceId, "user-1", Platform.Windows, "windows-key", "Windows PC"),
            Device(androidDeviceId, "user-1", Platform.Android, "android-key", "Android Phone"));
        dbContext.FocusSessions.AddRange(
            Focus(windowsDeviceId, "win-chrome", "chrome.exe", startedAtUtc, 1_800_000, isIdle: false),
            Focus(windowsDeviceId, "win-idle", "chrome.exe", startedAtUtc.AddHours(1), 300_000, isIdle: true),
            Focus(androidDeviceId, "android-chrome", "com.android.chrome", startedAtUtc.AddHours(2), 1_200_000, isIdle: false));
        dbContext.WebSessions.AddRange(
            Web(windowsDeviceId, "web-github", "win-chrome", "github.com", startedAtUtc, 600_000),
            Web(androidDeviceId, "web-chatgpt", "android-chrome", "chatgpt.com", startedAtUtc.AddHours(2), 300_000));
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
            TimezoneId = "UTC",
            DeviceTokenSalt = "salt",
            DeviceTokenHash = "hash",
            CreatedAtUtc = new DateTimeOffset(2026, 4, 30, 0, 0, 0, TimeSpan.Zero),
            LastSeenAtUtc = new DateTimeOffset(2026, 4, 30, 0, 0, 0, TimeSpan.Zero)
        };

    private static FocusSessionEntity Focus(
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
            LocalDate = DateOnly.FromDateTime(startedAtUtc.UtcDateTime),
            TimezoneId = "UTC",
            IsIdle = isIdle,
            Source = "test"
        };

    private static WebSessionEntity Web(
        Guid deviceId,
        string clientSessionId,
        string focusSessionId,
        string domain,
        DateTimeOffset startedAtUtc,
        long durationMs)
        => new()
        {
            DeviceId = deviceId,
            ClientSessionId = clientSessionId,
            FocusSessionId = focusSessionId,
            BrowserFamily = "Chrome",
            Domain = domain,
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
                    string databaseName = $"dashboard-tests-{Guid.NewGuid():N}";
                    services.RemoveAll<DbContextOptions<MonitorDbContext>>();
                    services.RemoveAll<DbContextOptions>();
                    services.AddDbContext<MonitorDbContext>(options =>
                        options.UseInMemoryDatabase(databaseName));
                });
            });
}
