using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Domain.Common;
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
            .Where(session => deviceIds.Contains(session.DeviceId) && session.LocalDate == summaryDate)
            .ToListAsync();
        List<WebSessionEntity> webSessions = await _dbContext.WebSessions
            .Where(session => deviceIds.Contains(session.DeviceId))
            .ToListAsync();

        List<WebSessionEntity> webSessionsForDate = webSessions
            .Where(session => LocalDateCalculator.GetLocalDate(session.StartedAtUtc, timezoneId) == summaryDate)
            .ToList();

        List<UsageTotal> topApps = focusSessions
            .Where(session => !session.IsIdle)
            .GroupBy(session => session.PlatformAppKey)
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
            focusSessions.Where(session => !session.IsIdle).Sum(session => session.DurationMs),
            focusSessions.Where(session => session.IsIdle).Sum(session => session.DurationMs),
            webSessionsForDate.Sum(session => session.DurationMs),
            topApps,
            topDomains);
    }
}
