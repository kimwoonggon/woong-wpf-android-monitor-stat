using System.Windows;
using System.Windows.Controls;
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
                Assert.Equal(["app-1", "app-2", "app-3", "app-4", "app-5", "app-6", "app-7", "app-8", "app-9", "app-10"], viewModel.Chart.Labels);
            }
            finally
            {
                window.Close();
            }
        });
}
