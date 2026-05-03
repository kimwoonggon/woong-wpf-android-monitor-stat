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
                Assert.Equal(WindowState.Maximized, window.WindowState);
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
                Assert.Equal("↻ Refresh", refreshButton.Content);
                Assert.Same(dashboard.ViewModel.RefreshDashboardCommand, refreshButton.Command);

                AssertPeriodButton(window, "TodayPeriodButton", "Today", DashboardPeriod.Today, dashboard.ViewModel);
                AssertPeriodButton(window, "LastHourPeriodButton", "1h", DashboardPeriod.LastHour, dashboard.ViewModel);
                AssertPeriodButton(window, "Last6HoursPeriodButton", "6h", DashboardPeriod.Last6Hours, dashboard.ViewModel);
                AssertPeriodButton(window, "Last24HoursPeriodButton", "24h", DashboardPeriod.Last24Hours, dashboard.ViewModel);

                Button startTracking = FindByAutomationId<Button>(window, "StartTrackingButton");
                Button stopTracking = FindByAutomationId<Button>(window, "StopTrackingButton");
                Button syncNow = FindByAutomationId<Button>(window, "SyncNowButton");
                Button customPeriod = FindByAutomationId<Button>(window, "CustomPeriodButton");
                Assert.Equal("▶ Start Tracking", startTracking.Content);
                Assert.Equal("■ Stop Tracking", stopTracking.Content);
                Assert.Equal("⇅ Sync Now", syncNow.Content);
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

                Assert.Contains("Woong Monitor Stack", CollectText(FindByAutomationId<HeaderStatusBar>(window, "HeaderArea")));
                Assert.Contains("chrome.exe", CollectText(FindByAutomationId<DetailsTabsPanel>(window, "DetailsTabsPanel")));
                Assert.Contains("Active Focus", CollectText(FindByAutomationId<SummaryCardsPanel>(window, "SummaryCardsContainer")));
                Assert.Contains("Foreground", CollectText(FindByAutomationId<SummaryCardsPanel>(window, "SummaryCardsContainer")));
                Assert.Contains("20m", CollectText(FindByAutomationId<SummaryCardsPanel>(window, "SummaryCardsContainer")));
                Assert.Contains("Idle", CollectText(FindByAutomationId<SummaryCardsPanel>(window, "SummaryCardsContainer")));
                Assert.Contains("10m", CollectText(FindByAutomationId<SummaryCardsPanel>(window, "SummaryCardsContainer")));
                Assert.Contains("Web Focus", CollectText(FindByAutomationId<SummaryCardsPanel>(window, "SummaryCardsContainer")));

                ChartsPanel chartsPanel = FindByAutomationId<ChartsPanel>(window, "ChartArea");
                Assert.Contains("시간대별 Active Focus", CollectText(chartsPanel));
                Assert.Contains("앱별 집중 시간", CollectText(chartsPanel));
                Assert.Contains("도메인별", CollectText(chartsPanel));
                Assert.Contains("집중 시간", CollectText(chartsPanel));
                Assert.NotNull(FindByAutomationId<CartesianChart>(window, "HourlyActivityChart"));
                Assert.NotNull(FindByAutomationId<CartesianChart>(window, "AppUsageChart"));
                Assert.IsType<CartesianChart>(FindByAutomationId<FrameworkElement>(window, "DomainUsageChart"));
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
                Assert.NotNull(tabs.Style);
                Assert.NotNull(tabs.ItemContainerStyle);
                Assert.Equal(["App Sessions", "Web Sessions", "Live Event Log", "Settings"], TabHeaders(tabs));

                tabs.SelectedIndex = 0;
                window.UpdateLayout();
                DataGrid appSessions = FindByAutomationId<DataGrid>(window, "RecentAppSessionsList");
                AssertSessionDataGridContract(appSessions);
                Assert.Equal(["App", "Process", "Start", "End", "Duration", "State", "Window", "Source"], ColumnHeaders(appSessions));
                AssertColumnMinWidths(appSessions, [160, 180, 90, 90, 100, 80, 260, 100]);
                Assert.IsType<DataGridTemplateColumn>(appSessions.Columns[0]);
                Assert.Same(dashboard.ViewModel.VisibleAppSessionRows, appSessions.ItemsSource);

                tabs.SelectedIndex = 1;
                window.UpdateLayout();
                DataGrid webSessions = FindByAutomationId<DataGrid>(window, "RecentWebSessionsList");
                AssertSessionDataGridContract(webSessions);
                Assert.Equal(["Domain", "Title", "URL Mode", "Start", "End", "Duration", "Browser", "Confidence"], ColumnHeaders(webSessions));
                AssertColumnMinWidths(webSessions, [180, 260, 120, 90, 90, 100, 120, 100]);
                Assert.Same(dashboard.ViewModel.VisibleWebSessionRows, webSessions.ItemsSource);

                tabs.SelectedIndex = 2;
                window.UpdateLayout();
                DataGrid liveEvents = FindByAutomationId<DataGrid>(window, "LiveEventsList");
                AssertSessionDataGridContract(liveEvents);
                Assert.Equal(["Time", "Event Type", "App", "Domain", "Message"], ColumnHeaders(liveEvents));
                Assert.Same(dashboard.ViewModel.VisibleLiveEventRows, liveEvents.ItemsSource);
                Assert.NotNull(FindByAutomationId<ComboBox>(window, "DetailsRowsPerPageComboBox"));
                Assert.Same(dashboard.ViewModel.PreviousDetailsPageCommand, FindByAutomationId<Button>(window, "DetailsPreviousPageButton").Command);
                Assert.Same(dashboard.ViewModel.NextDetailsPageCommand, FindByAutomationId<Button>(window, "DetailsNextPageButton").Command);
                Assert.Equal("1 / 1", FindByAutomationId<TextBlock>(window, "DetailsPageStatusText").Text);

                tabs.SelectedIndex = 3;
                window.UpdateLayout();
                CheckBox collectionVisible = FindByAutomationId<CheckBox>(window, "CollectionVisibleCheckBox");
                CheckBox windowTitleVisible = FindByAutomationId<CheckBox>(window, "WindowTitleVisibleCheckBox");
                CheckBox pageTitleCapture = FindByAutomationId<CheckBox>(window, "PageTitleCaptureCheckBox");
                CheckBox domainOnlyStorage = FindByAutomationId<CheckBox>(window, "DomainOnlyBrowserStorageCheckBox");
                CheckBox syncEnabled = FindByAutomationId<CheckBox>(window, "SyncEnabledCheckBox");
                TextBlock syncMode = FindByAutomationId<TextBlock>(window, "SyncModeLabel");
                TextBlock syncStatus = FindByAutomationId<TextBlock>(window, "SyncStatusLabel");
                TextBox syncEndpoint = FindByAutomationId<TextBox>(window, "SyncEndpointTextBox");
                TextBlock browserUrlPrivacy = FindByAutomationId<TextBlock>(window, "BrowserUrlPrivacyText");

                Assert.Equal("Collection visible", collectionVisible.Content);
                Assert.True(collectionVisible.IsChecked);
                Assert.Equal("Capture window title", windowTitleVisible.Content);
                Assert.False(windowTitleVisible.IsChecked);
                Assert.Equal("Capture page title", pageTitleCapture.Content);
                Assert.False(pageTitleCapture.IsChecked);
                Assert.Equal("Domain-only browser storage", domainOnlyStorage.Content);
                Assert.True(domainOnlyStorage.IsChecked);
                Assert.Equal("Sync enabled", syncEnabled.Content);
                Assert.False(syncEnabled.IsChecked);
                Assert.Equal(
                    "Browser URL storage is domain-only by default. Full URLs require explicit future opt-in.",
                    browserUrlPrivacy.Text);
                Assert.Equal("Local only", syncMode.Text);
                Assert.Equal("Sync is off. Data stays on this Windows device.", syncStatus.Text);
                Assert.Equal("No sync endpoint configured", syncEndpoint.Text);
                Assert.False(syncEndpoint.IsEnabled);
            }
            finally
            {
                window.Close();
            }
        });

}
