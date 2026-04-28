using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Server.Data;
using Woong.MonitorStack.Server.Summaries;

namespace Woong.MonitorStack.Server.Tests.Summaries;

public sealed class DailySummaryAggregationServiceTests
{
    [Fact]
    public async Task GenerateAsync_PersistsIntegratedDailySummaryForUserTimezone()
    {
        var options = new DbContextOptionsBuilder<MonitorDbContext>()
            .UseInMemoryDatabase($"summary-aggregation-{Guid.NewGuid():N}")
            .Options;
        await using var dbContext = new MonitorDbContext(options);
        await SeedSessionsAsync(dbContext);
        var service = new DailySummaryAggregationService(dbContext);
        var generatedAtUtc = new DateTimeOffset(2026, 4, 28, 23, 0, 0, TimeSpan.Zero);

        DailySummaryEntity summary = await service.GenerateAsync(
            userId: "user-1",
            summaryDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul",
            generatedAtUtc: generatedAtUtc);

        Assert.Equal("user-1", summary.UserId);
        Assert.Equal(new DateOnly(2026, 4, 28), summary.SummaryDate);
        Assert.Equal("Asia/Seoul", summary.TimezoneId);
        Assert.Equal(900_000, summary.TotalActiveMs);
        Assert.Equal(120_000, summary.TotalIdleMs);
        Assert.Equal(240_000, summary.TotalWebMs);
        Assert.Equal(generatedAtUtc, summary.GeneratedAtUtc);
        Assert.Contains("Chrome", summary.TopAppsJson, StringComparison.Ordinal);
        Assert.Contains("example.com", summary.TopDomainsJson, StringComparison.Ordinal);

        DailySummaryEntity persisted = Assert.Single(dbContext.DailySummaries);
        Assert.Equal(summary.TotalActiveMs, persisted.TotalActiveMs);
    }

    [Fact]
    public async Task GenerateAsync_GroupsKnownPlatformAppsIntoSharedAppFamily()
    {
        var options = new DbContextOptionsBuilder<MonitorDbContext>()
            .UseInMemoryDatabase($"summary-family-{Guid.NewGuid():N}")
            .Options;
        await using var dbContext = new MonitorDbContext(options);
        await SeedSessionsAsync(dbContext);
        var service = new DailySummaryAggregationService(dbContext);

        DailySummaryEntity summary = await service.GenerateAsync(
            userId: "user-1",
            summaryDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul",
            generatedAtUtc: new DateTimeOffset(2026, 4, 28, 23, 0, 0, TimeSpan.Zero));

        List<UsageTotal> topApps = JsonSerializer.Deserialize<List<UsageTotal>>(summary.TopAppsJson)!;
        Assert.Equal("Chrome", topApps[0].Key);
        Assert.Equal(900_000, topApps[0].DurationMs);
    }

    private static async Task SeedSessionsAsync(MonitorDbContext dbContext)
    {
        Guid windowsDeviceId = Guid.NewGuid();
        Guid androidDeviceId = Guid.NewGuid();
        Guid otherDeviceId = Guid.NewGuid();
        var summaryDate = new DateOnly(2026, 4, 28);

        dbContext.Devices.AddRange(
            Device(windowsDeviceId, "user-1", Platform.Windows, "windows-key"),
            Device(androidDeviceId, "user-1", Platform.Android, "android-key"),
            Device(otherDeviceId, "user-2", Platform.Windows, "other-key"));
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
        string deviceKey)
        => new()
        {
            Id = id,
            UserId = userId,
            Platform = platform,
            DeviceKey = deviceKey,
            DeviceName = deviceKey,
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
}
