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

        List<DeviceEntity> devices = await _dbContext.Devices
            .Where(device => device.UserId == userId)
            .OrderBy(device => device.Platform)
            .ThenBy(device => device.DeviceName)
            .ToListAsync();
        List<Guid> deviceIds = devices.Select(device => device.Id).ToList();

        List<FocusSessionEntity> focusSessions = await _dbContext.FocusSessions
            .Where(session =>
                deviceIds.Contains(session.DeviceId) &&
                session.LocalDate >= fromDate &&
                session.LocalDate <= toDate)
            .ToListAsync();
        List<WebSessionEntity> webSessions = (await _dbContext.WebSessions
            .Where(session => deviceIds.Contains(session.DeviceId))
            .ToListAsync())
            .Where(session =>
            {
                DateOnly sessionDate = DateOnly.FromDateTime(session.StartedAtUtc.UtcDateTime);

                return sessionDate >= fromDate && sessionDate <= toDate;
            })
            .ToList();
        List<LocationContextEntity> locationContexts = await _dbContext.LocationContexts
            .Where(context =>
                deviceIds.Contains(context.DeviceId) &&
                context.LocalDate >= fromDate &&
                context.LocalDate <= toDate &&
                context.Latitude.HasValue &&
                context.Longitude.HasValue)
            .ToListAsync();

        Dictionary<Guid, Platform> platformByDeviceId = devices.ToDictionary(
            device => device.Id,
            device => device.Platform);

        List<IntegratedDeviceSummary> deviceSummaries = devices
            .Select(device => new IntegratedDeviceSummary(
                device.Id,
                ToPlatformKey(device.Platform),
                device.DeviceName,
                device.TimezoneId,
                focusSessions.Where(session => session.DeviceId == device.Id && !session.IsIdle).Sum(session => session.DurationMs),
                focusSessions.Where(session => session.DeviceId == device.Id && session.IsIdle).Sum(session => session.DurationMs),
                webSessions.Where(session => session.DeviceId == device.Id).Sum(session => session.DurationMs)))
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
                group.Where(item => !item!.Session.IsIdle).Sum(item => item!.Session.DurationMs),
                group.Where(item => item!.Session.IsIdle).Sum(item => item!.Session.DurationMs),
                webSessions
                    .Where(session => platformByDeviceId.TryGetValue(session.DeviceId, out Platform platform) &&
                                      ToPlatformKey(platform) == group.Key)
                    .Sum(session => session.DurationMs)))
            .ToList();

        List<IntegratedUsageTotal> topApps = focusSessions
            .Where(session => !session.IsIdle)
            .GroupBy(session => AppFamilyMapper.GetFamilyLabel(session.PlatformAppKey))
            .Select(group => new IntegratedUsageTotal(group.Key, group.Sum(session => session.DurationMs)))
            .OrderByDescending(total => total.DurationMs)
            .ThenBy(total => total.Label, StringComparer.Ordinal)
            .ToList();
        List<IntegratedUsageTotal> topDomains = webSessions
            .GroupBy(session => session.Domain)
            .Select(group => new IntegratedUsageTotal(group.Key, group.Sum(session => session.DurationMs)))
            .OrderByDescending(total => total.DurationMs)
            .ThenBy(total => total.Label, StringComparer.Ordinal)
            .ToList();
        List<IntegratedLocationTotal> topLocations = locationContexts
            .GroupBy(context => FormatLocationCell(context.Latitude!.Value, context.Longitude!.Value))
            .Select(group => new IntegratedLocationTotal(group.Key, group.Count()))
            .OrderByDescending(total => total.SampleCount)
            .ThenBy(total => total.Label, StringComparer.Ordinal)
            .ToList();

        return new IntegratedDashboardSnapshot(
            userId,
            fromDate,
            toDate,
            timezoneId,
            focusSessions.Where(session => !session.IsIdle).Sum(session => session.DurationMs),
            focusSessions.Where(session => session.IsIdle).Sum(session => session.DurationMs),
            webSessions.Sum(session => session.DurationMs),
            deviceSummaries,
            platformTotals,
            topApps,
            topDomains,
            topLocations);
    }

    private static string FormatLocationCell(double latitude, double longitude)
        => $"{Math.Round(latitude, 4):F4},{Math.Round(longitude, 4):F4}";

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
    IReadOnlyList<IntegratedLocationTotal> TopLocations);

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
