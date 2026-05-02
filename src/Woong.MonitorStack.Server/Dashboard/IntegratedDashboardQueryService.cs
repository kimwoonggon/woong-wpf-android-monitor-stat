using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Server.Data;
using Woong.MonitorStack.Server.Summaries;

namespace Woong.MonitorStack.Server.Dashboard;

public sealed class IntegratedDashboardQueryService
{
    private readonly MonitorDbContext _dbContext;

    public IntegratedDashboardQueryService(MonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IntegratedDashboardSnapshot> GetAsync(
        string userId,
        DateOnly fromDate,
        DateOnly toDate,
        string timezoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(timezoneId);

        if (toDate < fromDate)
        {
            throw new ArgumentException("End date must be on or after start date.", nameof(toDate));
        }

        TimeZoneInfo timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        (DateTimeOffset rangeStartUtc, DateTimeOffset rangeEndUtc) = ToUtcRange(fromDate, toDate, timezone);

        List<DeviceEntity> devices = await _dbContext.Devices
            .Where(device => device.UserId == userId)
            .OrderBy(device => device.Platform)
            .ThenBy(device => device.DeviceName)
            .ToListAsync();
        List<Guid> deviceIds = devices.Select(device => device.Id).ToList();

        List<FocusSessionEntity> focusSessions = (await _dbContext.FocusSessions
            .Where(session => deviceIds.Contains(session.DeviceId))
            .ToListAsync())
            .Where(session =>
                session.StartedAtUtc < rangeEndUtc &&
                session.EndedAtUtc > rangeStartUtc)
            .ToList();
        List<WebSessionEntity> webSessions = (await _dbContext.WebSessions
            .Where(session => deviceIds.Contains(session.DeviceId))
            .ToListAsync())
            .Where(session =>
                session.StartedAtUtc < rangeEndUtc &&
                session.EndedAtUtc > rangeStartUtc)
            .ToList();
        List<LocationContextEntity> locationContexts = (await _dbContext.LocationContexts
            .Where(context => deviceIds.Contains(context.DeviceId))
            .ToListAsync())
            .Where(context =>
                context.CapturedAtUtc >= rangeStartUtc &&
                context.CapturedAtUtc < rangeEndUtc &&
                context.Latitude.HasValue &&
                context.Longitude.HasValue)
            .ToList();

        Dictionary<Guid, Platform> platformByDeviceId = devices.ToDictionary(
            device => device.Id,
            device => device.Platform);

        List<IntegratedDeviceSummary> deviceSummaries = devices
            .Select(device => new IntegratedDeviceSummary(
                device.Id,
                ToPlatformKey(device.Platform),
                device.DeviceName,
                device.TimezoneId,
                focusSessions.Where(session => session.DeviceId == device.Id && !session.IsIdle).Sum(FocusDuration),
                focusSessions.Where(session => session.DeviceId == device.Id && session.IsIdle).Sum(FocusDuration),
                webSessions.Where(session => session.DeviceId == device.Id).Sum(WebDuration)))
            .ToList();

        List<IntegratedPlatformTotal> platformTotals = focusSessions
            .Select(session => platformByDeviceId.TryGetValue(session.DeviceId, out Platform platform)
                ? new { Platform = ToPlatformKey(platform), Session = session }
                : null)
            .Where(item => item is not null)
            .GroupBy(item => item!.Platform)
            .OrderBy(group => PlatformSortOrder(group.Key))
            .Select(group => new IntegratedPlatformTotal(
                group.Key,
                group.Where(item => !item!.Session.IsIdle).Sum(item => FocusDuration(item!.Session)),
                group.Where(item => item!.Session.IsIdle).Sum(item => FocusDuration(item!.Session)),
                webSessions
                    .Where(session => platformByDeviceId.TryGetValue(session.DeviceId, out Platform platform) &&
                                      ToPlatformKey(platform) == group.Key)
                    .Sum(WebDuration)))
            .ToList();

        List<IntegratedUsageTotal> topApps = focusSessions
            .Where(session => !session.IsIdle)
            .GroupBy(session => AppFamilyMapper.GetFamilyLabel(session.PlatformAppKey))
            .Select(group => new IntegratedUsageTotal(group.Key, group.Sum(FocusDuration)))
            .OrderByDescending(total => total.DurationMs)
            .ThenBy(total => total.Label, StringComparer.Ordinal)
            .ToList();
        List<IntegratedUsageTotal> topDomains = webSessions
            .GroupBy(session => session.Domain)
            .Select(group => new IntegratedUsageTotal(group.Key, group.Sum(WebDuration)))
            .OrderByDescending(total => total.DurationMs)
            .ThenBy(total => total.Label, StringComparer.Ordinal)
            .ToList();
        List<IntegratedLocationTotal> topLocations = locationContexts
            .GroupBy(context => FormatLocationCell(context.Latitude!.Value, context.Longitude!.Value))
            .Select(group => new IntegratedLocationTotal(group.Key, group.Count()))
            .OrderByDescending(total => total.SampleCount)
            .ThenBy(total => total.Label, StringComparer.Ordinal)
            .ToList();
        List<IntegratedPlatformUsage> platformUsage = platformTotals
            .Select(total => new IntegratedPlatformUsage(
                total.Platform,
                total.ActiveMs,
                total.IdleMs,
                total.WebMs,
                TopAppsForPlatform(total.Platform),
                TopDomainsForPlatform(total.Platform)))
            .ToList();
        List<IntegratedCurrentApp> currentApps = devices
            .Select(device => new
            {
                Device = device,
                Session = focusSessions
                    .Where(session => session.DeviceId == device.Id)
                    .OrderByDescending(session => session.EndedAtUtc)
                    .ThenByDescending(session => session.StartedAtUtc)
                    .FirstOrDefault()
            })
            .Where(item => item.Session is not null)
            .GroupBy(item => ToPlatformKey(item.Device.Platform))
            .OrderBy(group => PlatformSortOrder(group.Key))
            .Select(group =>
            {
                var latest = group
                    .OrderByDescending(item => item.Session!.EndedAtUtc)
                    .ThenByDescending(item => item.Session!.StartedAtUtc)
                    .First();
                FocusSessionEntity session = latest.Session!;

                return new IntegratedCurrentApp(
                    ToPlatformKey(latest.Device.Platform),
                    latest.Device.DeviceName,
                    AppFamilyMapper.GetFamilyLabel(session.PlatformAppKey),
                    session.PlatformAppKey,
                    session.StartedAtUtc,
                    session.EndedAtUtc,
                    OverlapDurationMs(session.StartedAtUtc, session.EndedAtUtc, rangeStartUtc, rangeEndUtc),
                    session.IsIdle);
            })
            .ToList();
        List<IntegratedLocationRoutePoint> locationRoute = locationContexts
            .OrderBy(context => context.CapturedAtUtc)
            .ThenBy(context => context.ClientContextId, StringComparer.Ordinal)
            .Select(context => new IntegratedLocationRoutePoint(
                context.ClientContextId,
                context.CapturedAtUtc,
                context.Latitude!.Value,
                context.Longitude!.Value,
                context.AccuracyMeters,
                context.CaptureMode,
                context.PermissionState))
            .ToList();

        return new IntegratedDashboardSnapshot(
            userId,
            fromDate,
            toDate,
            timezoneId,
            focusSessions.Where(session => !session.IsIdle).Sum(FocusDuration),
            focusSessions.Where(session => session.IsIdle).Sum(FocusDuration),
            webSessions.Sum(WebDuration),
            deviceSummaries,
            platformTotals,
            topApps,
            topDomains,
            topLocations,
            platformUsage,
            currentApps,
            locationRoute);

        long FocusDuration(FocusSessionEntity session)
            => OverlapDurationMs(session.StartedAtUtc, session.EndedAtUtc, rangeStartUtc, rangeEndUtc);

        long WebDuration(WebSessionEntity session)
            => OverlapDurationMs(session.StartedAtUtc, session.EndedAtUtc, rangeStartUtc, rangeEndUtc);

        List<IntegratedUsageTotal> TopAppsForPlatform(string platformKey)
            => focusSessions
                .Where(session => !session.IsIdle &&
                                  platformByDeviceId.TryGetValue(session.DeviceId, out Platform platform) &&
                                  ToPlatformKey(platform) == platformKey)
                .GroupBy(session => AppFamilyMapper.GetFamilyLabel(session.PlatformAppKey))
                .Select(group => new IntegratedUsageTotal(group.Key, group.Sum(FocusDuration)))
                .OrderByDescending(total => total.DurationMs)
                .ThenBy(total => total.Label, StringComparer.Ordinal)
                .ToList();

        List<IntegratedUsageTotal> TopDomainsForPlatform(string platformKey)
            => webSessions
                .Where(session => platformByDeviceId.TryGetValue(session.DeviceId, out Platform platform) &&
                                  ToPlatformKey(platform) == platformKey)
                .GroupBy(session => session.Domain)
                .Select(group => new IntegratedUsageTotal(group.Key, group.Sum(WebDuration)))
                .OrderByDescending(total => total.DurationMs)
                .ThenBy(total => total.Label, StringComparer.Ordinal)
                .ToList();
    }

    private static string FormatLocationCell(double latitude, double longitude)
        => $"{Math.Round(latitude, 4):F4},{Math.Round(longitude, 4):F4}";

    private static (DateTimeOffset StartUtc, DateTimeOffset EndUtc) ToUtcRange(
        DateOnly fromDate,
        DateOnly toDate,
        TimeZoneInfo timezone)
    {
        DateTime localStart = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        DateTime localEnd = toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        return (
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, timezone), TimeSpan.Zero),
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, timezone), TimeSpan.Zero));
    }

    private static long OverlapDurationMs(
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc,
        DateTimeOffset rangeStartUtc,
        DateTimeOffset rangeEndUtc)
    {
        DateTimeOffset overlapStart = startedAtUtc > rangeStartUtc ? startedAtUtc : rangeStartUtc;
        DateTimeOffset overlapEnd = endedAtUtc < rangeEndUtc ? endedAtUtc : rangeEndUtc;

        if (overlapEnd <= overlapStart)
        {
            return 0;
        }

        return (long)(overlapEnd - overlapStart).TotalMilliseconds;
    }

    private static string ToPlatformKey(Platform platform)
        => platform.ToString().ToLowerInvariant();

    private static int PlatformSortOrder(string platform)
        => platform switch
        {
            "windows" => 0,
            "android" => 1,
            _ => 2
        };
}

