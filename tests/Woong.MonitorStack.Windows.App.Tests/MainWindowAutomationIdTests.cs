using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using static Woong.MonitorStack.Windows.App.Tests.WpfTestHelpers;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class MainWindowAutomationIdTests
{
    [Fact]
    public void MainWindow_ExposesStableAutomationIdsForSnapshotAutomation()
        => RunWindowTest(
            () =>
            {
                var viewModel = new DashboardViewModel(
                    new EmptyDataSource(),
                    new FixedClock(new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero)),
                    new DashboardOptions("Asia/Seoul"));

                return new MainWindow(viewModel);
            },
            window =>
            {
                ISet<string> automationIds = CollectAutomationIds(window);
                TabControl tabs = FindByAutomationId<TabControl>(window, "DashboardTabs");
                for (var selectedIndex = 0; selectedIndex < tabs.Items.Count; selectedIndex++)
                {
                    tabs.SelectedIndex = selectedIndex;
                    window.UpdateLayout();
                    automationIds.UnionWith(CollectAutomationIds(window));
                }

                Assert.Contains("MainWindow", automationIds);
                Assert.Contains("StartTrackingButton", automationIds);
                Assert.Contains("StopTrackingButton", automationIds);
                Assert.Contains("SyncNowButton", automationIds);
                Assert.Contains("TrackingStatusText", automationIds);
                Assert.Contains("CurrentAppNameText", automationIds);
                Assert.Contains("CurrentProcessNameText", automationIds);
                Assert.Contains("CurrentWindowTitleText", automationIds);
                Assert.Contains("BrowserCaptureStatusText", automationIds);
                Assert.Contains("CurrentSessionDurationText", automationIds);
                Assert.Contains("LastPersistedSessionText", automationIds);
                Assert.Contains("LastSyncStatusText", automationIds);
                Assert.Contains("RefreshButton", automationIds);
                Assert.Contains("PeriodSelector", automationIds);
                Assert.Contains("SummaryCardsContainer", automationIds);
                Assert.Contains("ChartArea", automationIds);
                Assert.Contains("RecentAppSessionsList", automationIds);
                Assert.Contains("WebSessionsTab", automationIds);
                Assert.Contains("LiveEventsTab", automationIds);
                Assert.Contains("RecentWebSessionsList", automationIds);
                Assert.Contains("LiveEventsList", automationIds);
                Assert.Contains("SettingsTab", automationIds);
                Assert.Contains("WindowTitleVisibleCheckBox", automationIds);
            });

    private static ISet<string> CollectAutomationIds(DependencyObject root)
        => FindVisualDescendants<DependencyObject>(root)
            .Select(AutomationProperties.GetAutomationId)
            .Where(automationId => !string.IsNullOrWhiteSpace(automationId))
            .ToHashSet(StringComparer.Ordinal);

    private sealed class EmptyDataSource : IDashboardDataSource
    {
        public IReadOnlyList<FocusSession> QueryFocusSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => [];

        public IReadOnlyList<WebSession> QueryWebSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => [];
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IDashboardClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
