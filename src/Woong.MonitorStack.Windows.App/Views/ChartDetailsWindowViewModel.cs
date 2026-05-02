using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Views;

public sealed record ChartDetailsWindowViewModel(
    string Title,
    DashboardLiveChartsData Chart,
    IReadOnlyList<ChartDetailsRow> DetailRows)
{
    public static ChartDetailsWindowViewModel FromRequest(DashboardChartDetailsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<DashboardChartPoint> points = request.Points
            .GroupBy(point => point.Label, StringComparer.Ordinal)
            .Select(group => new DashboardChartPoint(group.Key, group.Sum(point => point.ValueMs)))
            .OrderByDescending(point => point.ValueMs)
            .ThenBy(point => point.Label, StringComparer.Ordinal)
            .ToList();

        return new ChartDetailsWindowViewModel(
            request.Title,
            DashboardLiveChartsMapper.BuildHorizontalBarChart(
                request.SeriesName,
                points,
                maxCategoryLabelLength: null),
            points.Select(point => new ChartDetailsRow(
                    point.Label,
                    point.ValueMs,
                    FormatDuration(point.ValueMs)))
                .ToList());
    }

    private static string FormatDuration(long durationMs)
    {
        if (durationMs <= 0)
        {
            return "0s";
        }

        long totalSeconds = Math.Max(1, (durationMs + 999) / 1_000);
        long hours = totalSeconds / 3_600;
        long minutes = totalSeconds % 3_600 / 60;
        long seconds = totalSeconds % 60;

        if (hours > 0)
        {
            return seconds > 0
                ? $"{hours}h {minutes:D2}m {seconds:D2}s"
                : $"{hours}h {minutes:D2}m";
        }

        if (minutes > 0)
        {
            return seconds > 0
                ? $"{minutes}m {seconds:D2}s"
                : $"{minutes}m";
        }

        return $"{seconds}s";
    }
}

public sealed record ChartDetailsRow(
    string Label,
    long ValueMs,
    string DurationText);
