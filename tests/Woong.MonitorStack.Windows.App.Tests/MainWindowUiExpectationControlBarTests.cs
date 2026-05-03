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
    public void DashboardView_HostsControlBarAndPreservesCommandBindings()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                ControlBar controlBar = FindByAutomationId<ControlBar>(window, "PeriodSelector");
                Button startTracking = FindByAutomationId<Button>(controlBar, "StartTrackingButton");
                Button stopTracking = FindByAutomationId<Button>(controlBar, "StopTrackingButton");
                Button refreshButton = FindByAutomationId<Button>(controlBar, "RefreshButton");
                Button syncNow = FindByAutomationId<Button>(controlBar, "SyncNowButton");

                Assert.Equal("▶ Start Tracking", startTracking.Content);
                Assert.Equal("■ Stop Tracking", stopTracking.Content);
                Assert.Equal("↻ Refresh", refreshButton.Content);
                Assert.Equal("⇅ Sync Now", syncNow.Content);
                Assert.Same(dashboard.ViewModel.StartTrackingCommand, startTracking.Command);
                Assert.Same(dashboard.ViewModel.StopTrackingCommand, stopTracking.Command);
                Assert.Same(dashboard.ViewModel.RefreshDashboardCommand, refreshButton.Command);
                Assert.Same(dashboard.ViewModel.SyncNowCommand, syncNow.Command);
                AssertPeriodButton(controlBar, "TodayPeriodButton", "Today", DashboardPeriod.Today, dashboard.ViewModel);
                AssertPeriodButton(controlBar, "LastHourPeriodButton", "1h", DashboardPeriod.LastHour, dashboard.ViewModel);
                AssertPeriodButton(controlBar, "Last6HoursPeriodButton", "6h", DashboardPeriod.Last6Hours, dashboard.ViewModel);
                AssertPeriodButton(controlBar, "Last24HoursPeriodButton", "24h", DashboardPeriod.Last24Hours, dashboard.ViewModel);
                AssertPeriodButton(controlBar, "CustomPeriodButton", "Custom", DashboardPeriod.Custom, dashboard.ViewModel);
                AssertReadableButton(startTracking);
                AssertReadableButton(stopTracking);
                AssertReadableButton(refreshButton);
                AssertReadableButton(syncNow);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void ControlBar_RendersGoalActionAndPeriodGroups()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                ControlBar controlBar = FindByAutomationId<ControlBar>(window, "PeriodSelector");
                FrameworkElement actionGroup = FindByAutomationId<FrameworkElement>(controlBar, "TrackingActionGroup");
                FrameworkElement periodGroup = FindByAutomationId<FrameworkElement>(controlBar, "PeriodFilterGroup");

                IReadOnlySet<string> actionText = CollectText(actionGroup);
                Assert.Contains("▶ Start Tracking", actionText);
                Assert.Contains("■ Stop Tracking", actionText);
                Assert.Contains("↻ Refresh", actionText);
                Assert.Contains("⇅ Sync Now", actionText);

                IReadOnlySet<string> periodText = CollectText(periodGroup);
                Assert.Contains("Today", periodText);
                Assert.Contains("1h", periodText);
                Assert.Contains("6h", periodText);
                Assert.Contains("24h", periodText);
                Assert.Contains("Custom", periodText);
            }
            finally
            {
                window.Close();
            }
        });
}
