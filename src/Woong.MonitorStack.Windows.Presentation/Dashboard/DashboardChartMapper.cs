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
        => totals
            .GroupBy(total => total.Key, StringComparer.Ordinal)
            .Select(group => new DashboardChartPoint(group.Key, group.Sum(total => total.DurationMs)))
            .OrderByDescending(point => point.ValueMs)
            .ThenBy(point => point.Label, StringComparer.Ordinal)
            .ToList();
}
