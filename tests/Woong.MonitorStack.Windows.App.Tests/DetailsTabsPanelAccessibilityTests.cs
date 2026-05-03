using System.Windows.Controls;
using Woong.MonitorStack.Windows.App.Views;
using static Woong.MonitorStack.Windows.App.Tests.WpfTestHelpers;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class DetailsTabsPanelAccessibilityTests
{
    [Fact]
    public void DetailsTabsPanel_PrimaryTabsAndListsExposeReadableAutomationNames()
        => RunContentWindowTest(
            () => new DetailsTabsPanel(),
            (window, panel) =>
            {
                AssertAutomationName<TabControl>(panel, "DashboardTabs", "Dashboard details tabs");
                AssertAutomationName<TabItem>(panel, "AppSessionsTab", "App Sessions");
                AssertAutomationName<TabItem>(panel, "WebSessionsTab", "Web Sessions");
                AssertAutomationName<TabItem>(panel, "LiveEventsTab", "Live Event Log");
                AssertAutomationName<TabItem>(panel, "SettingsTab", "Settings");
                AssertAutomationName<ComboBox>(panel, "DetailsRowsPerPageComboBox", "Rows per details page");

                TabControl tabs = FindByAutomationId<TabControl>(panel, "DashboardTabs");

                tabs.SelectedIndex = 0;
                window.UpdateLayout();
                AssertAutomationName<DataGrid>(panel, "RecentAppSessionsList", "Recent app sessions");

                tabs.SelectedIndex = 1;
                window.UpdateLayout();
                AssertAutomationName<DataGrid>(panel, "RecentWebSessionsList", "Recent web sessions");

                tabs.SelectedIndex = 2;
                window.UpdateLayout();
                AssertAutomationName<DataGrid>(panel, "LiveEventsList", "Live event log");
            });
}
