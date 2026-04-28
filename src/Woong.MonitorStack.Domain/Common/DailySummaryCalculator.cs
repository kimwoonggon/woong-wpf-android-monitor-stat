namespace Woong.MonitorStack.Domain.Common;

public static class DailySummaryCalculator
{
    public static DailySummary Calculate(IEnumerable<FocusSession> sessions, DateOnly summaryDate)
        => Calculate(sessions, [], summaryDate, timezoneId: "UTC");

    public static DailySummary Calculate(
        IEnumerable<FocusSession> sessions,
        IEnumerable<WebSession> webSessions,
        DateOnly summaryDate,
        string timezoneId)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(webSessions);

        var sessionsForDate = sessions
            .Where(session => session.LocalDate == summaryDate)
            .ToList();
        var webSessionsForDate = webSessions
            .Where(session => LocalDateCalculator.GetLocalDate(session.StartedAtUtc, timezoneId) == summaryDate)
            .ToList();

        var totalActiveMs = sessionsForDate
            .Where(session => !session.IsIdle)
            .Sum(session => session.DurationMs);
        var totalIdleMs = sessionsForDate
            .Where(session => session.IsIdle)
            .Sum(session => session.DurationMs);
        var topApps = sessionsForDate
            .Where(session => !session.IsIdle)
            .GroupBy(session => session.PlatformAppKey)
            .Select(group => new UsageTotal(group.Key, group.Sum(session => session.DurationMs)))
            .OrderByDescending(total => total.DurationMs)
            .ThenBy(total => total.Key, StringComparer.Ordinal)
            .ToList();
        var totalWebMs = webSessionsForDate.Sum(session => session.DurationMs);
        var topDomains = webSessionsForDate
            .GroupBy(session => session.Domain)
            .Select(group => new UsageTotal(group.Key, group.Sum(session => session.DurationMs)))
            .OrderByDescending(total => total.DurationMs)
            .ThenBy(total => total.Key, StringComparer.Ordinal)
            .ToList();

        return new DailySummary(
            summaryDate,
            totalActiveMs,
            totalIdleMs,
            totalWebMs,
            topApps,
            topDomains);
    }
}
