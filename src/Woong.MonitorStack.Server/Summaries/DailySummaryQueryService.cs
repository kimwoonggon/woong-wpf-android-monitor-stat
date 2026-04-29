using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Summaries;

public sealed class DailySummaryQueryService
{
    private readonly MonitorDbContext _dbContext;

    public DailySummaryQueryService(MonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DailySummary> GetAsync(string userId, DateOnly summaryDate, string timezoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(timezoneId);

        List<Guid> deviceIds = await _dbContext.Devices
            .Where(device => device.UserId == userId)
            .Select(device => device.Id)
            .ToListAsync();

        List<FocusSessionEntity> focusSessions = await _dbContext.FocusSessions
            .Where(session => deviceIds.Contains(session.DeviceId))
            .ToListAsync();
        List<WebSessionEntity> webSessions = await _dbContext.WebSessions
            .Where(session => deviceIds.Contains(session.DeviceId))
            .ToListAsync();

        List<FocusSessionEntity> focusSessionsForDate = focusSessions
            .Where(session => LocalDateCalculator.GetLocalDate(session.StartedAtUtc, timezoneId) == summaryDate)
            .ToList();
        List<WebSessionEntity> webSessionsForDate = webSessions
            .Where(session => LocalDateCalculator.GetLocalDate(session.StartedAtUtc, timezoneId) == summaryDate)
            .ToList();

        List<UsageTotal> topApps = focusSessionsForDate
            .Where(session => !session.IsIdle)
            .GroupBy(session => AppFamilyMapper.GetFamilyLabel(session.PlatformAppKey))
            .Select(group => new UsageTotal(group.Key, group.Sum(session => session.DurationMs)))
            .OrderByDescending(total => total.DurationMs)
            .ThenBy(total => total.Key, StringComparer.Ordinal)
            .ToList();
        List<UsageTotal> topDomains = webSessionsForDate
            .GroupBy(session => session.Domain)
            .Select(group => new UsageTotal(group.Key, group.Sum(session => session.DurationMs)))
            .OrderByDescending(total => total.DurationMs)
            .ThenBy(total => total.Key, StringComparer.Ordinal)
            .ToList();

        return new DailySummary(
            summaryDate,
            focusSessionsForDate.Where(session => !session.IsIdle).Sum(session => session.DurationMs),
            focusSessionsForDate.Where(session => session.IsIdle).Sum(session => session.DurationMs),
            webSessionsForDate.Sum(session => session.DurationMs),
            topApps,
            topDomains);
    }

    public async Task<DateRangeStatisticsResponse> GetRangeAsync(
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

        List<Guid> deviceIds = await _dbContext.Devices
            .Where(device => device.UserId == userId)
            .Select(device => device.Id)
            .ToListAsync();

        List<FocusSessionEntity> focusSessions = await _dbContext.FocusSessions
            .Where(session => deviceIds.Contains(session.DeviceId))
            .ToListAsync();
        List<WebSessionEntity> webSessions = await _dbContext.WebSessions
            .Where(session => deviceIds.Contains(session.DeviceId))
            .ToListAsync();

        List<FocusSessionEntity> focusSessionsForRange = focusSessions
            .Where(session =>
            {
                DateOnly localDate = LocalDateCalculator.GetLocalDate(session.StartedAtUtc, timezoneId);

                return localDate >= fromDate && localDate <= toDate;
            })
            .ToList();
        List<WebSessionEntity> webSessionsForRange = webSessions
            .Where(session =>
            {
                DateOnly localDate = LocalDateCalculator.GetLocalDate(session.StartedAtUtc, timezoneId);

                return localDate >= fromDate && localDate <= toDate;
            })
            .ToList();

        List<UsageTotal> topApps = focusSessionsForRange
            .Where(session => !session.IsIdle)
            .GroupBy(session => AppFamilyMapper.GetFamilyLabel(session.PlatformAppKey))
            .Select(group => new UsageTotal(group.Key, group.Sum(session => session.DurationMs)))
            .OrderByDescending(total => total.DurationMs)
            .ThenBy(total => total.Key, StringComparer.Ordinal)
            .ToList();
        List<UsageTotal> topDomains = webSessionsForRange
            .GroupBy(session => session.Domain)
            .Select(group => new UsageTotal(group.Key, group.Sum(session => session.DurationMs)))
            .OrderByDescending(total => total.DurationMs)
            .ThenBy(total => total.Key, StringComparer.Ordinal)
            .ToList();

        return new DateRangeStatisticsResponse(
            fromDate,
            toDate,
            focusSessionsForRange.Where(session => !session.IsIdle).Sum(session => session.DurationMs),
            focusSessionsForRange.Where(session => session.IsIdle).Sum(session => session.DurationMs),
            webSessionsForRange.Sum(session => session.DurationMs),
            topApps,
            topDomains);
    }
}
