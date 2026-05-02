using System.Windows;
using System.Windows.Controls;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using Woong.MonitorStack.Windows.App.Views;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using static Woong.MonitorStack.Windows.App.Tests.WpfTestHelpers;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class ChartDetailsWindowTests
{
    [Fact]
    public void ChartDetailsWindow_RendersHorizontalBarChartRequest()
        => RunOnStaThread(() =>
        {
            var request = new DashboardChartDetailsRequest(
                "App focus details",
                "Apps",
                Enumerable.Range(1, 10)
                    .Select(index => new DashboardChartPoint($"app-{index}", index * 60_000))
                    .ToList());
            var window = new ChartDetailsWindow(request);

            try
            {
                window.Show();
                window.UpdateLayout();

                Assert.Equal("App focus details", window.Title);
                Assert.NotNull(FindByAutomationId<CartesianChart>(window, "ChartDetailsHorizontalBarChart"));
                var title = FindByAutomationId<TextBlock>(window, "ChartDetailsTitleText");
                Assert.Equal("App focus details", title.Text);
                var viewModel = Assert.IsType<ChartDetailsWindowViewModel>(window.DataContext);
                Assert.Equal(["app-10", "app-9", "app-8", "app-7", "app-6", "app-5", "app-4", "app-3", "app-2", "app-1"], viewModel.Chart.Labels);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void ChartDetailsWindow_ShowsFullLabelsAndDurationValuesForTopTenDetails()
        => RunOnStaThread(() =>
        {
            const string longDomain = "very-long-domain-name-for-review.example.com";
            var request = new DashboardChartDetailsRequest(
                "Domain focus details",
                "Domains",
                [
                    new DashboardChartPoint(longDomain, 600_000),
                    new DashboardChartPoint("chatgpt.com", 3_661_000)
                ]);
            var window = new ChartDetailsWindow(request);

            try
            {
                window.Show();
                window.UpdateLayout();

                Assert.NotNull(FindByAutomationId<ListView>(window, "ChartDetailsRowsList"));
                var viewModel = Assert.IsType<ChartDetailsWindowViewModel>(window.DataContext);
                Assert.Equal(["chatgpt.com", longDomain], viewModel.DetailRows.Select(row => row.Label));
                Assert.Equal("1h 01m 01s", viewModel.DetailRows[0].DurationText);
                Assert.Equal("10m", viewModel.DetailRows[1].DurationText);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void ChartDetailsWindow_RendersOneHorizontalBarPerLabelWithAlignedValues()
        => RunOnStaThread(() =>
        {
            var request = new DashboardChartDetailsRequest(
                "Domain focus details",
                "Domains",
                [
                    new DashboardChartPoint("github.com", 120_000),
                    new DashboardChartPoint("openai.com", 60_000),
                    new DashboardChartPoint("github.com", 180_000)
                ]);
            var window = new ChartDetailsWindow(request);

            try
            {
                window.Show();
                window.UpdateLayout();

                Assert.NotNull(FindByAutomationId<CartesianChart>(window, "ChartDetailsHorizontalBarChart"));
                var viewModel = Assert.IsType<ChartDetailsWindowViewModel>(window.DataContext);
                Assert.Equal(["github.com", "openai.com"], viewModel.Chart.Labels);
                Assert.Equal(["github.com", "openai.com"], Assert.Single(viewModel.Chart.YAxes).Labels);
                var rowSeries = Assert.IsType<RowSeries<long>>(Assert.Single(viewModel.Chart.Series));
                Assert.Equal([300_000, 60_000], rowSeries.Values);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void ChartDetailsWindow_GroupsDuplicateAppLabelsIntoOneSummedHorizontalBar()
        => RunOnStaThread(() =>
        {
            var request = new DashboardChartDetailsRequest(
                "App focus details",
                "Apps",
                [
                    new DashboardChartPoint("Chrome", 600_000),
                    new DashboardChartPoint("Code.exe", 300_000),
                    new DashboardChartPoint("Chrome", 900_000)
                ]);
            var window = new ChartDetailsWindow(request);

            try
            {
                window.Show();
                window.UpdateLayout();

                Assert.NotNull(FindByAutomationId<CartesianChart>(window, "ChartDetailsHorizontalBarChart"));
                var viewModel = Assert.IsType<ChartDetailsWindowViewModel>(window.DataContext);
                Assert.Equal(["Chrome", "Code.exe"], viewModel.Chart.Labels);
                Assert.Equal(["Chrome", "Code.exe"], Assert.Single(viewModel.Chart.YAxes).Labels);
                var rowSeries = Assert.IsType<RowSeries<long>>(Assert.Single(viewModel.Chart.Series));
                Assert.Equal([1_500_000, 300_000], rowSeries.Values);
            }
            finally
            {
                window.Close();
            }
        });
}
