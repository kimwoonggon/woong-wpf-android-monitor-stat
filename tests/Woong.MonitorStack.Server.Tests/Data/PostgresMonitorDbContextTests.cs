using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Data;
using Woong.MonitorStack.Server.Events;
using Woong.MonitorStack.Server.Locations;
using Woong.MonitorStack.Server.Sessions;

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

    [PostgresFact]
    public async Task PostgresFocusUpload_WhenConcurrentDuplicateRequests_StoresOneSessionAndReturnsIdempotentStatuses()
    {
        await using var database = await PostgresTestDatabase.CreateAsync();
        Guid deviceId = Guid.NewGuid();
        database.Context.Devices.Add(CreateDevice(deviceId));
        await database.Context.SaveChangesAsync();
        var request = new UploadFocusSessionsRequest(
            deviceId.ToString("N"),
            [
                new FocusSessionUploadItem(
                    "focus-session-concurrent",
                    "chrome.exe",
                    new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
                    600_000,
                    new DateOnly(2026, 4, 28),
                    "Asia/Seoul",
                    isIdle: false,
                    source: "foreground_window",
                    processId: 10,
                    processName: "chrome.exe",
                    processPath: null,
                    windowHandle: 100,
                    windowTitle: null)
            ]);
        using var startGate = new ManualResetEventSlim(initialState: false);
        Task<UploadBatchResult>[] tasks = Enumerable
            .Range(0, 8)
            .Select(_ => Task.Run(async () =>
            {
                startGate.Wait(TimeSpan.FromSeconds(5));
                await using MonitorDbContext context = database.CreateContext();
                var service = new FocusSessionUploadService(context);

                return await service.UploadAsync(request);
            }))
            .ToArray();

        startGate.Set();
        UploadBatchResult[] results = await Task.WhenAll(tasks);

        await using MonitorDbContext verificationContext = database.CreateContext();
        Assert.Equal(1, await verificationContext.FocusSessions.CountAsync(session =>
            session.DeviceId == deviceId &&
            session.ClientSessionId == "focus-session-concurrent"));
        List<UploadItemStatus> statuses = results
            .SelectMany(result => result.Items)
            .Select(item => item.Status)
            .ToList();
        Assert.Contains(UploadItemStatus.Accepted, statuses);
        Assert.All(statuses, status => Assert.Contains(
            status,
            new[] { UploadItemStatus.Accepted, UploadItemStatus.Duplicate }));
    }

    [PostgresFact]
    public async Task PostgresWebUpload_WhenConcurrentDuplicateRequests_StoresOneSessionAndReturnsIdempotentStatuses()
    {
        await using var database = await PostgresTestDatabase.CreateAsync();
        Guid deviceId = Guid.NewGuid();
        database.Context.Devices.Add(CreateDevice(deviceId));
        database.Context.FocusSessions.Add(CreateFocusSession(deviceId, "focus-session-1"));
        await database.Context.SaveChangesAsync();
        var request = new UploadWebSessionsRequest(
            deviceId.ToString("N"),
            [
                new WebSessionUploadItem(
                    "web-session-concurrent",
                    "focus-session-1",
                    "Chrome",
                    url: null,
                    "github.com",
                    pageTitle: null,
                    new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
                    600_000,
                    "BrowserExtensionFuture",
                    "High",
                    false)
            ]);

        UploadBatchResult[] results = await RunConcurrentUploadsAsync(database, async context =>
            await new WebSessionUploadService(context).UploadAsync(request));

        await using MonitorDbContext verificationContext = database.CreateContext();
        Assert.Equal(1, await verificationContext.WebSessions.CountAsync(session =>
            session.DeviceId == deviceId &&
            session.ClientSessionId == "web-session-concurrent"));
        AssertIdempotentStatuses(results);
    }

    [PostgresFact]
    public async Task PostgresRawEventUpload_WhenConcurrentDuplicateRequests_StoresOneEventAndReturnsIdempotentStatuses()
    {
        await using var database = await PostgresTestDatabase.CreateAsync();
        Guid deviceId = Guid.NewGuid();
        database.Context.Devices.Add(CreateDevice(deviceId));
        await database.Context.SaveChangesAsync();
        var request = new UploadRawEventsRequest(
            deviceId.ToString("N"),
            [
                new RawEventUploadItem(
                    "raw-event-concurrent",
                    "foreground_window",
                    new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
                    """{"processName":"Code.exe"}""")
            ]);

        UploadBatchResult[] results = await RunConcurrentUploadsAsync(database, async context =>
            await new RawEventUploadService(context).UploadAsync(request));

        await using MonitorDbContext verificationContext = database.CreateContext();
        Assert.Equal(1, await verificationContext.RawEvents.CountAsync(rawEvent =>
            rawEvent.DeviceId == deviceId &&
            rawEvent.ClientEventId == "raw-event-concurrent"));
        AssertIdempotentStatuses(results);
    }

    [PostgresFact]
    public async Task PostgresLocationUpload_WhenConcurrentDuplicateRequests_StoresOneContextAndReturnsIdempotentStatuses()
    {
        await using var database = await PostgresTestDatabase.CreateAsync();
        Guid deviceId = Guid.NewGuid();
        database.Context.Devices.Add(CreateDevice(deviceId));
        await database.Context.SaveChangesAsync();
        var request = new UploadLocationContextsRequest(
            deviceId.ToString("N"),
            [
                new LocationContextUploadItem(
                    "location-context-concurrent",
                    new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
                    new DateOnly(2026, 4, 28),
                    "Asia/Seoul",
                    latitude: null,
                    longitude: null,
                    accuracyMeters: null,
                    "location_unavailable",
                    "denied",
                    "android_location_context")
            ]);

        UploadBatchResult[] results = await RunConcurrentUploadsAsync(database, async context =>
            await new LocationContextUploadService(context).UploadAsync(request));

        await using MonitorDbContext verificationContext = database.CreateContext();
        Assert.Equal(1, await verificationContext.LocationContexts.CountAsync(context =>
            context.DeviceId == deviceId &&
            context.ClientContextId == "location-context-concurrent"));
        AssertIdempotentStatuses(results);
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

    private static async Task<UploadBatchResult[]> RunConcurrentUploadsAsync(
        PostgresTestDatabase database,
        Func<MonitorDbContext, Task<UploadBatchResult>> upload)
    {
        using var startGate = new ManualResetEventSlim(initialState: false);
        Task<UploadBatchResult>[] tasks = Enumerable
            .Range(0, 8)
            .Select(_ => Task.Run(async () =>
            {
                startGate.Wait(TimeSpan.FromSeconds(5));
                await using MonitorDbContext context = database.CreateContext();

                return await upload(context);
            }))
            .ToArray();

        startGate.Set();

        return await Task.WhenAll(tasks);
    }

    private static void AssertIdempotentStatuses(IEnumerable<UploadBatchResult> results)
    {
        List<UploadItemStatus> statuses = results
            .SelectMany(result => result.Items)
            .Select(item => item.Status)
            .ToList();
        Assert.Contains(UploadItemStatus.Accepted, statuses);
        Assert.All(statuses, status => Assert.Contains(
            status,
            new[] { UploadItemStatus.Accepted, UploadItemStatus.Duplicate }));
    }
}
