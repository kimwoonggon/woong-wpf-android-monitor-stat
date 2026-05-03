using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public static class DashboardSummaryBuilder
{
    public static DashboardSummarySnapshot Build(
        IEnumerable<FocusSession> focusSessions,
        IEnumerable<WebSession> webSessions,
        TimeRange range,
        DashboardPeriod period,
        string timeZoneId)
    {
        IReadOnlyList<FocusSession> focusSessionList = DashboardRangeClipper.ClipFocusSessions(focusSessions, range);
        IReadOnlyList<WebSession> webSessionList = DashboardRangeClipper.ClipWebSessions(webSessions, range);
        DailySummary summary = BuildDailySummaryFromClipped(focusSessionList, webSessionList, range, timeZoneId);
        long totalForegroundMs = focusSessionList.Sum(session => session.DurationMs);
        string periodDescriptor = FormatPeriodDescriptor(period);
        IReadOnlyList<DashboardSummaryCard> summaryCards =
        [
            new("Active Focus", FormatDuration(summary.TotalActiveMs), $"{periodDescriptor} focused foreground time"),
            new("Foreground", FormatDuration(totalForegroundMs), $"{periodDescriptor} foreground time"),
            new("Idle", FormatDuration(summary.TotalIdleMs), $"{periodDescriptor} idle foreground time"),
            new("Web Focus", FormatDuration(summary.TotalWebMs), $"{periodDescriptor} browser domain time")
        ];

        return new DashboardSummarySnapshot(
            summary,
            totalForegroundMs,
            summaryCards,
            summary.TopApps.FirstOrDefault()?.Key ?? "",
            summary.TopDomains.FirstOrDefault()?.Key ?? "");
    }

    public static DailySummary BuildDailySummary(
        IEnumerable<FocusSession> focusSessions,
        IEnumerable<WebSession> webSessions,
        TimeRange range,
        string timeZoneId)
    {
        IReadOnlyList<FocusSession> focusSessionList = DashboardRangeClipper.ClipFocusSessions(focusSessions, range);
        IReadOnlyList<WebSession> webSessionList = DashboardRangeClipper.ClipWebSessions(webSessions, range);

        return BuildDailySummaryFromClipped(focusSessionList, webSessionList, range, timeZoneId);
    }

    private static DailySummary BuildDailySummaryFromClipped(
        IEnumerable<FocusSession> focusSessions,
        IEnumerable<WebSession> webSessions,
        TimeRange range,
        string timeZoneId)
    {
        List<FocusSession> focusSessionList = focusSessions.ToList();
        List<WebSession> webSessionList = webSessions.ToList();
        DateOnly summaryDate = LocalDateCalculator.GetLocalDate(range.StartedAtUtc, timeZoneId);
        long totalActiveMs = focusSessionList
            .Where(session => !session.IsIdle)
            .Sum(session => session.DurationMs);
        long totalIdleMs = focusSessionList
            .Where(session => session.IsIdle)
            .Sum(session => session.DurationMs);
        IReadOnlyList<UsageTotal> topApps = DashboardUsageLabelGrouper.GroupTotals(
            focusSessionList
                .Where(session => !session.IsIdle)
                .Select(session => new UsageTotal(session.PlatformAppKey, session.DurationMs)));
        long totalWebMs = webSessionList.Sum(session => session.DurationMs);
        IReadOnlyList<UsageTotal> topDomains = DashboardUsageLabelGrouper.GroupTotals(
            webSessionList
                .Where(session => !string.IsNullOrWhiteSpace(session.Domain))
                .Select(session => new UsageTotal(session.Domain, session.DurationMs)));

        return new DailySummary(summaryDate, totalActiveMs, totalIdleMs, totalWebMs, topApps, topDomains);
    }

    private static string FormatPeriodDescriptor(DashboardPeriod period)
        => period switch
        {
            DashboardPeriod.Today => "Today's",
            DashboardPeriod.LastHour => "Last 1h",
            DashboardPeriod.Last6Hours => "Last 6h",
            DashboardPeriod.Last24Hours => "Last 24h",
            DashboardPeriod.Custom => "Custom range",
            _ => "Selected range"
        };

    private static string FormatDuration(long durationMs)
    {
        if (durationMs <= 0)
        {
            return "0m";
        }

        long totalMinutes = durationMs / 60_000;
        long hours = totalMinutes / 60;
        long minutes = totalMinutes % 60;

        return hours > 0
            ? $"{hours}h {minutes:D2}m"
            : $"{Math.Max(1, minutes)}m";
    }
}

public sealed record DashboardSummarySnapshot(
    DailySummary Summary,
    long TotalForegroundMs,
    IReadOnlyList<DashboardSummaryCard> SummaryCards,
    string TopAppName,
    string TopDomainName)
{
    public long TotalActiveMs => Summary.TotalActiveMs;

    public long TotalIdleMs => Summary.TotalIdleMs;

    public long TotalWebMs => Summary.TotalWebMs;
}
