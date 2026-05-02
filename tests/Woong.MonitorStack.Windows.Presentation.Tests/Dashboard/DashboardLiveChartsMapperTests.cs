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

    [Fact]
    public void BuildHorizontalBarChart_UsesRowSeriesAndCategoryLabelsOnYAxis()
    {
        var points = new[]
        {
            new DashboardChartPoint("Chrome", 1_200_000),
            new DashboardChartPoint("VS Code", 600_000)
        };

        DashboardLiveChartsData chart = DashboardLiveChartsMapper.BuildHorizontalBarChart("Apps", points);

        Assert.Equal(["Chrome", "VS Code"], chart.Labels);
        Assert.Equal("", chart.EmptyStateText);
        Assert.Empty(Assert.Single(chart.XAxes).Labels ?? []);
        Assert.Equal("10m", Assert.Single(chart.XAxes).Labeler?.Invoke(600_000));
        Assert.Equal(["Chrome", "VS Code"], Assert.Single(chart.YAxes).Labels);
        var rowSeries = Assert.IsType<RowSeries<long>>(Assert.Single(chart.Series));
        Assert.Equal("Apps", rowSeries.Name);
        Assert.Equal([1_200_000, 600_000], rowSeries.Values);
    }

    [Fact]
    public void BuildHorizontalBarChart_CompactsLongExecutableLabelsForDashboardCards()
    {
        var points = new[]
        {
            new DashboardChartPoint("Woong.MonitorStack.Windows.App.exe", 1_200_000),
            new DashboardChartPoint("Code.exe", 600_000)
        };

        DashboardLiveChartsData chart = DashboardLiveChartsMapper.BuildHorizontalBarChart("Apps", points);

        Assert.Equal(["Woong.MonitorStack...", "Code.exe"], chart.Labels);
        Assert.Equal(["Woong.MonitorStack...", "Code.exe"], Assert.Single(chart.YAxes).Labels);
    }

    [Fact]
    public void BuildHorizontalBarChart_WhenLabelLimitIsDisabled_PreservesLongLabelsForDetails()
    {
        var points = new[]
        {
            new DashboardChartPoint("Woong.MonitorStack.Windows.App.exe", 1_200_000)
        };

        DashboardLiveChartsData chart = DashboardLiveChartsMapper.BuildHorizontalBarChart(
            "Apps",
            points,
            maxCategoryLabelLength: null);

        Assert.Equal(["Woong.MonitorStack.Windows.App.exe"], chart.Labels);
        Assert.Equal(["Woong.MonitorStack.Windows.App.exe"], Assert.Single(chart.YAxes).Labels);
    }

    [Fact]
    public void BuildHorizontalBarChart_ForDetailsGroupsDuplicateAppLabelsIntoOneSummedBar()
    {
        var points = new[]
        {
            new DashboardChartPoint("Chrome", 600_000),
            new DashboardChartPoint("Code.exe", 300_000),
            new DashboardChartPoint("Chrome", 900_000)
        };

        DashboardLiveChartsData chart = DashboardLiveChartsMapper.BuildHorizontalBarChart(
            "Apps",
            points,
            maxCategoryLabelLength: null);

        Assert.Equal(["Chrome", "Code.exe"], chart.Labels);
        Assert.Equal(["Chrome", "Code.exe"], Assert.Single(chart.YAxes).Labels);
        var rowSeries = Assert.IsType<RowSeries<long>>(Assert.Single(chart.Series));
        Assert.Equal([1_500_000, 300_000], rowSeries.Values);
    }

    [Fact]
    public void BuildHorizontalBarChart_ForDetailsGroupsDuplicateDomainLabelsIntoOneSummedBar()
    {
        var points = new[]
        {
            new DashboardChartPoint("chatgpt.com", 120_000),
            new DashboardChartPoint("github.com", 300_000),
            new DashboardChartPoint("chatgpt.com", 480_000)
        };

        DashboardLiveChartsData chart = DashboardLiveChartsMapper.BuildHorizontalBarChart(
            "Domains",
            points,
            maxCategoryLabelLength: null);

        Assert.Equal(["chatgpt.com", "github.com"], chart.Labels);
        Assert.Equal(["chatgpt.com", "github.com"], Assert.Single(chart.YAxes).Labels);
        var rowSeries = Assert.IsType<RowSeries<long>>(Assert.Single(chart.Series));
        Assert.Equal([600_000, 300_000], rowSeries.Values);
    }

    [Fact]
    public void BuildHorizontalBarChart_ForDetailsForcesEveryCategoryLabel()
    {
        DashboardChartPoint[] points = Enumerable.Range(1, 10)
            .Select(index => new DashboardChartPoint($"app-{index}", (11 - index) * 60_000))
            .ToArray();

        DashboardLiveChartsData chart = DashboardLiveChartsMapper.BuildHorizontalBarChart(
            "Apps",
            points,
            maxCategoryLabelLength: null);

        Axis yAxis = Assert.Single(chart.YAxes);
        Assert.Equal(points.Select(point => point.Label), yAxis.Labels);
        Assert.Equal(1, yAxis.MinStep);
        Assert.True(yAxis.ForceStepToMin);
    }

    [Fact]
    public void BuildHorizontalBarChart_ForDetailsUsesSmallerCategoryLabelText()
    {
        DashboardChartPoint[] points = Enumerable.Range(1, 10)
            .Select(index => new DashboardChartPoint($"domain-{index}.example", (11 - index) * 60_000))
            .ToArray();

        DashboardLiveChartsData chart = DashboardLiveChartsMapper.BuildHorizontalBarChart(
            "Domains",
            points,
            maxCategoryLabelLength: null);

        Axis yAxis = Assert.Single(chart.YAxes);
        Assert.Equal(11, yAxis.TextSize);
    }
}
