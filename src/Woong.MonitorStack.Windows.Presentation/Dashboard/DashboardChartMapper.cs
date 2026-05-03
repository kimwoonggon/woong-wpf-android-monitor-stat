using System.Globalization;
using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public static class DashboardChartMapper
{
    public static IReadOnlyList<DashboardChartPoint> BuildHourlyActivityPoints(
        IEnumerable<FocusSession> sessions,
        string timezoneId)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);

        return TimeBucketAggregator.AggregateByHour(sessions)
            .Select(bucket => new DashboardChartPoint(
                FormatHourLabel(bucket.BucketStartUtc, timeZone),
                Convert.ToInt64(bucket.Duration.TotalMilliseconds)))
            .ToList();
    }

    public static IReadOnlyList<DashboardChartPoint> BuildHourlyActivityPoints(
        IEnumerable<FocusSession> sessions,
        TimeRange range,
        string timezoneId)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(range);

        return BuildHourlyActivityPoints(
            DashboardRangeClipper.ClipFocusSessions(sessions, range),
            timezoneId);
    }

    public static IReadOnlyList<DashboardChartPoint> BuildAppUsagePoints(DailySummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return BuildUsagePoints(summary.TopApps);
    }

    public static IReadOnlyList<DashboardChartPoint> BuildDomainUsagePoints(DailySummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return BuildUsagePoints(summary.TopDomains);
    }

    private static string FormatHourLabel(DateTimeOffset bucketStartUtc, TimeZoneInfo timeZone)
    {
        DateTimeOffset local = TimeZoneInfo.ConvertTime(bucketStartUtc, timeZone);

        return local.ToString("HH", CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<DashboardChartPoint> BuildUsagePoints(IEnumerable<UsageTotal> totals)
        => DashboardUsageLabelGrouper.GroupTotals(totals)
            .Select(total => new DashboardChartPoint(total.Key, total.DurationMs))
            .ToList();
}
