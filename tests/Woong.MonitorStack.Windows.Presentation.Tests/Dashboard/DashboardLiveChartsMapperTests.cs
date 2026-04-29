using LiveChartsCore.SkiaSharpView;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.Presentation.Tests.Dashboard;

public sealed class DashboardLiveChartsMapperTests
{
    [Fact]
    public void BuildColumnChart_WithEmptyInput_ReturnsNamedEmptySeries()
    {
        DashboardLiveChartsData chart = DashboardLiveChartsMapper.BuildColumnChart("Activity", []);

        Assert.Empty(chart.Labels);
        Assert.Equal("No data for selected period", chart.EmptyStateText);
        Assert.Empty(Assert.Single(chart.XAxes).Labels ?? []);
        Assert.Equal("0m", Assert.Single(chart.YAxes).Labeler?.Invoke(0));
        var series = Assert.Single(chart.Series);
        var columnSeries = Assert.IsType<ColumnSeries<long>>(series);
        Assert.Equal("Activity", columnSeries.Name);
        Assert.Empty(columnSeries.Values ?? []);
    }

    [Fact]
    public void BuildColumnChart_MapsLabelsAndValues()
    {
        var points = new[]
        {
            new DashboardChartPoint("Chrome", 1_200_000),
            new DashboardChartPoint("VS Code", 600_000)
        };

        DashboardLiveChartsData chart = DashboardLiveChartsMapper.BuildColumnChart("Apps", points);

        Assert.Equal(["Chrome", "VS Code"], chart.Labels);
        Assert.Equal("", chart.EmptyStateText);
        Assert.Equal(["Chrome", "VS Code"], Assert.Single(chart.XAxes).Labels);
        Assert.Equal("10m", Assert.Single(chart.YAxes).Labeler?.Invoke(600_000));
        Assert.Equal("60m", Assert.Single(chart.YAxes).Labeler?.Invoke(3_600_000));
        var series = Assert.Single(chart.Series);
        var columnSeries = Assert.IsType<ColumnSeries<long>>(series);
        Assert.Equal("Apps", columnSeries.Name);
        Assert.Equal([1_200_000, 600_000], columnSeries.Values);
    }

    [Fact]
    public void BuildColumnChart_MapsDomainLabelsAndValues()
    {
        var points = new[]
        {
            new DashboardChartPoint("example.com", 600_000),
            new DashboardChartPoint("openai.com", 300_000)
        };

        DashboardLiveChartsData chart = DashboardLiveChartsMapper.BuildColumnChart("Domains", points);

        Assert.Equal(["example.com", "openai.com"], chart.Labels);
        Assert.Equal(["example.com", "openai.com"], Assert.Single(chart.XAxes).Labels);
        var series = Assert.IsType<ColumnSeries<long>>(Assert.Single(chart.Series));
        Assert.Equal("Domains", series.Name);
        Assert.Equal([600_000, 300_000], series.Values);
    }
}
