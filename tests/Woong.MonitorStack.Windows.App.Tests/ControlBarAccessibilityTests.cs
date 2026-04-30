using System.Windows;
using System.Windows.Controls;
using Woong.MonitorStack.Windows.App.Views;
using static Woong.MonitorStack.Windows.App.Tests.WpfTestHelpers;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class ControlBarAccessibilityTests
{
    [Fact]
    public void ControlBar_ButtonsExposeReadableAutomationNames()
        => RunOnStaThread(() =>
        {
            var panel = new ControlBar();
            var window = new Window { Content = panel };

            try
            {
                window.Show();
                window.UpdateLayout();

                AssertAutomationName<Button>(panel, "StartTrackingButton", "Start tracking");
                AssertAutomationName<Button>(panel, "StopTrackingButton", "Stop tracking");
                AssertAutomationName<Button>(panel, "RefreshButton", "Refresh dashboard");
                AssertAutomationName<Button>(panel, "SyncNowButton", "Sync now");
                AssertAutomationName<Button>(panel, "TodayPeriodButton", "Show today");
                AssertAutomationName<Button>(panel, "LastHourPeriodButton", "Show last hour");
                AssertAutomationName<Button>(panel, "Last6HoursPeriodButton", "Show last 6 hours");
                AssertAutomationName<Button>(panel, "Last24HoursPeriodButton", "Show last 24 hours");
                AssertAutomationName<Button>(panel, "CustomPeriodButton", "Show custom range");
            }
            finally
            {
                window.Close();
            }
        });
}
