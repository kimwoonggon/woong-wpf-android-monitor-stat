using LiveChartsCore.SkiaSharpView.WPF;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Woong.MonitorStack.Domain.Common;
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
                Assert.True(window.MinWidth >= 860);
                Assert.True(window.MinHeight >= 560);
                Assert.Same(dashboard.ViewModel, window.DataContext);

                Button refreshButton = FindByAutomationId<Button>(window, "RefreshButton");
                Assert.Equal("Refresh", refreshButton.Content);
                Assert.Same(dashboard.ViewModel.RefreshDashboardCommand, refreshButton.Command);

                AssertPeriodButton(window, "TodayPeriodButton", "Today", DashboardPeriod.Today, dashboard.ViewModel);
                AssertPeriodButton(window, "LastHourPeriodButton", "1h", DashboardPeriod.LastHour, dashboard.ViewModel);
                AssertPeriodButton(window, "Last6HoursPeriodButton", "6h", DashboardPeriod.Last6Hours, dashboard.ViewModel);
                AssertPeriodButton(window, "Last24HoursPeriodButton", "24h", DashboardPeriod.Last24Hours, dashboard.ViewModel);
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
                Assert.Contains("Active", CollectText(window));
                Assert.Contains("20m", CollectText(window));
                Assert.Contains("Idle", CollectText(window));
                Assert.Contains("10m", CollectText(window));
                Assert.Contains("Web", CollectText(window));
                Assert.Contains("Activity", CollectText(window));
                Assert.Contains("Apps", CollectText(window));
                Assert.Contains("Domains", CollectText(window));

                Assert.NotNull(FindByAutomationId<ContentControl>(window, "ChartArea"));
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
                Assert.Equal(["App", "Started", "Duration", "Idle"], ColumnHeaders(appSessions));
                Assert.Same(dashboard.ViewModel.RecentSessions, appSessions.ItemsSource);

                tabs.SelectedIndex = 1;
                window.UpdateLayout();
                DataGrid webSessions = FindByAutomationId<DataGrid>(window, "RecentWebSessionsList");
                Assert.Equal(["Domain", "Page", "Started", "Duration"], ColumnHeaders(webSessions));
                Assert.Same(dashboard.ViewModel.RecentWebSessions, webSessions.ItemsSource);

                tabs.SelectedIndex = 2;
                window.UpdateLayout();
                DataGrid liveEvents = FindByAutomationId<DataGrid>(window, "LiveEventsList");
                Assert.Equal(["Time", "Kind", "Message"], ColumnHeaders(liveEvents));
                Assert.Same(dashboard.ViewModel.LiveEvents, liveEvents.ItemsSource);

                tabs.SelectedIndex = 3;
                window.UpdateLayout();
                CheckBox collectionVisible = FindByAutomationId<CheckBox>(window, "CollectionVisibleCheckBox");
                CheckBox syncEnabled = FindByAutomationId<CheckBox>(window, "SyncEnabledCheckBox");
                TextBlock syncMode = FindByAutomationId<TextBlock>(window, "SyncModeLabel");
                TextBlock syncStatus = FindByAutomationId<TextBlock>(window, "SyncStatusLabel");

                Assert.Equal("Collection visible", collectionVisible.Content);
                Assert.True(collectionVisible.IsChecked);
                Assert.Equal("Sync enabled", syncEnabled.Content);
                Assert.False(syncEnabled.IsChecked);
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
