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

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class MainWindowUiExpectationTests
{
    [Fact]
    public void MainWindow_ExposesDashboardControlsAndCommandBindings()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                Assert.Equal("Woong Monitor Stack", window.Title);
                Assert.True(window.Width >= 1024);
                Assert.True(window.MinWidth >= 1024);
                Assert.True(window.MinHeight >= 768);
                Assert.Same(dashboard.ViewModel, window.DataContext);
                Assert.NotNull(FindByAutomationId<DashboardView>(window, "DashboardView"));

                FrameworkElement header = FindByAutomationId<FrameworkElement>(window, "HeaderArea");
                IReadOnlySet<string> headerText = CollectText(header);
                Assert.Contains("Woong Monitor Stack", headerText);
                Assert.Contains("Windows Focus Tracker", headerText);
                Assert.Contains("Tracking Stopped", headerText);
                Assert.Contains("Sync Off", headerText);
                Assert.Contains("Privacy Safe", headerText);
                Assert.DoesNotContain("chrome.exe", headerText);

                Button refreshButton = FindByAutomationId<Button>(window, "RefreshButton");
                Assert.Equal("Refresh", refreshButton.Content);
                Assert.Same(dashboard.ViewModel.RefreshDashboardCommand, refreshButton.Command);

                AssertPeriodButton(window, "TodayPeriodButton", "Today", DashboardPeriod.Today, dashboard.ViewModel);
                AssertPeriodButton(window, "LastHourPeriodButton", "1h", DashboardPeriod.LastHour, dashboard.ViewModel);
                AssertPeriodButton(window, "Last6HoursPeriodButton", "6h", DashboardPeriod.Last6Hours, dashboard.ViewModel);
                AssertPeriodButton(window, "Last24HoursPeriodButton", "24h", DashboardPeriod.Last24Hours, dashboard.ViewModel);

                Button startTracking = FindByAutomationId<Button>(window, "StartTrackingButton");
                Button stopTracking = FindByAutomationId<Button>(window, "StopTrackingButton");
                Button syncNow = FindByAutomationId<Button>(window, "SyncNowButton");
                Button customPeriod = FindByAutomationId<Button>(window, "CustomPeriodButton");
                Assert.Equal("Start Tracking", startTracking.Content);
                Assert.Equal("Stop Tracking", stopTracking.Content);
                Assert.Equal("Sync Now", syncNow.Content);
                Assert.Equal("Custom", customPeriod.Content);
                AssertReadableButton(startTracking);
                AssertReadableButton(stopTracking);
                AssertReadableButton(syncNow);
                AssertReadableButton(refreshButton);
                AssertReadableButton(customPeriod);
                Assert.Same(dashboard.ViewModel.StartTrackingCommand, startTracking.Command);
                Assert.Same(dashboard.ViewModel.StopTrackingCommand, stopTracking.Command);
                Assert.Same(dashboard.ViewModel.SyncNowCommand, syncNow.Command);

                Assert.Equal("Stopped", FindByAutomationId<TextBlock>(window, "TrackingStatusText").Text);
                Assert.Equal("No current app", FindByAutomationId<TextBlock>(window, "CurrentAppNameText").Text);
                Assert.Equal("No process", FindByAutomationId<TextBlock>(window, "CurrentProcessNameText").Text);
                Assert.Equal(
                    "Window title hidden by privacy settings",
                    FindByAutomationId<TextBlock>(window, "CurrentWindowTitleText").Text);
                Assert.Equal("00:00:00", FindByAutomationId<TextBlock>(window, "CurrentSessionDurationText").Text);
                Assert.Equal("No session persisted", FindByAutomationId<TextBlock>(window, "LastPersistedSessionText").Text);
                Assert.NotNull(FindByAutomationId<TextBlock>(window, "CurrentBrowserDomainText"));
                Assert.NotNull(FindByAutomationId<TextBlock>(window, "LastPollTimeText"));
                Assert.NotNull(FindByAutomationId<TextBlock>(window, "LastDbWriteTimeText"));
                Assert.Equal(
                    "Sync is off. Data stays on this Windows device.",
                    FindByAutomationId<TextBlock>(window, "LastSyncStatusText").Text);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void DashboardView_HostsHeaderStatusBarAndPreservesHeaderContent()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                HeaderStatusBar header = FindByAutomationId<HeaderStatusBar>(window, "HeaderArea");
                IReadOnlySet<string> headerText = CollectText(header);

                Assert.Contains("Woong Monitor Stack", headerText);
                Assert.Contains("Windows Focus Tracker", headerText);
                Assert.Contains("Tracking Stopped", headerText);
                Assert.Contains("Sync Off", headerText);
                Assert.Contains("Privacy Safe", headerText);
                Assert.DoesNotContain("chrome.exe", headerText);
                Assert.NotNull(FindByAutomationId<Border>(header, "TrackingStatusBadge"));
                Assert.NotNull(FindByAutomationId<Border>(header, "SyncStatusBadge"));
                Assert.NotNull(FindByAutomationId<Border>(header, "PrivacyStatusBadge"));
            }
            finally
            {
                window.Close();
            }
        });

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

                Assert.Equal("Start Tracking", startTracking.Content);
                Assert.Equal("Stop Tracking", stopTracking.Content);
                Assert.Equal("Refresh", refreshButton.Content);
                Assert.Equal("Sync Now", syncNow.Content);
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
    public void DashboardView_HostsCurrentFocusPanelAndPreservesCurrentFocusBindings()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                CurrentFocusPanel panel = FindByAutomationId<CurrentFocusPanel>(window, "CurrentActivityPanel");
                IReadOnlySet<string> panelText = CollectText(panel);

                Assert.Contains("Current Focus", panelText);
                Assert.Equal("Stopped", FindByAutomationId<TextBlock>(panel, "TrackingStatusText").Text);
                Assert.Equal("No current app", FindByAutomationId<TextBlock>(panel, "CurrentAppNameText").Text);
                Assert.Equal("No process", FindByAutomationId<TextBlock>(panel, "CurrentProcessNameText").Text);
                Assert.Equal(
                    "Window title hidden by privacy settings",
                    FindByAutomationId<TextBlock>(panel, "CurrentWindowTitleText").Text);
                Assert.Equal(
                    "Browser metadata unavailable",
                    FindByAutomationId<TextBlock>(panel, "CurrentBrowserDomainText").Text);
                Assert.Equal("00:00:00", FindByAutomationId<TextBlock>(panel, "CurrentSessionDurationText").Text);
                Assert.Equal("No session persisted", FindByAutomationId<TextBlock>(panel, "LastPersistedSessionText").Text);
                Assert.Equal("No poll yet", FindByAutomationId<TextBlock>(panel, "LastPollTimeText").Text);
                Assert.Equal("No DB write yet", FindByAutomationId<TextBlock>(panel, "LastDbWriteTimeText").Text);
                Assert.Equal(
                    "Sync is off. Data stays on this Windows device.",
                    FindByAutomationId<TextBlock>(panel, "LastSyncStatusText").Text);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void DashboardView_HostsSummaryCardsPanelAndPreservesSummaryCardContent()
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

                SummaryCardsPanel panel = FindByAutomationId<SummaryCardsPanel>(window, "SummaryCardsContainer");
                IReadOnlySet<string> panelText = CollectText(panel);

                Assert.Contains("Active Focus", panelText);
                Assert.Contains("20m", panelText);
                Assert.Contains("Today's focused foreground time", panelText);
                Assert.Contains("Foreground", panelText);
                Assert.Contains("30m", panelText);
                Assert.Contains("Today's foreground time", panelText);
                Assert.Contains("Idle", panelText);
                Assert.Contains("10m", panelText);
                Assert.Contains("Today's idle foreground time", panelText);
                Assert.Contains("Web Focus", panelText);
                Assert.Contains("Today's browser domain time", panelText);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void MetricCard_RendersLabelValueAndSubtitle()
        => RunOnStaThread(() =>
        {
            var card = new MetricCard
            {
                Label = "Active Focus",
                Value = "3h 12m",
                Subtitle = "Today's focused foreground time"
            };
            var window = new Window { Content = card };

            try
            {
                window.Show();
                window.UpdateLayout();

                IReadOnlySet<string> cardText = CollectText(card);
                Assert.Contains("Active Focus", cardText);
                Assert.Contains("3h 12m", cardText);
                Assert.Contains("Today's focused foreground time", cardText);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void MainWindow_TrackingButtonsUpdateVisibleStatus()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                Invoke(FindByAutomationId<Button>(window, "StartTrackingButton"));
                window.UpdateLayout();
                Assert.Equal("Running", FindByAutomationId<TextBlock>(window, "TrackingStatusText").Text);

                Invoke(FindByAutomationId<Button>(window, "StopTrackingButton"));
                window.UpdateLayout();
                Assert.Equal("Stopped", FindByAutomationId<TextBlock>(window, "TrackingStatusText").Text);

                Invoke(FindByAutomationId<Button>(window, "SyncNowButton"));
                window.UpdateLayout();
                Assert.Equal("Sync skipped. Enable sync to upload.", FindByAutomationId<TextBlock>(window, "LastSyncStatusText").Text);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void MainWindow_RefreshButtonRendersSummaryCardsAndChartSurface()
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

                Assert.Contains("Woong Monitor Stack", CollectText(window));
                Assert.Contains("chrome.exe", CollectText(window));
                Assert.Contains("Active Focus", CollectText(window));
                Assert.Contains("Foreground", CollectText(window));
                Assert.Contains("20m", CollectText(window));
                Assert.Contains("Idle", CollectText(window));
                Assert.Contains("10m", CollectText(window));
                Assert.Contains("Web Focus", CollectText(window));
                Assert.Contains("시간대별 Active Focus", CollectText(window));
                Assert.Contains("앱별 집중 시간", CollectText(window));
                Assert.Contains("도메인별 집중 시간", CollectText(window));

                Assert.NotNull(FindByAutomationId<FrameworkElement>(window, "ChartArea"));
                Assert.NotNull(FindByAutomationId<CartesianChart>(window, "HourlyActivityChart"));
                Assert.NotNull(FindByAutomationId<CartesianChart>(window, "AppUsageChart"));
                Assert.NotNull(FindByAutomationId<PieChart>(window, "DomainUsageChart"));
                Assert.True(FindVisualDescendants<CartesianChart>(window).Distinct().Count() >= 2);
                Assert.True(FindVisualDescendants<PieChart>(window).Distinct().Any());
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void MainWindow_WithEmptyData_ShowsReadableChartEmptyStates()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateEmptyDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                Invoke(FindByAutomationId<Button>(window, "RefreshButton"));
                window.UpdateLayout();

                Assert.Equal(
                    "No data for selected period",
                    FindByAutomationId<TextBlock>(window, "HourlyActivityEmptyStateText").Text);
                Assert.Equal(
                    "No data for selected period",
                    FindByAutomationId<TextBlock>(window, "AppUsageEmptyStateText").Text);
                Assert.Equal(
                    "No data for selected period",
                    FindByAutomationId<TextBlock>(window, "DomainUsageEmptyStateText").Text);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void MainWindow_PeriodButtonsSelectExpectedDashboardRanges()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                Invoke(FindByAutomationId<Button>(window, "LastHourPeriodButton"));
                Assert.Equal(DashboardPeriod.LastHour, dashboard.ViewModel.SelectedPeriod);
                Assert.Equal(dashboard.Now.AddHours(-1), dashboard.DataSource.LastFocusQueryStartedAtUtc);
                Assert.Equal(dashboard.Now, dashboard.DataSource.LastFocusQueryEndedAtUtc);

                Invoke(FindByAutomationId<Button>(window, "Last6HoursPeriodButton"));
                Assert.Equal(DashboardPeriod.Last6Hours, dashboard.ViewModel.SelectedPeriod);
                Assert.Equal(dashboard.Now.AddHours(-6), dashboard.DataSource.LastFocusQueryStartedAtUtc);

                Invoke(FindByAutomationId<Button>(window, "Last24HoursPeriodButton"));
                Assert.Equal(DashboardPeriod.Last24Hours, dashboard.ViewModel.SelectedPeriod);
                Assert.Equal(dashboard.Now.AddHours(-24), dashboard.DataSource.LastFocusQueryStartedAtUtc);

                Invoke(FindByAutomationId<Button>(window, "TodayPeriodButton"));
                Assert.Equal(DashboardPeriod.Today, dashboard.ViewModel.SelectedPeriod);
                Assert.Equal(new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero), dashboard.DataSource.LastFocusQueryStartedAtUtc);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void MainWindow_TabsExposeExpectedListsAndSettingsControls()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                Invoke(FindByAutomationId<Button>(window, "RefreshButton"));
                window.UpdateLayout();

                TabControl tabs = FindByAutomationId<TabControl>(window, "DashboardTabs");
                Assert.Equal(["App Sessions", "Web Sessions", "Live Event Log", "Settings"], TabHeaders(tabs));

                tabs.SelectedIndex = 0;
                window.UpdateLayout();
                DataGrid appSessions = FindByAutomationId<DataGrid>(window, "RecentAppSessionsList");
                Assert.Equal(["App", "Process", "Start", "End", "Duration", "State", "Window", "Source"], ColumnHeaders(appSessions));
                AssertColumnMinWidths(appSessions, [160, 180, 90, 90, 100, 80, 260, 100]);
                Assert.Same(dashboard.ViewModel.RecentSessions, appSessions.ItemsSource);

                tabs.SelectedIndex = 1;
                window.UpdateLayout();
                DataGrid webSessions = FindByAutomationId<DataGrid>(window, "RecentWebSessionsList");
                Assert.Equal(["Domain", "Title", "URL Mode", "Start", "End", "Duration", "Browser", "Confidence"], ColumnHeaders(webSessions));
                AssertColumnMinWidths(webSessions, [180, 260, 120, 90, 90, 100, 120, 100]);
                Assert.Same(dashboard.ViewModel.RecentWebSessions, webSessions.ItemsSource);

                tabs.SelectedIndex = 2;
                window.UpdateLayout();
                DataGrid liveEvents = FindByAutomationId<DataGrid>(window, "LiveEventsList");
                Assert.Equal(["Time", "Event Type", "App", "Domain", "Message"], ColumnHeaders(liveEvents));
                Assert.Same(dashboard.ViewModel.LiveEvents, liveEvents.ItemsSource);

                tabs.SelectedIndex = 3;
                window.UpdateLayout();
                CheckBox collectionVisible = FindByAutomationId<CheckBox>(window, "CollectionVisibleCheckBox");
                CheckBox windowTitleVisible = FindByAutomationId<CheckBox>(window, "WindowTitleVisibleCheckBox");
                CheckBox syncEnabled = FindByAutomationId<CheckBox>(window, "SyncEnabledCheckBox");
                TextBlock syncMode = FindByAutomationId<TextBlock>(window, "SyncModeLabel");
                TextBlock syncStatus = FindByAutomationId<TextBlock>(window, "SyncStatusLabel");
                TextBlock browserUrlPrivacy = FindByAutomationId<TextBlock>(window, "BrowserUrlPrivacyText");

                Assert.Equal("Collection visible", collectionVisible.Content);
                Assert.True(collectionVisible.IsChecked);
                Assert.Equal("Capture window title", windowTitleVisible.Content);
                Assert.False(windowTitleVisible.IsChecked);
                Assert.Equal("Sync enabled", syncEnabled.Content);
                Assert.False(syncEnabled.IsChecked);
                Assert.Equal(
                    "Browser URL storage is domain-only by default. Full URLs require explicit future opt-in.",
                    browserUrlPrivacy.Text);
                Assert.Equal("Local only", syncMode.Text);
                Assert.Equal("Sync is off. Data stays on this Windows device.", syncStatus.Text);
            }
            finally
            {
                window.Close();
            }
        });

    private static TestDashboard CreateDashboard()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource(
            [
                FocusSession.FromUtc(
                    "focus-1",
                    "windows-device-1",
                    "chrome.exe",
                    now.AddMinutes(-30),
                    now.AddMinutes(-10),
                    "Asia/Seoul",
                    isIdle: false,
                    "foreground_window"),
                FocusSession.FromUtc(
                    "focus-2",
                    "windows-device-1",
                    "devenv.exe",
                    now.AddMinutes(-10),
                    now,
                    "Asia/Seoul",
                    isIdle: true,
                    "foreground_window")
            ],
            [
                WebSession.FromUtc(
                    "focus-1",
                    "Chrome",
                    "https://example.com/docs",
                    "Docs",
                    now.AddMinutes(-25),
                    now.AddMinutes(-15))
            ]);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        return new TestDashboard(now, dataSource, viewModel);
    }

    private static TestDashboard CreateEmptyDashboard()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource([], []);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        return new TestDashboard(now, dataSource, viewModel);
    }

    private static void AssertPeriodButton(
        DependencyObject root,
        string automationId,
        string expectedContent,
        DashboardPeriod expectedPeriod,
        DashboardViewModel viewModel)
    {
        Button button = FindByAutomationId<Button>(root, automationId);

        Assert.Equal(expectedContent, button.Content);
        Assert.Same(viewModel.SelectDashboardPeriodCommand, button.Command);
        Assert.Equal(expectedPeriod, button.CommandParameter);
    }

    private static void AssertReadableButton(Button button)
    {
        Assert.True(button.MinHeight >= 40, $"{AutomationProperties.GetAutomationId(button)} should have MinHeight >= 40.");
        Assert.True(button.MinWidth >= 96, $"{AutomationProperties.GetAutomationId(button)} should have MinWidth >= 96.");
        Assert.True(button.Padding.Left >= 12, $"{AutomationProperties.GetAutomationId(button)} should have horizontal padding >= 12.");
        Assert.True(button.Padding.Right >= 12, $"{AutomationProperties.GetAutomationId(button)} should have horizontal padding >= 12.");
    }

    private static void AssertColumnMinWidths(DataGrid dataGrid, IReadOnlyList<double> expectedMinWidths)
    {
        Assert.Equal(expectedMinWidths.Count, dataGrid.Columns.Count);

        for (int index = 0; index < expectedMinWidths.Count; index++)
        {
            Assert.True(
                dataGrid.Columns[index].MinWidth >= expectedMinWidths[index],
                $"{dataGrid.Columns[index].Header} MinWidth should be >= {expectedMinWidths[index]}.");
        }
    }

    private static void Invoke(Button button)
    {
        if (button.Command is not null)
        {
            Assert.True(button.Command.CanExecute(button.CommandParameter));
            button.Command.Execute(button.CommandParameter);

            return;
        }

        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
    }

    private static IReadOnlyList<string> TabHeaders(TabControl tabs)
        => tabs.Items
            .OfType<TabItem>()
            .Select(tab => tab.Header?.ToString() ?? "")
            .ToList();

    private static IReadOnlyList<string> ColumnHeaders(DataGrid dataGrid)
        => dataGrid.Columns
            .Select(column => column.Header?.ToString() ?? "")
            .ToList();

    private static IReadOnlySet<string> CollectText(DependencyObject root)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);
        CollectText(root, values);

        return values;
    }

    private static void CollectText(DependencyObject root, ISet<string> values)
    {
        if (root is TextBlock { Text.Length: > 0 } textBlock)
        {
            values.Add(textBlock.Text);
        }

        if (root is ContentControl { Content: string content } && !string.IsNullOrWhiteSpace(content))
        {
            values.Add(content);
        }

        foreach (DependencyObject child in GetChildren(root))
        {
            CollectText(child, values);
        }
    }

    private static T FindByAutomationId<T>(DependencyObject root, string automationId)
        where T : DependencyObject
    {
        if (root is T candidate && AutomationProperties.GetAutomationId(root) == automationId)
        {
            return candidate;
        }

        foreach (DependencyObject child in GetChildren(root))
        {
            try
            {
                return FindByAutomationId<T>(child, automationId);
            }
            catch (InvalidOperationException)
            {
            }
        }

        throw new InvalidOperationException($"Could not find {typeof(T).Name} with AutomationId '{automationId}'.");
    }

    private static IReadOnlyList<T> FindVisualDescendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        var descendants = new List<T>();
        CollectVisualDescendants(root, descendants);

        return descendants;
    }

    private static void CollectVisualDescendants<T>(DependencyObject root, ICollection<T> descendants)
        where T : DependencyObject
    {
        if (root is T current)
        {
            descendants.Add(current);
        }

        foreach (DependencyObject child in GetChildren(root))
        {
            CollectVisualDescendants(child, descendants);
        }
    }

    private static IEnumerable<DependencyObject> GetChildren(DependencyObject root)
    {
        int visualChildCount = 0;
        try
        {
            visualChildCount = VisualTreeHelper.GetChildrenCount(root);
        }
        catch (InvalidOperationException)
        {
        }

        for (var index = 0; index < visualChildCount; index++)
        {
            yield return VisualTreeHelper.GetChild(root, index);
        }

        foreach (object child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is DependencyObject dependencyObject)
            {
                yield return dependencyObject;
            }
        }
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

    private sealed record TestDashboard(
        DateTimeOffset Now,
        FakeDashboardDataSource DataSource,
        DashboardViewModel ViewModel);

    private sealed class FakeDashboardDataSource(
        IReadOnlyList<FocusSession> focusSessions,
        IReadOnlyList<WebSession> webSessions) : IDashboardDataSource
    {
        public DateTimeOffset LastFocusQueryStartedAtUtc { get; private set; }

        public DateTimeOffset LastFocusQueryEndedAtUtc { get; private set; }

        public IReadOnlyList<FocusSession> QueryFocusSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
        {
            LastFocusQueryStartedAtUtc = startedAtUtc;
            LastFocusQueryEndedAtUtc = endedAtUtc;

            return focusSessions;
        }

        public IReadOnlyList<WebSession> QueryWebSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => webSessions;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IDashboardClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