public sealed record IntegratedDashboardSnapshot(
    string UserId,
    DateOnly FromDate,
    DateOnly ToDate,
    string TimezoneId,
    long TotalActiveMs,
    long TotalIdleMs,
    long TotalWebMs,
    IReadOnlyList<IntegratedDeviceSummary> Devices,
    IReadOnlyList<IntegratedPlatformTotal> PlatformTotals,
    IReadOnlyList<IntegratedUsageTotal> TopApps,
    IReadOnlyList<IntegratedUsageTotal> TopDomains,
    IReadOnlyList<IntegratedLocationTotal> TopLocations,
    IReadOnlyList<IntegratedPlatformUsage> PlatformUsage,
    IReadOnlyList<IntegratedCurrentApp> CurrentApps,
    IReadOnlyList<IntegratedLocationRoutePoint> LocationRoute);

public sealed record IntegratedDeviceSummary(
    Guid DeviceId,
    string Platform,
    string DeviceName,
    string TimezoneId,
    long ActiveMs,
    long IdleMs,
    long WebMs);

public sealed record IntegratedPlatformTotal(
    string Platform,
    long ActiveMs,
    long IdleMs,
    long WebMs);

public sealed record IntegratedUsageTotal(string Label, long DurationMs);

public sealed record IntegratedLocationTotal(string Label, int SampleCount);

public sealed record IntegratedPlatformUsage(
    string Platform,
    long ActiveMs,
    long IdleMs,
    long WebMs,
    IReadOnlyList<IntegratedUsageTotal> TopApps,
    IReadOnlyList<IntegratedUsageTotal> TopDomains);

public sealed record IntegratedCurrentApp(
    string Platform,
    string DeviceName,
    string AppLabel,
    string PlatformAppKey,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    long DurationMs,
    bool IsIdle);

public sealed record IntegratedLocationRoutePoint(
    string ClientContextId,
    DateTimeOffset CapturedAtUtc,
    double Latitude,
    double Longitude,
    double? AccuracyMeters,
    string CaptureMode,
    string PermissionState);
