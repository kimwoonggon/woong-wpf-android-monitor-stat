using System.Windows.Controls;
using Woong.MonitorStack.Windows.App.Views;
using static Woong.MonitorStack.Windows.App.Tests.WpfTestHelpers;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class ControlBarAccessibilityTests
{
    [Fact]
    public void ControlBar_ButtonsExposeReadableAutomationNames()
        => RunContentWindowTest(
            () => new ControlBar(),
            panel =>
            {
                AssertAutomationName<Button>(panel, "StartTrackingButton", "Start tracking");
                AssertAutomationName<Button>(panel, "StopTrackingButton", "Stop tracking");
                AssertAutomationName<Button>(panel, "RefreshButton", "Refresh dashboard");
                AssertAutomationName<Button>(panel, "SyncNowButton", "Sync now");
                AssertAutomationName<Button>(panel, "TodayPeriodButton", "Show today");
                AssertAutomationName<Button>(panel, "LastHourPeriodButton", "Show last hour");
                AssertAutomationName<Button>(panel, "Last6HoursPeriodButton", "Show last 6 hours");
                AssertAutomationName<Button>(panel, "Last24HoursPeriodButton", "Show last 24 hours");
                AssertAutomationName<Button>(panel, "CustomPeriodButton", "Show custom range");
            });

    [Fact]
    public void ControlBar_CustomRangeEditorExposesDateTimeInputsAndApplyCommand()
        => RunContentWindowTest(
            () => new ControlBar(),
            panel =>
            {
                AssertAutomationName<DatePicker>(panel, "CustomStartDatePicker", "Custom range start date");
                AssertAutomationName<TextBox>(panel, "CustomStartTimeTextBox", "Custom range start time");
                AssertAutomationName<DatePicker>(panel, "CustomEndDatePicker", "Custom range end date");
                AssertAutomationName<TextBox>(panel, "CustomEndTimeTextBox", "Custom range end time");
                AssertAutomationName<Button>(panel, "ApplyCustomRangeButton", "Apply custom range");
                Assert.NotNull(FindByAutomationId<TextBlock>(panel, "CustomRangeStatusText"));
            });
}
