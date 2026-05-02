using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Server.Dashboard;
using Woong.MonitorStack.Server.Data;
using Woong.MonitorStack.Server.Tests.Data;

namespace Woong.MonitorStack.Server.Tests.Dashboard;

public sealed class IntegratedDashboardQueryServiceTests
{
    [Fact]
    public async Task GetAsync_CombinesWindowsAndAndroidDeviceUsageForRange()
    {
        await using RelationalTestDatabase database = await RelationalTestDatabase.CreateAsync();
        Guid windowsDeviceId = Guid.NewGuid();
        Guid androidDeviceId = Guid.NewGuid();
        Guid otherUserDeviceId = Guid.NewGuid();
        DateTimeOffset dayStartUtc = new(2026, 4, 30, 0, 0, 0, TimeSpan.Zero);

        database.Context.Devices.AddRange(
            Device(windowsDeviceId, "user-1", Platform.Windows, "windows-key", "Windows PC"),
            Device(androidDeviceId, "user-1", Platform.Android, "android-key", "Android Phone"),
            Device(otherUserDeviceId, "user-2", Platform.Windows, "other-key", "Other PC"));
        database.Context.FocusSessions.AddRange(
            Focus(windowsDeviceId, "win-chrome", "chrome.exe", dayStartUtc, 3_600_000, isIdle: false),
            Focus(windowsDeviceId, "win-idle", "chrome.exe", dayStartUtc.AddHours(2), 300_000, isIdle: true),
            Focus(androidDeviceId, "android-chrome", "com.android.chrome", dayStartUtc.AddHours(3), 1_800_000, isIdle: false),
            Focus(otherUserDeviceId, "other-user", "chrome.exe", dayStartUtc, 9_999_999, isIdle: false));
        database.Context.WebSessions.AddRange(
            Web(windowsDeviceId, "web-github", "win-chrome", "github.com", dayStartUtc.AddMinutes(10), 1_200_000),
            Web(androidDeviceId, "web-chatgpt", "android-chrome", "chatgpt.com", dayStartUtc.AddHours(3), 600_000),
            Web(otherUserDeviceId, "web-other", "other-user", "ignored.example", dayStartUtc, 9_999_999));
        database.Context.LocationContexts.AddRange(
            Location(androidDeviceId, "loc-1", dayStartUtc.AddHours(3), 37.5665, 126.9780),
            Location(androidDeviceId, "loc-2", dayStartUtc.AddHours(4), 37.56654, 126.97804),
            Location(otherUserDeviceId, "loc-other", dayStartUtc, 0, 0));
        await database.Context.SaveChangesAsync();
        var service = new IntegratedDashboardQueryService(database.Context);

        IntegratedDashboardSnapshot snapshot = await service.GetAsync(
            "user-1",
            new DateOnly(2026, 4, 30),
            new DateOnly(2026, 4, 30),
            "UTC");

        Assert.Equal("user-1", snapshot.UserId);
        Assert.Equal(5_400_000, snapshot.TotalActiveMs);
        Assert.Equal(300_000, snapshot.TotalIdleMs);
        Assert.Equal(1_800_000, snapshot.TotalWebMs);
        Assert.Equal(2, snapshot.Devices.Count);
        Assert.Collection(
            snapshot.PlatformTotals,
            windows =>
            {
                Assert.Equal("windows", windows.Platform);
                Assert.Equal(3_600_000, windows.ActiveMs);
                Assert.Equal(300_000, windows.IdleMs);
                Assert.Equal(1_200_000, windows.WebMs);
            },
            android =>
            {
                Assert.Equal("android", android.Platform);
                Assert.Equal(1_800_000, android.ActiveMs);
                Assert.Equal(0, android.IdleMs);
                Assert.Equal(600_000, android.WebMs);
            });
        Assert.Equal("Chrome", snapshot.TopApps[0].Label);
        Assert.Equal(5_400_000, snapshot.TopApps[0].DurationMs);
        Assert.Equal("github.com", snapshot.TopDomains[0].Label);
        Assert.Equal(1_200_000, snapshot.TopDomains[0].DurationMs);
        Assert.Equal("37.5665,126.9780", snapshot.TopLocations[0].Label);
        Assert.Equal(2, snapshot.TopLocations[0].SampleCount);
    }

