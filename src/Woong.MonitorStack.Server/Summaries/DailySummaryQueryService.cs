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

        List<FocusDailySegment> focusSegmentsForDate = focusSessions
            .SelectMany(session => SplitFocusSessionByLocalDate(session, timezoneId))
            .Where(segment => segment.LocalDate == summaryDate)
            .ToList();
        List<WebDailySegment> webSegmentsForDate = webSessions
            .SelectMany(session => SplitWebSessionByLocalDate(session, timezoneId))
            .Where(segment => segment.LocalDate == summaryDate)
            .ToList();

        List<UsageTotal> topApps = focusSegmentsForDate
            .Where(segment => !segment.IsIdle)
            .GroupBy(segment => AppFamilyMapper.GetFamilyLabel(segment.PlatformAppKey))
            .Select(group => new UsageTotal(group.Key, group.Sum(segment => segment.DurationMs)))
            .OrderByDescending(total => total.DurationMs)
            .ThenBy(total => total.Key, StringComparer.Ordinal)
            .ToList();
        List<UsageTotal> topDomains = webSegmentsForDate
            .GroupBy(segment => segment.Domain)
            .Select(group => new UsageTotal(group.Key, group.Sum(segment => segment.DurationMs)))
            .OrderByDescending(total => total.DurationMs)
            .ThenBy(total => total.Key, StringComparer.Ordinal)
            .ToList();

        return new DailySummary(
            summaryDate,
            focusSegmentsForDate.Where(segment => !segment.IsIdle).Sum(segment => segment.DurationMs),
            focusSegmentsForDate.Where(segment => segment.IsIdle).Sum(segment => segment.DurationMs),
            webSegmentsForDate.Sum(segment => segment.DurationMs),
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

    private static IEnumerable<FocusDailySegment> SplitFocusSessionByLocalDate(
        FocusSessionEntity session,
        string timezoneId)
        => SplitByLocalDate(session.StartedAtUtc, session.DurationMs, timezoneId)
            .Select(segment => new FocusDailySegment(
                segment.LocalDate,
                session.PlatformAppKey,
                session.IsIdle,
                segment.DurationMs));

    private static IEnumerable<WebDailySegment> SplitWebSessionByLocalDate(
        WebSessionEntity session,
        string timezoneId)
        => SplitByLocalDate(session.StartedAtUtc, session.DurationMs, timezoneId)
            .Select(segment => new WebDailySegment(
                segment.LocalDate,
                session.Domain,
                segment.DurationMs));

    private static IEnumerable<DailyDurationSegment> SplitByLocalDate(
        DateTimeOffset startedAtUtc,
        long durationMs,
        string timezoneId)
    {
        TimeZoneInfo timeZone = ResolveTimeZone(timezoneId);
        DateTimeOffset cursorUtc = startedAtUtc.ToUniversalTime();
        DateTimeOffset endUtc = cursorUtc.AddMilliseconds(durationMs);

        while (cursorUtc < endUtc)
        {
            DateTimeOffset localCursor = TimeZoneInfo.ConvertTime(cursorUtc, timeZone);
            DateOnly localDate = DateOnly.FromDateTime(localCursor.DateTime);
            DateTime nextLocalMidnight = localCursor.Date.AddDays(1);
            DateTimeOffset nextLocalMidnightUtc = new(
                TimeZoneInfo.ConvertTimeToUtc(nextLocalMidnight, timeZone),
                TimeSpan.Zero);
            DateTimeOffset segmentEndUtc = nextLocalMidnightUtc < endUtc
                ? nextLocalMidnightUtc
                : endUtc;

            yield return new DailyDurationSegment(
                localDate,
                (long)(segmentEndUtc - cursorUtc).TotalMilliseconds);

            cursorUtc = segmentEndUtc;
        }
    }

    private static TimeZoneInfo ResolveTimeZone(string timezoneId)
    {
        if (string.Equals(timezoneId, "UTC", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(timezoneId, "Etc/UTC", StringComparison.OrdinalIgnoreCase))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch (TimeZoneNotFoundException) when (WindowsFallbacks.TryGetValue(timezoneId, out string? windowsId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
        }
        catch (InvalidTimeZoneException) when (WindowsFallbacks.TryGetValue(timezoneId, out string? windowsId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
        }
    }

    private sealed record DailyDurationSegment(DateOnly LocalDate, long DurationMs);

    private sealed record FocusDailySegment(
        DateOnly LocalDate,
        string PlatformAppKey,
        bool IsIdle,
        long DurationMs);

    private sealed record WebDailySegment(
        DateOnly LocalDate,
        string Domain,
        long DurationMs);

    private static readonly IReadOnlyDictionary<string, string> WindowsFallbacks =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Asia/Seoul"] = "Korea Standard Time"
        };
}
