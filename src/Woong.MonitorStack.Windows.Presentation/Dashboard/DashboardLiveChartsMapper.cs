using LiveChartsCore.SkiaSharpView;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public static class DashboardLiveChartsMapper
{
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

    public static IReadOnlyList<PieSeries<long>> BuildPieSeries(IEnumerable<DashboardChartPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        return points
            .Select(point => new PieSeries<long>
            {
                Name = point.Label,
                Values = [point.ValueMs]
            })
            .ToList();
    }

    private static string FormatMillisecondsAsMinutes(double value)
    {
        double safeValue = Math.Max(0, value);
        long minutes = Convert.ToInt64(Math.Round(safeValue / 60_000, MidpointRounding.AwayFromZero));

        return $"{minutes}m";
    }
}
