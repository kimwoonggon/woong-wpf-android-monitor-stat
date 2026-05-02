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
