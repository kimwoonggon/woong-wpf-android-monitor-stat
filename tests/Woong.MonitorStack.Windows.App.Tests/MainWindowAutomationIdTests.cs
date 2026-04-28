using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class MainWindowAutomationIdTests
{
    [Fact]
    public void MainWindow_ExposesStableAutomationIdsForSnapshotAutomation()
        => RunOnStaThread(() =>
        {
            var viewModel = new DashboardViewModel(
                new EmptyDataSource(),
                new FixedClock(new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero)),
                new DashboardOptions("Asia/Seoul"));
            var window = new MainWindow(viewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                ISet<string> automationIds = CollectAutomationIds(window);
                TabControl tabs = FindTabControl(window);
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
            }
            finally
            {
                window.Close();
            }
        });

    private static ISet<string> CollectAutomationIds(DependencyObject root)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        CollectAutomationIds(root, ids);

        return ids;
    }

    private static void CollectAutomationIds(DependencyObject element, ISet<string> ids)
    {
        string automationId = AutomationProperties.GetAutomationId(element);
        if (!string.IsNullOrWhiteSpace(automationId))
        {
            ids.Add(automationId);
        }

        int childCount = VisualTreeHelper.GetChildrenCount(element);
        for (var index = 0; index < childCount; index++)
        {
            CollectAutomationIds(VisualTreeHelper.GetChild(element, index), ids);
        }
    }

    private static TabControl FindTabControl(DependencyObject root)
    {
        if (root is TabControl tabControl)
        {
            return tabControl;
        }

        int childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < childCount; index++)
        {
            try
            {
                return FindTabControl(VisualTreeHelper.GetChild(root, index));
            }
            catch (InvalidOperationException)
            {
            }
        }

        throw new InvalidOperationException("Dashboard tab control was not found.");
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw failure;
        }
    }

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
