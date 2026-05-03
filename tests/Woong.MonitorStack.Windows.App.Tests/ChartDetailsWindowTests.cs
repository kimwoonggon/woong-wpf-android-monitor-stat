using System.Windows;
using System.Windows.Controls;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.App.Views;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using static Woong.MonitorStack.Windows.App.Tests.WpfTestHelpers;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class ChartDetailsWindowTests
{
    [Fact]
    public void ChartDetailsWindow_RendersHorizontalBarChartRequest()
        => RunWindowTest(
            () =>
            {
                var request = new DashboardChartDetailsRequest(
                    "App focus details",
                    "Apps",
                    Enumerable.Range(1, 10)
                        .Select(index => new DashboardChartPoint($"app-{index}", index * 60_000))
                        .ToList());

                return new ChartDetailsWindow(request);
            },
            window =>
            {
                Assert.Equal("App focus details", window.Title);
                Assert.NotNull(FindByAutomationId<CartesianChart>(window, "ChartDetailsHorizontalBarChart"));
                var title = FindByAutomationId<TextBlock>(window, "ChartDetailsTitleText");
                Assert.Equal("App focus details", title.Text);
                var viewModel = Assert.IsType<ChartDetailsWindowViewModel>(window.DataContext);
                Assert.Equal(["app-10", "app-9", "app-8", "app-7", "app-6", "app-5", "app-4", "app-3", "app-2", "app-1"], viewModel.Chart.Labels);
            });

    [Fact]
    public void ChartDetailsWindow_ShowsFullLabelsAndDurationValuesForTopTenDetails()
        => RunWindowTest(
            () =>
            {
                const string longDomain = "very-long-domain-name-for-review.example.com";
                var request = new DashboardChartDetailsRequest(
                    "Domain focus details",
                    "Domains",
                    [
                        new DashboardChartPoint(longDomain, 600_000),
                        new DashboardChartPoint("chatgpt.com", 3_661_000)
                    ]);

                return new ChartDetailsWindow(request);
            },
            window =>
            {
                const string longDomain = "very-long-domain-name-for-review.example.com";
                Assert.NotNull(FindByAutomationId<ListView>(window, "ChartDetailsRowsList"));
                var viewModel = Assert.IsType<ChartDetailsWindowViewModel>(window.DataContext);
                Assert.Equal(["chatgpt.com", longDomain], viewModel.DetailRows.Select(row => row.Label));
                Assert.Equal("1h 01m 01s", viewModel.DetailRows[0].DurationText);
                Assert.Equal("10m", viewModel.DetailRows[1].DurationText);
            });

    [Fact]
    public void ChartDetailsWindow_RendersOneHorizontalBarPerLabelWithAlignedValues()
        => RunWindowTest(
            () =>
            {
                var request = new DashboardChartDetailsRequest(
                    "Domain focus details",
                    "Domains",
                    [
                        new DashboardChartPoint("github.com", 120_000),
                        new DashboardChartPoint("openai.com", 60_000),
                        new DashboardChartPoint("github.com", 180_000)
                    ]);

                return new ChartDetailsWindow(request);
            },
            window =>
            {
                Assert.NotNull(FindByAutomationId<CartesianChart>(window, "ChartDetailsHorizontalBarChart"));
                var viewModel = Assert.IsType<ChartDetailsWindowViewModel>(window.DataContext);
                Assert.Equal(["github.com", "openai.com"], viewModel.Chart.Labels);
                Assert.Equal(["github.com", "openai.com"], Assert.Single(viewModel.Chart.YAxes).Labels);
                var rowSeries = Assert.IsType<RowSeries<long>>(Assert.Single(viewModel.Chart.Series));
                Assert.Equal([300_000, 60_000], rowSeries.Values);
            });

    [Fact]
    public void ChartDetailsWindow_GroupsDuplicateAppLabelsIntoOneSummedHorizontalBar()
        => RunWindowTest(
            () =>
            {
                var request = new DashboardChartDetailsRequest(
                    "App focus details",
                    "Apps",
                    [
                        new DashboardChartPoint("Chrome", 600_000),
                        new DashboardChartPoint("Code.exe", 300_000),
                        new DashboardChartPoint("Chrome", 900_000)
                    ]);

                return new ChartDetailsWindow(request);
            },
            window =>
            {
                Assert.NotNull(FindByAutomationId<CartesianChart>(window, "ChartDetailsHorizontalBarChart"));
                var viewModel = Assert.IsType<ChartDetailsWindowViewModel>(window.DataContext);
                Assert.Equal(["Chrome", "Code.exe"], viewModel.Chart.Labels);
                Assert.Equal(["Chrome", "Code.exe"], Assert.Single(viewModel.Chart.YAxes).Labels);
                var rowSeries = Assert.IsType<RowSeries<long>>(Assert.Single(viewModel.Chart.Series));
                Assert.Equal([1_500_000, 300_000], rowSeries.Values);
            });

    [Fact]
    public void ChartDetailsWindow_GroupsCaseVariantsIntoOneHorizontalBar()
        => RunWindowTest(
            () =>
            {
                var request = new DashboardChartDetailsRequest(
                    "App focus details",
                    "Apps",
                    [
                        new DashboardChartPoint("Chrome.exe", 600_000),
                        new DashboardChartPoint("chrome.exe", 300_000)
                    ]);

                return new ChartDetailsWindow(request);
            },
            window =>
            {
                var viewModel = Assert.IsType<ChartDetailsWindowViewModel>(window.DataContext);
                DashboardChartPoint row = Assert.Single(
                    viewModel.DetailRows.Select(detail => new DashboardChartPoint(detail.Label, detail.ValueMs)));
                Assert.Equal("Chrome.exe", row.Label);
                Assert.Equal(900_000, row.ValueMs);
                var rowSeries = Assert.IsType<RowSeries<long>>(Assert.Single(viewModel.Chart.Series));
                Assert.Equal([900_000], rowSeries.Values);
            });

    [Fact]
    public void ChartDetailsWindowViewModel_SelectPeriodUpdatesChartRowsAndKeepsOrderAligned()
    {
        var request = new DashboardChartDetailsRequest(
            "App focus details",
            "Apps",
            Points:
            [
                new DashboardChartPoint("Chrome", 600_000),
                new DashboardChartPoint("Code.exe", 300_000)
            ],
            SelectedPeriod: DashboardPeriod.LastHour,
            PeriodPoints: new Dictionary<DashboardPeriod, IReadOnlyList<DashboardChartPoint>>
            {
                [DashboardPeriod.LastHour] =
                [
                    new DashboardChartPoint("Chrome", 600_000),
                    new DashboardChartPoint("Code.exe", 300_000)
                ],
                [DashboardPeriod.Last6Hours] =
                [
                    new DashboardChartPoint("Terminal", 900_000),
                    new DashboardChartPoint("Chrome", 120_000),
                    new DashboardChartPoint("Terminal", 60_000)
                ]
            });
        ChartDetailsWindowViewModel viewModel = ChartDetailsWindowViewModel.FromRequest(request);

        viewModel.SelectPeriod(DashboardPeriod.Last6Hours);

        Assert.Equal(DashboardPeriod.Last6Hours, viewModel.SelectedPeriod);
        Assert.Equal(["Terminal", "Chrome"], viewModel.DetailRows.Select(row => row.Label));
        Assert.Equal(["Terminal", "Chrome"], viewModel.Chart.Labels);
        Assert.Equal(["Terminal", "Chrome"], Assert.Single(viewModel.Chart.YAxes).Labels);
        var rowSeries = Assert.IsType<RowSeries<long>>(Assert.Single(viewModel.Chart.Series));
        Assert.Equal([960_000, 120_000], rowSeries.Values);
    }

    [Fact]
    public void ChartDetailsWindow_RendersPeriodFilterOptions()
        => RunWindowTest(
            () =>
            {
                var request = new DashboardChartDetailsRequest(
                    "Domain focus details",
                    "Domains",
                    Points: [new DashboardChartPoint("github.com", 600_000)],
                    SelectedPeriod: DashboardPeriod.Today,
                    PeriodPoints: new Dictionary<DashboardPeriod, IReadOnlyList<DashboardChartPoint>>
                    {
                        [DashboardPeriod.Today] = [new DashboardChartPoint("github.com", 600_000)],
                        [DashboardPeriod.LastHour] = [new DashboardChartPoint("github.com", 60_000)],
                        [DashboardPeriod.Last6Hours] = [new DashboardChartPoint("github.com", 360_000)],
                        [DashboardPeriod.Last24Hours] = [new DashboardChartPoint("github.com", 1_200_000)],
                        [DashboardPeriod.Custom] = [new DashboardChartPoint("github.com", 120_000)]
                    });

                return new ChartDetailsWindow(request);
            },
            window =>
            {
                var periodSelector = FindByAutomationId<ListBox>(window, "ChartDetailsPeriodSelector");
                Assert.Equal(5, periodSelector.Items.Count);
                var viewModel = Assert.IsType<ChartDetailsWindowViewModel>(window.DataContext);
                Assert.Equal(
                    ["Today", "1h", "6h", "24h", "Custom"],
                    viewModel.PeriodOptions.Select(option => option.Label));
            });

    [Fact]
    public void ChartDetailsWindow_CustomPeriodShowsRangeEditorAndAppliesCustomRange()
    {
        TimeRange? requestedRange = null;

        RunWindowTest(
            () =>
            {
                var request = new DashboardChartDetailsRequest(
                    "App focus details",
                    "Apps",
                    Points: [new DashboardChartPoint("Chrome", 60_000)],
                    SelectedPeriod: DashboardPeriod.Today,
                    PeriodPoints: new Dictionary<DashboardPeriod, IReadOnlyList<DashboardChartPoint>>
                    {
                        [DashboardPeriod.Today] = [new DashboardChartPoint("Chrome", 60_000)]
                    },
                    CustomRangePointsProvider: range =>
                    {
                        requestedRange = range;
                        return [new DashboardChartPoint("Terminal", 300_000)];
                    },
                    TimeZoneId: "Asia/Seoul");

                return new ChartDetailsWindow(request);
            },
            window =>
            {
                var viewModel = Assert.IsType<ChartDetailsWindowViewModel>(window.DataContext);
                viewModel.SelectPeriod(DashboardPeriod.Custom);
                window.UpdateLayout();

                FrameworkElement customPanel = FindByAutomationId<FrameworkElement>(window, "ChartDetailsCustomRangePanel");
                Assert.Equal(Visibility.Visible, customPanel.Visibility);
                Assert.NotNull(FindByAutomationId<DatePicker>(window, "ChartDetailsCustomStartDatePicker"));
                Assert.NotNull(FindByAutomationId<TextBox>(window, "ChartDetailsCustomStartTimeTextBox"));
                Assert.NotNull(FindByAutomationId<DatePicker>(window, "ChartDetailsCustomEndDatePicker"));
                Assert.NotNull(FindByAutomationId<TextBox>(window, "ChartDetailsCustomEndTimeTextBox"));
                Assert.NotNull(FindByAutomationId<Button>(window, "ChartDetailsApplyCustomRangeButton"));

                viewModel.CustomStartDate = new DateTime(2026, 5, 3);
                viewModel.CustomStartTimeText = "09:15";
                viewModel.CustomEndDate = new DateTime(2026, 5, 3);
                viewModel.CustomEndTimeText = "10:45";
                Assert.True(viewModel.ApplyCustomRangeCommand.CanExecute(null));
                viewModel.ApplyCustomRangeCommand.Execute(null);

                Assert.NotNull(requestedRange);
                Assert.Equal(new DateTimeOffset(2026, 5, 3, 0, 15, 0, TimeSpan.Zero), requestedRange!.StartedAtUtc);
                Assert.Equal(new DateTimeOffset(2026, 5, 3, 1, 45, 0, TimeSpan.Zero), requestedRange.EndedAtUtc);
                Assert.Equal(["Terminal"], viewModel.DetailRows.Select(row => row.Label));
                Assert.Contains("2026-05-03 09:15 - 2026-05-03 10:45", viewModel.CustomRangeStatusText);
            });
    }

    [Fact]
    public void ChartDetailsWindow_TopTenLongLabelsUseScrollableReadableLayout()
        => RunWindowTest(
            () =>
            {
                var request = new DashboardChartDetailsRequest(
                    "Domain focus details",
                    "Domains",
                    Enumerable.Range(1, 10)
                        .Select(index => new DashboardChartPoint($"very-long-review-domain-{index}.example.com", (11 - index) * 60_000))
                        .ToList());

                return new ChartDetailsWindow(request);
            },
            window =>
            {
                Assert.NotNull(FindByAutomationId<ScrollViewer>(window, "ChartDetailsRootScrollViewer"));

                var chart = FindByAutomationId<CartesianChart>(window, "ChartDetailsHorizontalBarChart");
                Assert.True(
                    chart.MinHeight >= 640,
                    "Ten full-label horizontal bars need enough vertical space to stay readable instead of compressing into the table area.");

                var viewModel = Assert.IsType<ChartDetailsWindowViewModel>(window.DataContext);
                Assert.Equal(10, viewModel.DetailRows.Count);
                Assert.Equal(viewModel.DetailRows.Select(row => row.Label), viewModel.Chart.Labels);

                Axis yAxis = Assert.Single(viewModel.Chart.YAxes);
                Assert.Equal(viewModel.Chart.Labels, yAxis.Labels);
                Assert.True(
                    yAxis.TextSize >= 13,
                    "Full app/domain labels in detail charts must remain readable.");
            });

    [Fact]
    public void ChartDetailsWindow_GivesTopTenDetailChartEnoughVerticalSpace()
        => RunWindowTest(
            () =>
            {
                var request = new DashboardChartDetailsRequest(
                    "App focus details",
                    "Apps",
                    Enumerable.Range(1, 10)
                        .Select(index => new DashboardChartPoint($"app-{index}", (11 - index) * 60_000))
                        .ToList());

                return new ChartDetailsWindow(request);
            },
            window =>
            {
                var chart = FindByAutomationId<CartesianChart>(window, "ChartDetailsHorizontalBarChart");
                Assert.True(
                    window.MinHeight <= 700,
                    "The detail window should be allowed to fit shorter screens while the root scroll viewer keeps the top-10 chart reachable.");
                Assert.True(
                    window.Height >= 900,
                    "Top-10 detail windows should open tall by default instead of relying on the user to resize before labels become stable.");
                Assert.Equal(WindowState.Maximized, window.WindowState);
                Assert.True(
                    chart.MinHeight >= 640,
                    "Top-10 detail charts need enough height to avoid overlapping forced Y-axis labels.");
            });
}