    [Fact]
    public async Task GetAsync_SeparatesWindowsAndroidAndCombinedUsageAndOrdersLocationRoute()
    {
        await using RelationalTestDatabase database = await RelationalTestDatabase.CreateAsync();
        Guid windowsDeviceId = Guid.NewGuid();
        Guid androidDeviceId = Guid.NewGuid();
        DateTimeOffset dayStartUtc = new(2026, 5, 2, 0, 0, 0, TimeSpan.Zero);

        database.Context.Devices.AddRange(
            Device(windowsDeviceId, "user-1", Platform.Windows, "windows-key", "Windows PC"),
            Device(androidDeviceId, "user-1", Platform.Android, "android-key", "Android Phone"));
        database.Context.FocusSessions.AddRange(
            Focus(windowsDeviceId, "win-vscode", "Code.exe", dayStartUtc.AddHours(9), 3_600_000, isIdle: false),
            Focus(windowsDeviceId, "win-chrome", "chrome.exe", dayStartUtc.AddHours(10), 1_800_000, isIdle: false),
            Focus(androidDeviceId, "android-youtube", "com.google.android.youtube", dayStartUtc.AddHours(11), 2_400_000, isIdle: false),
            Focus(androidDeviceId, "android-chrome", "com.android.chrome", dayStartUtc.AddHours(12), 1_200_000, isIdle: false));
        database.Context.WebSessions.AddRange(
            Web(windowsDeviceId, "web-github", "win-chrome", "github.com", dayStartUtc.AddHours(10), 1_200_000),
            Web(androidDeviceId, "web-mobile", "android-chrome", "m.example", dayStartUtc.AddHours(12), 600_000));
        database.Context.LocationContexts.AddRange(
            Location(androidDeviceId, "loc-home", dayStartUtc.AddHours(8), 37.5665, 126.9780),
            Location(androidDeviceId, "loc-office", dayStartUtc.AddHours(9), 37.5700, 126.9820),
            Location(androidDeviceId, "loc-cafe", dayStartUtc.AddHours(12), 37.5750, 126.9900));
        await database.Context.SaveChangesAsync();
        var service = new IntegratedDashboardQueryService(database.Context);

        IntegratedDashboardSnapshot snapshot = await service.GetAsync(
            "user-1",
            new DateOnly(2026, 5, 2),
            new DateOnly(2026, 5, 2),
            "UTC");

        IntegratedPlatformUsage windows = Assert.Single(
            snapshot.PlatformUsage,
            usage => usage.Platform == "windows");
        IntegratedPlatformUsage android = Assert.Single(
            snapshot.PlatformUsage,
            usage => usage.Platform == "android");

        Assert.Equal(5_400_000, windows.ActiveMs);
        Assert.Equal("VS Code", windows.TopApps[0].Label);
        Assert.Equal("github.com", windows.TopDomains[0].Label);
        Assert.Equal(3_600_000, android.ActiveMs);
        Assert.Equal("YouTube", android.TopApps[0].Label);
        Assert.Equal("m.example", android.TopDomains[0].Label);
        Assert.Equal(9_000_000, snapshot.TotalActiveMs);
        Assert.Collection(
            snapshot.LocationRoute,
            point => Assert.Equal("loc-home", point.ClientContextId),
            point => Assert.Equal("loc-office", point.ClientContextId),
            point => Assert.Equal("loc-cafe", point.ClientContextId));
        Assert.Equal(37.5750, snapshot.LocationRoute[^1].Latitude);
        Assert.Equal(126.9900, snapshot.LocationRoute[^1].Longitude);
    }

