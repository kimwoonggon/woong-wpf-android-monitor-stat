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

        return new DashboardLiveChartsData(
            [series],
            pointList.Select(point => point.Label).ToArray());
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
}
