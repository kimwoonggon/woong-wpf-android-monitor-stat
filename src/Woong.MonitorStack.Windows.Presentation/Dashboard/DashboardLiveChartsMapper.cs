using LiveChartsCore.SkiaSharpView;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public static class DashboardLiveChartsMapper
{
    private const int DefaultHorizontalCategoryLabelLength = 22;

    public static DashboardLiveChartsData BuildColumnChart(
        string seriesName,
        IEnumerable<DashboardChartPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        List<DashboardChartPoint> pointList = points.ToList();
        var series = new ColumnSeries<long>
        {
            Name = seriesName,
            Values = pointList.Select(point => point.ValueMs).ToArray()
        };
        string[] labels = pointList.Select(point => point.Label).ToArray();

        return new DashboardLiveChartsData(
            [series],
            labels,
            [new Axis { Labels = labels }],
            [new Axis { MinLimit = 0, Labeler = FormatMillisecondsAsMinutes }],
            pointList.Count == 0 ? "No data for selected period" : "");
    }

    public static DashboardLiveChartsData BuildHorizontalBarChart(
        string seriesName,
        IEnumerable<DashboardChartPoint> points,
        int? maxCategoryLabelLength = DefaultHorizontalCategoryLabelLength)
    {
        ArgumentNullException.ThrowIfNull(points);

        List<DashboardChartPoint> pointList = points
            .Select(point => new DashboardChartPoint(
                CompactCategoryLabel(point.Label, maxCategoryLabelLength),
                point.ValueMs))
            .GroupBy(point => point.Label, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DashboardChartPoint(group.First().Label, group.Sum(point => point.ValueMs)))
            .ToList();
        var series = new RowSeries<long>
        {
            Name = seriesName,
            Values = pointList.Select(point => point.ValueMs).ToArray()
        };
        string[] labels = pointList.Select(point => point.Label).ToArray();

        var categoryAxis = new Axis
        {
            Labels = labels,
            MinStep = 1,
            ForceStepToMin = true
        };
        if (maxCategoryLabelLength is null)
        {
            categoryAxis.TextSize = 11;
        }

        return new DashboardLiveChartsData(
            [series],
            labels,
            [new Axis { MinLimit = 0, Labeler = FormatMillisecondsAsMinutes }],
            [categoryAxis],
            pointList.Count == 0 ? "No data for selected period" : "");
    }

    private static string FormatMillisecondsAsMinutes(double value)
    {
        double safeValue = Math.Max(0, value);
        long minutes = Convert.ToInt64(Math.Round(safeValue / 60_000, MidpointRounding.AwayFromZero));

        return $"{minutes}m";
    }

    private static string CompactCategoryLabel(string label, int? maxLength)
    {
        string normalized = string.IsNullOrWhiteSpace(label) ? "(unknown)" : label.Trim();
        if (maxLength is null || normalized.Length <= maxLength.Value)
        {
            return normalized;
        }

        if (normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            string withoutExecutableSuffix = normalized[..^4];
            if (withoutExecutableSuffix.Length <= maxLength.Value)
            {
                return withoutExecutableSuffix;
            }

            normalized = withoutExecutableSuffix;
        }

        int safeMaxLength = Math.Max(4, maxLength.Value);
        string prefix = normalized[..(safeMaxLength - 3)].TrimEnd(' ', '.');
        return prefix + "...";
    }
}
