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
    public void DetailsTabsPanel_RendersSvgLikeTabIconsAndIconPager()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                DetailsTabsPanel panel = FindByAutomationId<DetailsTabsPanel>(window, "DetailsTabsPanel");

                Assert.Equal("▦", FindByAutomationId<TextBlock>(panel, "AppSessionsTabIcon").Text);
                Assert.Equal("◎", FindByAutomationId<TextBlock>(panel, "WebSessionsTabIcon").Text);
                Assert.Equal("≡", FindByAutomationId<TextBlock>(panel, "LiveEventsTabIcon").Text);
                Assert.Equal("⚙", FindByAutomationId<TextBlock>(panel, "SettingsTabIcon").Text);
                Assert.Equal("‹", FindByAutomationId<Button>(panel, "DetailsPreviousPageButton").Content);
                Assert.Equal("›", FindByAutomationId<Button>(panel, "DetailsNextPageButton").Content);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void DetailsTabsPanel_PagerControlsExposeReadableAutomationNames()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                DetailsTabsPanel panel = FindByAutomationId<DetailsTabsPanel>(window, "DetailsTabsPanel");
                Button previous = FindByAutomationId<Button>(panel, "DetailsPreviousPageButton");
                Button next = FindByAutomationId<Button>(panel, "DetailsNextPageButton");
                TextBlock pageStatus = FindByAutomationId<TextBlock>(panel, "DetailsPageStatusText");

                Assert.Equal("Previous details page", AutomationProperties.GetName(previous));
                Assert.Equal("Next details page", AutomationProperties.GetName(next));
                Assert.Equal("Current details page", AutomationProperties.GetName(pageStatus));
                Assert.Equal("1 / 1", pageStatus.Text);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void DashboardView_HostsDetailsTabsPanelAndPreservesTabsBinding()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                DetailsTabsPanel panel = FindByAutomationId<DetailsTabsPanel>(window, "DetailsTabsPanel");
                TabControl tabs = FindByAutomationId<TabControl>(panel, "DashboardTabs");

                Assert.Equal(["App Sessions", "Web Sessions", "Live Event Log", "Settings"], TabHeaders(tabs));
                Assert.Equal("Tag", tabs.SelectedValuePath);
                Assert.Equal(DetailsTab.AppSessions, dashboard.ViewModel.SelectedDetailsTab);

                tabs.SelectedIndex = 3;
                window.UpdateLayout();

                Assert.Equal(DetailsTab.Settings, dashboard.ViewModel.SelectedDetailsTab);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void DashboardView_UsesVerticalRootScrollAndKeepsGridHorizontalScroll()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                DashboardView dashboardView = FindByAutomationId<DashboardView>(window, "DashboardView");
                ScrollViewer rootScrollViewer = FindVisualDescendants<ScrollViewer>(dashboardView)
                    .First(scrollViewer => scrollViewer.Content is Grid);

                Assert.Equal(ScrollBarVisibility.Auto, rootScrollViewer.VerticalScrollBarVisibility);
                Assert.Equal(ScrollBarVisibility.Disabled, rootScrollViewer.HorizontalScrollBarVisibility);

                TabControl tabs = FindByAutomationId<TabControl>(window, "DashboardTabs");

                tabs.SelectedIndex = 0;
                window.UpdateLayout();
                DataGrid appSessions = FindByAutomationId<DataGrid>(window, "RecentAppSessionsList");
                Assert.Equal(ScrollBarVisibility.Auto, ScrollViewer.GetHorizontalScrollBarVisibility(appSessions));

                tabs.SelectedIndex = 1;
                window.UpdateLayout();
                DataGrid webSessions = FindByAutomationId<DataGrid>(window, "RecentWebSessionsList");
                Assert.Equal(ScrollBarVisibility.Auto, ScrollViewer.GetHorizontalScrollBarVisibility(webSessions));

                tabs.SelectedIndex = 2;
                window.UpdateLayout();
                DataGrid liveEvents = FindByAutomationId<DataGrid>(window, "LiveEventsList");
                Assert.Equal(ScrollBarVisibility.Auto, ScrollViewer.GetHorizontalScrollBarVisibility(liveEvents));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void DetailsTabsPanel_HostsSettingsPanelInsideSettingsTab()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                TabControl tabs = FindByAutomationId<TabControl>(window, "DashboardTabs");
                tabs.SelectedIndex = 3;
                window.UpdateLayout();

                Assert.Equal(DetailsTab.Settings, dashboard.ViewModel.SelectedDetailsTab);
                Assert.NotNull(FindByAutomationId<TabItem>(window, "SettingsTab"));
                Assert.NotNull(FindByAutomationId<SettingsPanel>(window, "SettingsPanel"));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void DetailsTabsPanel_UsesSharedPagerTypography()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                Invoke(FindByAutomationId<Button>(window, "RefreshButton"));
                window.UpdateLayout();

                DetailsTabsPanel panel = FindByAutomationId<DetailsTabsPanel>(window, "DetailsTabsPanel");

                AssertMutedTextStyle(FindTextBlock(panel, "Rows per page:"));
                AssertBodyTextStyle(FindByAutomationId<TextBlock>(panel, "DetailsPageStatusText"));
            }
            finally
            {
                window.Close();
            }
        });
}
