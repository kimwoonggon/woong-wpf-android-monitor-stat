using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Tests.Data;

public sealed class PostgresMonitorDbContextTests
{
    [PostgresFact]
    public async Task PostgresMigrations_ApplyLatestSchemaAndEnforceProviderConstraints()
    {
        await using var database = await PostgresTestDatabase.CreateAsync();
        IReadOnlyList<string> appliedMigrations = (await database.Context.Database.GetAppliedMigrationsAsync()).ToList();

        Assert.Contains(appliedMigrations, migration => migration.EndsWith("AddLocationContextTable", StringComparison.Ordinal));
        Assert.Contains("Npgsql", database.Context.Database.ProviderName, StringComparison.OrdinalIgnoreCase);

        Guid deviceId = Guid.NewGuid();
        database.Context.Devices.Add(CreateDevice(deviceId));
        database.Context.FocusSessions.Add(CreateFocusSession(deviceId, "focus-session-1"));
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        database.Context.FocusSessions.Add(CreateFocusSession(deviceId, "focus-session-1"));

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());

        database.Context.ChangeTracker.Clear();
        database.Context.WebSessions.Add(CreateWebSession(deviceId, "web-without-focus", "missing-focus-session"));

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [PostgresFact]
    public async Task PostgresMigration_BackfillsLegacyWebSessionClientSessionIdsBeforeUniqueIndex()
    {
        await using var database = await PostgresTestDatabase.CreateUnmigratedAsync();
        IMigrator migrator = database.Context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20260428170042_AddDeviceStateAndAppFamilyTables");

        Guid deviceId = Guid.NewGuid();
        string deviceIdText = deviceId.ToString();
        await database.Context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO devices ("Id", "UserId", "Platform", "DeviceKey", "DeviceName", "TimezoneId", "CreatedAtUtc", "LastSeenAtUtc")
            VALUES ({0}::uuid, 'user-1', 0, 'device-key', 'Workstation', 'Asia/Seoul', now(), now())
            """,
            deviceIdText);
        await database.Context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO focus_sessions ("DeviceId", "ClientSessionId", "PlatformAppKey", "StartedAtUtc", "EndedAtUtc", "DurationMs", "LocalDate", "TimezoneId", "IsIdle", "Source")
            VALUES ({0}::uuid, 'focus-session-1', 'chrome.exe', now() - interval '10 minutes', now(), 600000, DATE '2026-04-28', 'Asia/Seoul', false, 'foreground_window')
            """,
            deviceIdText);
        await database.Context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO web_sessions ("DeviceId", "FocusSessionId", "BrowserFamily", "Url", "Domain", "PageTitle", "StartedAtUtc", "EndedAtUtc", "DurationMs")
            VALUES ({0}::uuid, 'focus-session-1', 'Chrome', NULL, 'github.com', NULL, now() - interval '10 minutes', now(), 600000)
            """,
            deviceIdText);

        await migrator.MigrateAsync();

        string clientSessionId = await database.Context.WebSessions
            .Where(session => session.DeviceId == deviceId)
            .Select(session => session.ClientSessionId)
            .SingleAsync();

        Assert.StartsWith("legacy-web-session-", clientSessionId, StringComparison.Ordinal);
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

    private static WebSessionEntity CreateWebSession(Guid deviceId, string clientSessionId, string focusSessionId)
        => new()
        {
            DeviceId = deviceId,
            ClientSessionId = clientSessionId,
            FocusSessionId = focusSessionId,
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
}