    [Fact]
    public async Task GetAsync_FiltersWebSessionsAndLocationSamplesByRequestedTimezoneLocalDate()
    {
        await using RelationalTestDatabase database = await RelationalTestDatabase.CreateAsync();
        Guid androidDeviceId = Guid.NewGuid();

        database.Context.Devices.Add(
            Device(androidDeviceId, "user-1", Platform.Android, "android-key", "Android Phone"));
        database.Context.FocusSessions.AddRange(
            Focus(
                androidDeviceId,
                "focus-kst-may-1",
                "com.android.chrome",
                new DateTimeOffset(2026, 4, 30, 15, 30, 0, TimeSpan.Zero),
                900_000,
                isIdle: false),
            Focus(
                androidDeviceId,
                "focus-kst-may-2",
                "com.android.chrome",
                new DateTimeOffset(2026, 5, 1, 15, 30, 0, TimeSpan.Zero),
                1_800_000,
                isIdle: false));
        database.Context.WebSessions.AddRange(
            Web(
                androidDeviceId,
                "web-kst-may-1",
                "focus-kst-may-1",
                "included.example",
                new DateTimeOffset(2026, 4, 30, 15, 30, 0, TimeSpan.Zero),
                900_000),
            Web(
                androidDeviceId,
                "web-kst-may-2",
                "focus-kst-may-2",
                "excluded.example",
                new DateTimeOffset(2026, 5, 1, 15, 30, 0, TimeSpan.Zero),
                1_800_000));
        database.Context.LocationContexts.AddRange(
            Location(
                androidDeviceId,
                "loc-kst-may-1",
                new DateTimeOffset(2026, 4, 30, 15, 45, 0, TimeSpan.Zero),
                37.5665,
                126.9780),
            Location(
                androidDeviceId,
                "loc-kst-may-2",
                new DateTimeOffset(2026, 5, 1, 15, 15, 0, TimeSpan.Zero),
                35.1796,
                129.0756));
        await database.Context.SaveChangesAsync();
        var service = new IntegratedDashboardQueryService(database.Context);

        IntegratedDashboardSnapshot snapshot = await service.GetAsync(
            "user-1",
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 1),
            "Asia/Seoul");

        Assert.Equal(900_000, snapshot.TotalWebMs);
        Assert.Collection(
            snapshot.TopDomains,
            domain =>
            {
                Assert.Equal("included.example", domain.Label);
                Assert.Equal(900_000, domain.DurationMs);
            });
        Assert.Collection(
            snapshot.TopLocations,
            location =>
            {
                Assert.Equal("37.5665,126.9780", location.Label);
                Assert.Equal(1, location.SampleCount);
            });
    }

    [Fact]
    public async Task GetAsync_SplitsFocusAndWebDurationsAtRequestedTimezoneRangeBoundary()
    {
        await using RelationalTestDatabase database = await RelationalTestDatabase.CreateAsync();
        Guid windowsDeviceId = Guid.NewGuid();
        DateTimeOffset startUtc = new(2026, 4, 30, 14, 50, 0, TimeSpan.Zero);

        database.Context.Devices.Add(
            Device(windowsDeviceId, "user-1", Platform.Windows, "windows-key", "Windows PC"));
        database.Context.FocusSessions.Add(
            Focus(
                windowsDeviceId,
                "focus-cross-midnight",
                "chrome.exe",
                startUtc,
                1_200_000,
                isIdle: false));
        database.Context.WebSessions.Add(
            Web(
                windowsDeviceId,
                "web-cross-midnight",
                "focus-cross-midnight",
                "github.com",
                startUtc,
                1_200_000));
        await database.Context.SaveChangesAsync();
        var service = new IntegratedDashboardQueryService(database.Context);

        IntegratedDashboardSnapshot snapshot = await service.GetAsync(
            "user-1",
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 1),
            "Asia/Seoul");

        Assert.Equal(600_000, snapshot.TotalActiveMs);
        Assert.Equal(600_000, snapshot.TotalWebMs);
        Assert.Equal(600_000, snapshot.TopApps[0].DurationMs);
        Assert.Equal(600_000, snapshot.TopDomains[0].DurationMs);
        Assert.Equal(600_000, snapshot.Devices[0].ActiveMs);
        Assert.Equal(600_000, snapshot.Devices[0].WebMs);
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
            DurationMs = durationMs,
            CaptureMethod = "BrowserExtensionFuture",
            CaptureConfidence = "High"
        };

    private static LocationContextEntity Location(
        Guid deviceId,
        string clientContextId,
        DateTimeOffset capturedAtUtc,
        double latitude,
        double longitude)
        => new()
        {
            DeviceId = deviceId,
            ClientContextId = clientContextId,
            CapturedAtUtc = capturedAtUtc,
            LocalDate = DateOnly.FromDateTime(capturedAtUtc.UtcDateTime),
            TimezoneId = "UTC",
            Latitude = latitude,
            Longitude = longitude,
            AccuracyMeters = 100,
            CaptureMode = "ForegroundOnly",
            PermissionState = "Granted",
            Source = "test",
            CreatedAtUtc = capturedAtUtc
        };
}
