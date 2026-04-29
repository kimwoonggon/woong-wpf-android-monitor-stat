using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Tests.Data;

public sealed class RelationalMonitorDbContextTests
{
    [Fact]
    public async Task DeviceUniqueIndex_IsEnforcedByRelationalProvider()
    {
        await using var database = await RelationalTestDatabase.CreateAsync();

        database.Context.Devices.Add(new DeviceEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Platform = Platform.Windows,
            DeviceKey = "device-key",
            DeviceName = "Workstation",
            TimezoneId = "Asia/Seoul",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastSeenAtUtc = DateTimeOffset.UtcNow
        });
        await database.Context.SaveChangesAsync();

        database.Context.Devices.Add(new DeviceEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Platform = Platform.Windows,
            DeviceKey = "device-key",
            DeviceName = "Workstation duplicate",
            TimezoneId = "Asia/Seoul",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastSeenAtUtc = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task ResetAsync_RecreatesEmptyRelationalSchema()
    {
        await using var database = await RelationalTestDatabase.CreateAsync();
        database.Context.Devices.Add(new DeviceEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Platform = Platform.Android,
            DeviceKey = "device-key",
            DeviceName = "Phone",
            TimezoneId = "Asia/Seoul",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastSeenAtUtc = DateTimeOffset.UtcNow
        });
        await database.Context.SaveChangesAsync();

        await database.ResetAsync();

        Assert.Empty(await database.Context.Devices.ToListAsync());
    }

    [Fact]
    public async Task DeviceStateSessionUniqueIndex_IsEnforcedByRelationalProvider()
    {
        await using var database = await RelationalTestDatabase.CreateAsync();
        Guid deviceId = Guid.NewGuid();
        database.Context.Devices.Add(CreateDevice(deviceId));
        await database.Context.SaveChangesAsync();
        database.Context.DeviceStateSessions.Add(CreateDeviceStateSession(deviceId, "state-session-1"));
        await database.Context.SaveChangesAsync();

        database.Context.DeviceStateSessions.Add(CreateDeviceStateSession(deviceId, "state-session-1"));

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task AppFamilyMappingUniqueIndex_IsEnforcedByRelationalProvider()
    {
        await using var database = await RelationalTestDatabase.CreateAsync();
        var family = new AppFamilyEntity
        {
            Key = "chrome",
            DisplayName = "Chrome",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        database.Context.AppFamilies.Add(family);
        await database.Context.SaveChangesAsync();
        database.Context.AppFamilyMappings.Add(CreateAppFamilyMapping(family.Id, "platform_app", "chrome.exe"));
        await database.Context.SaveChangesAsync();

        database.Context.AppFamilyMappings.Add(CreateAppFamilyMapping(family.Id, "platform_app", "chrome.exe"));

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task WebSessionClientSessionUniqueIndex_IsEnforcedForDomainOnlySessions()
    {
        await using var database = await RelationalTestDatabase.CreateAsync();
        Guid deviceId = Guid.NewGuid();
        database.Context.Devices.Add(CreateDevice(deviceId));
        database.Context.FocusSessions.Add(CreateFocusSession(deviceId, "focus-session-1"));
        await database.Context.SaveChangesAsync();
        database.Context.WebSessions.Add(CreateWebSession(deviceId, "web-session-1"));
        await database.Context.SaveChangesAsync();

        database.Context.WebSessions.Add(CreateWebSession(deviceId, "web-session-1"));

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SessionForeignKeys_AreEnforcedByRelationalProvider()
    {
        await using var database = await RelationalTestDatabase.CreateAsync();
        Guid missingDeviceId = Guid.NewGuid();
        database.Context.FocusSessions.Add(CreateFocusSession(missingDeviceId, "focus-without-device"));

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());

        await database.ResetAsync();
        Guid deviceId = Guid.NewGuid();
        database.Context.Devices.Add(CreateDevice(deviceId));
        await database.Context.SaveChangesAsync();
        database.Context.WebSessions.Add(CreateWebSession(deviceId, "web-without-focus"));

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    private static DeviceEntity CreateDevice(Guid deviceId)
        => new()
        {
            Id = deviceId,
            UserId = "user-1",
            Platform = Platform.Windows,
            DeviceKey = $"device-{deviceId:N}",
            DeviceName = "Workstation",
            TimezoneId = "Asia/Seoul",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastSeenAtUtc = DateTimeOffset.UtcNow
        };

    private static FocusSessionEntity CreateFocusSession(Guid deviceId, string clientSessionId)
        => new()
        {
            DeviceId = deviceId,
            ClientSessionId = clientSessionId,
            PlatformAppKey = "chrome.exe",
            StartedAtUtc = new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            EndedAtUtc = new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
            DurationMs = 600_000,
            LocalDate = new DateOnly(2026, 4, 28),
            TimezoneId = "Asia/Seoul",
            IsIdle = false,
            Source = "foreground_window"
        };

    private static DeviceStateSessionEntity CreateDeviceStateSession(Guid deviceId, string clientSessionId)
        => new()
        {
            DeviceId = deviceId,
            ClientSessionId = clientSessionId,
            StateType = "idle",
            StartedAtUtc = new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            EndedAtUtc = new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
            DurationMs = 600_000,
            LocalDate = new DateOnly(2026, 4, 28),
            TimezoneId = "Asia/Seoul",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

    private static WebSessionEntity CreateWebSession(Guid deviceId, string clientSessionId)
        => new()
        {
            DeviceId = deviceId,
            ClientSessionId = clientSessionId,
            FocusSessionId = "focus-session-1",
            BrowserFamily = "Chrome",
            Url = null,
            Domain = "github.com",
            PageTitle = null,
            StartedAtUtc = new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            EndedAtUtc = new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
            DurationMs = 600_000,
            CaptureMethod = "BrowserExtensionFuture",
            CaptureConfidence = "High",
            IsPrivateOrUnknown = false
        };

    private static AppFamilyMappingEntity CreateAppFamilyMapping(long appFamilyId, string mappingType, string matchKey)
        => new()
        {
            AppFamilyId = appFamilyId,
            MappingType = mappingType,
            MatchKey = matchKey,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
}
