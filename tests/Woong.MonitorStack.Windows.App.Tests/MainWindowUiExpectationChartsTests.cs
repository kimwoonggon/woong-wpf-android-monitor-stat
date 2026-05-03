using LiveChartsCore.SkiaSharpView.WPF;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.App.Controls;
using Woong.MonitorStack.Windows.App.Views;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using static Woong.MonitorStack.Windows.App.Tests.WpfTestHelpers;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed partial class MainWindowUiExpectationTests
{

    [Fact]
    public void DashboardView_HostsChartsPanelAndPreservesChartContent()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                Invoke(FindByAutomationId<Button>(window, "RefreshButton"));
                window.UpdateLayout();

                ChartsPanel panel = FindByAutomationId<ChartsPanel>(window, "ChartArea");
                IReadOnlySet<string> panelText = CollectText(panel);

                Assert.Contains("시간대별 Active Focus", panelText);
                Assert.Contains("앱별 집중 시간", panelText);
                Assert.Contains("도메인별", panelText);
                Assert.Contains("집중 시간", panelText);
                Assert.NotNull(FindByAutomationId<CartesianChart>(panel, "HourlyActivityChart"));
                Assert.NotNull(FindByAutomationId<CartesianChart>(panel, "AppUsageChart"));
                Assert.IsType<CartesianChart>(FindByAutomationId<FrameworkElement>(panel, "DomainUsageChart"));
                Assert.NotNull(FindByAutomationId<TextBlock>(panel, "HourlyActivityEmptyStateText"));
                Assert.NotNull(FindByAutomationId<TextBlock>(panel, "AppUsageEmptyStateText"));
                Assert.NotNull(FindByAutomationId<TextBlock>(panel, "DomainUsageEmptyStateText"));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void DashboardView_ChartsPanelUsesSeparateGoalCardSurfaces()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                ChartsPanel panel = FindByAutomationId<ChartsPanel>(window, "ChartArea");

                Assert.NotNull(FindByAutomationId<SectionCard>(panel, "HourlyChartCard"));
                Assert.NotNull(FindByAutomationId<SectionCard>(panel, "AppUsageChartCard"));
                Assert.NotNull(FindByAutomationId<SectionCard>(panel, "DomainUsageChartCard"));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void DashboardView_ChartsPanelRendersSvgLikeHeaderIcons()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                ChartsPanel panel = FindByAutomationId<ChartsPanel>(window, "ChartArea");

                Assert.Equal("▥", FindByAutomationId<TextBlock>(panel, "HourlyChartIconText").Text);
                Assert.Equal("▰", FindByAutomationId<TextBlock>(panel, "AppUsageChartIconText").Text);
                Assert.Equal("◇", FindByAutomationId<TextBlock>(panel, "DomainUsageChartIconText").Text);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void DashboardView_ChartsPanelUsesSharedSectionTitleTypography()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                ChartsPanel panel = FindByAutomationId<ChartsPanel>(window, "ChartArea");
                Assert.IsType<Style>(panel.FindResource("SectionTitleTextStyle"));

                AssertSectionTitleStyle(FindTextBlock(panel, "시간대별 Active Focus"));
                AssertSectionTitleStyle(FindTextBlock(panel, "앱별 집중 시간"));
                AssertSectionTitleStyle(FindByAutomationId<TextBlock>(panel, "DomainUsageChartTitleFirstLine"));
                AssertSectionTitleStyle(FindByAutomationId<TextBlock>(panel, "DomainUsageChartTitleSecondLine"));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void DashboardView_ChartDetailButtonsSelectExpectedDetailsTabs()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                Button appDetails = FindByAutomationId<Button>(window, "AppChartDetailsButton");
                Button domainDetails = FindByAutomationId<Button>(window, "DomainChartDetailsButton");
                TabControl tabs = FindByAutomationId<TabControl>(window, "DashboardTabs");

                Assert.Same(dashboard.ViewModel.ShowAppFocusDetailsCommand, appDetails.Command);
                Assert.Same(dashboard.ViewModel.ShowDomainFocusDetailsCommand, domainDetails.Command);
                AssertCompactActionButton(appDetails);
                AssertCompactActionButton(domainDetails);

                Invoke(domainDetails);
                window.UpdateLayout();
                Assert.Equal(DetailsTab.WebSessions, dashboard.ViewModel.SelectedDetailsTab);
                Assert.Equal(1, tabs.SelectedIndex);

                Invoke(appDetails);
                window.UpdateLayout();
                Assert.Equal(DetailsTab.AppSessions, dashboard.ViewModel.SelectedDetailsTab);
                Assert.Equal(0, tabs.SelectedIndex);
            }
            finally
            {
                window.Close();
            }
        });
}
