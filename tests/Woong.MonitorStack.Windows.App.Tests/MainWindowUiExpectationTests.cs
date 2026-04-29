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
                Assert.NotNull(FindByAutomationId<StatusBadge>(header, "TrackingStatusBadge"));
                Assert.NotNull(FindByAutomationId<StatusBadge>(header, "SyncStatusBadge"));
                Assert.NotNull(FindByAutomationId<StatusBadge>(header, "PrivacyStatusBadge"));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void HeaderStatusBar_UsesSharedBadgeColorResources()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                HeaderStatusBar header = FindByAutomationId<HeaderStatusBar>(window, "HeaderArea");
                AssertBadgeUsesBrushes(
                    header,
                    "TrackingStatusBadge",
                    "TrackingBadgeBackgroundBrush",
                    "TrackingBadgeBorderBrush",
                    "TrackingBadgeTextBrush");
                AssertBadgeUsesBrushes(
                    header,
                    "SyncStatusBadge",
                    "SyncBadgeBackgroundBrush",
                    "SyncBadgeBorderBrush",
                    "SyncBadgeTextBrush");
                AssertBadgeUsesBrushes(
                    header,
                    "PrivacyStatusBadge",
                    "PrivacyBadgeBackgroundBrush",
                    "PrivacyBadgeBorderBrush",
                    "PrivacyBadgeTextBrush");
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void StatusBadge_RendersTextAndPreservesAutomationId()
        => RunOnStaThread(() =>
        {
            var badge = new StatusBadge
            {
                Text = "Tracking Stopped"
            };
            AutomationProperties.SetAutomationId(badge, "TestStatusBadge");
            var window = new Window { Content = badge };

            try
            {
                window.Show();
                window.UpdateLayout();

                StatusBadge renderedBadge = FindByAutomationId<StatusBadge>(window, "TestStatusBadge");
                Assert.Same(badge, renderedBadge);
                Assert.Contains("Tracking Stopped", CollectText(renderedBadge));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void StatusBadge_UsesSharedShapeAndTextStyles()
        => RunOnStaThread(() =>
        {
            var uniqueBackground = new SolidColorBrush(Color.FromRgb(0x11, 0x22, 0x33));
            var badge = new StatusBadge
            {
                Text = "Tracking Running",
                Background = uniqueBackground
            };
            var window = new Window { Content = badge };

            try
            {
                window.Show();
                window.UpdateLayout();

                Border border = Assert.Single(
                    FindVisualDescendants<Border>(badge).Distinct(),
                    candidate => ReferenceEquals(candidate.Background, uniqueBackground)
                        && candidate.Child is TextBlock textBlock
                        && string.Equals(textBlock.Text, "Tracking Running", StringComparison.Ordinal));
                Style borderStyle = Assert.IsType<Style>(border.Style);
                AssertStyleSetter(borderStyle, Border.CornerRadiusProperty, new CornerRadius(16));
                AssertStyleSetter(borderStyle, Border.PaddingProperty, new Thickness(14, 8, 14, 8));
                AssertStyleSetter(borderStyle, Border.BorderThicknessProperty, new Thickness(1));

                TextBlock label = FindTextBlock(badge, "Tracking Running");
                AssertStatusBadgeTextStyle(label);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void DetailRow_RendersLabelAndValueWithStableValueAutomationId()
        => RunOnStaThread(() =>
        {
            var row = new DetailRow
            {
                Label = "Current app",
                Value = "Chrome",
                ValueAutomationId = "CurrentAppNameText"
            };
            var window = new Window { Content = row };

            try
            {
                window.Show();
                window.UpdateLayout();

                IReadOnlySet<string> text = CollectText(row);
                Assert.Contains("Current app", text);
                Assert.Contains("Chrome", text);
                TextBlock value = FindByAutomationId<TextBlock>(row, "CurrentAppNameText");
                Assert.Equal("Chrome", value.Text);
                Assert.Equal(TextTrimming.CharacterEllipsis, value.TextTrimming);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SectionCard_RendersContentAndOptionalActionCommand()
        => RunOnStaThread(() =>
        {
            var command = new CountingCommand();
            var card = new SectionCard
            {
                Title = "앱별 집중 시간",
                ActionText = "상세보기",
                ActionCommand = command,
                CardContent = new TextBlock { Text = "Chrome" }
            };
            var window = new Window { Content = card };

            try
            {
                window.Show();
                window.UpdateLayout();

                IReadOnlySet<string> text = CollectText(card);
                Assert.Contains("앱별 집중 시간", text);
                Assert.Contains("상세보기", text);
                Assert.Contains("Chrome", text);

                Button actionButton = FindByAutomationId<Button>(card, "SectionCardActionButton");
                AssertCompactActionButton(actionButton);
                Invoke(actionButton);
                Assert.Equal(1, command.ExecuteCount);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void ButtonStyleDictionary_DefinesReadableDashboardButtonStyles()
        => RunOnStaThread(() =>
        {
            ResourceDictionary resources = LoadStyleResource("Buttons.xaml");

            Assert.True(resources.Contains("DashboardButtonStyle"));
            Assert.True(resources.Contains("PrimaryButtonStyle"));
            Assert.True(resources.Contains("DangerButtonStyle"));
            Assert.True(resources.Contains("SecondaryButtonStyle"));
            Assert.True(resources.Contains("PeriodButtonStyle"));
            Assert.True(resources.Contains("CompactActionButtonStyle"));

            Style primaryButtonStyle = Assert.IsType<Style>(resources["PrimaryButtonStyle"]);
            AssertStyleSetter(primaryButtonStyle, Button.MinWidthProperty, 96.0);
            AssertStyleSetter(primaryButtonStyle, Button.MinHeightProperty, 40.0);
            AssertStyleSetter(primaryButtonStyle, Control.PaddingProperty, new Thickness(12, 0, 12, 0));

            Style compactActionButtonStyle = Assert.IsType<Style>(resources["CompactActionButtonStyle"]);
            AssertStyleSetter(compactActionButtonStyle, Button.MinWidthProperty, 72.0);
            AssertStyleSetter(compactActionButtonStyle, Button.MinHeightProperty, 28.0);
            AssertStyleSetter(compactActionButtonStyle, Control.PaddingProperty, new Thickness(10, 0, 10, 0));
            AssertStyleSetter(compactActionButtonStyle, Control.FontSizeProperty, 12.0);
        });

    [Fact]
    public void CardStyleDictionary_DefinesReusableDashboardCardStyles()
        => RunOnStaThread(() =>
        {
            ResourceDictionary resources = LoadStyleResource("Cards.xaml");

            Assert.True(resources.Contains("DashboardCardBorderStyle"));
            Assert.True(resources.Contains("CompactSurfaceBorderStyle"));

            Style dashboardCardStyle = Assert.IsType<Style>(resources["DashboardCardBorderStyle"]);
            AssertStyleSetter(dashboardCardStyle, Border.CornerRadiusProperty, new CornerRadius(8));
            AssertStyleSetter(dashboardCardStyle, Border.PaddingProperty, new Thickness(18));
            AssertStyleSetter(dashboardCardStyle, Border.BorderThicknessProperty, new Thickness(1));

            Style compactSurfaceStyle = Assert.IsType<Style>(resources["CompactSurfaceBorderStyle"]);
            AssertStyleSetter(compactSurfaceStyle, Border.CornerRadiusProperty, new CornerRadius(8));
            AssertStyleSetter(compactSurfaceStyle, Border.PaddingProperty, new Thickness(12));
            AssertStyleSetter(compactSurfaceStyle, Border.BorderThicknessProperty, new Thickness(1));
        });

    [Fact]
    public void ColorStyleDictionary_DefinesCoreDashboardBrushes()
        => RunOnStaThread(() =>
        {
            ResourceDictionary resources = LoadStyleResource("Colors.xaml");

            AssertSolidBrush(resources, "AppBackgroundBrush", Color.FromRgb(0xF6, 0xF8, 0xFB));
            AssertSolidBrush(resources, "SurfaceBrush", Colors.White);
            AssertSolidBrush(resources, "BorderBrush", Color.FromRgb(0xDC, 0xE1, 0xE8));
            AssertSolidBrush(resources, "TextPrimaryBrush", Color.FromRgb(0x16, 0x20, 0x33));
            AssertSolidBrush(resources, "TextMutedBrush", Color.FromRgb(0x5A, 0x64, 0x72));
        });

    [Fact]
    public void MainWindow_UsesSharedBackgroundBrush()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                object backgroundBrush = window.FindResource("AppBackgroundBrush");
                Assert.Same(backgroundBrush, window.Background);
                var root = Assert.IsType<Grid>(window.Content);
                Assert.Same(backgroundBrush, root.Background);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void TypographyStyleDictionary_DefinesDashboardTextStyles()
        => RunOnStaThread(() =>
        {
            ResourceDictionary resources = LoadStyleResource("Typography.xaml");

            Assert.True(resources.Contains("HeadingTextStyle"));
            Assert.True(resources.Contains("SubtitleTextStyle"));
            Assert.True(resources.Contains("SectionTitleTextStyle"));
            Assert.True(resources.Contains("BodyTextStyle"));
            Assert.True(resources.Contains("MutedTextStyle"));

            Style headingStyle = Assert.IsType<Style>(resources["HeadingTextStyle"]);
            AssertStyleSetter(headingStyle, TextBlock.FontSizeProperty, 24.0);
            AssertStyleSetter(headingStyle, TextBlock.FontWeightProperty, FontWeights.SemiBold);

            Style sectionTitleStyle = Assert.IsType<Style>(resources["SectionTitleTextStyle"]);
            AssertStyleSetter(sectionTitleStyle, TextBlock.FontSizeProperty, 16.0);
            AssertStyleSetter(sectionTitleStyle, TextBlock.FontWeightProperty, FontWeights.SemiBold);

            Style mutedStyle = Assert.IsType<Style>(resources["MutedTextStyle"]);
            AssertStyleSetter(mutedStyle, TextBlock.FontSizeProperty, 12.0);
        });

    [Fact]
    public void DataGridStyleDictionary_DefinesReadableSessionGridStyle()
        => RunOnStaThread(() =>
        {
            ResourceDictionary resources = LoadStyleResource("DataGrid.xaml");

            Assert.True(resources.Contains("SessionDataGridStyle"));

            Style sessionGridStyle = Assert.IsType<Style>(resources["SessionDataGridStyle"]);
            AssertStyleSetter(sessionGridStyle, DataGrid.AutoGenerateColumnsProperty, false);
            AssertStyleSetter(sessionGridStyle, DataGrid.CanUserAddRowsProperty, false);
            AssertStyleSetter(sessionGridStyle, DataGrid.CanUserDeleteRowsProperty, false);
            AssertStyleSetter(sessionGridStyle, DataGrid.HeadersVisibilityProperty, DataGridHeadersVisibility.Column);
            AssertStyleSetter(sessionGridStyle, DataGrid.IsReadOnlyProperty, true);
            AssertStyleSetter(sessionGridStyle, FrameworkElement.MinHeightProperty, 260.0);
            AssertStyleSetter(
                sessionGridStyle,
                ScrollViewer.HorizontalScrollBarVisibilityProperty,
                ScrollBarVisibility.Auto);
        });

    [Fact]
    public void TabsStyleDictionary_DefinesReadableDashboardTabsStyle()
        => RunOnStaThread(() =>
        {
            ResourceDictionary resources = LoadStyleResource("Tabs.xaml");

            Assert.True(resources.Contains("DashboardTabControlStyle"));
            Assert.True(resources.Contains("DashboardTabItemStyle"));

            Style tabItemStyle = Assert.IsType<Style>(resources["DashboardTabItemStyle"]);
            AssertStyleSetter(tabItemStyle, FrameworkElement.MinHeightProperty, 36.0);
            AssertStyleSetter(tabItemStyle, Control.PaddingProperty, new Thickness(14, 8, 14, 8));
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
                    "No browser domain yet. Connect browser capture; app focus is tracked.",
                    FindByAutomationId<TextBlock>(panel, "CurrentBrowserDomainText").Text);
                Assert.Equal(
                    "Browser capture unavailable",
                    FindByAutomationId<TextBlock>(panel, "BrowserCaptureStatusText").Text);
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
    public void CurrentFocusPanel_UsesSharedTypographyForTitleAndPersistenceStatus()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                CurrentFocusPanel panel = FindByAutomationId<CurrentFocusPanel>(window, "CurrentActivityPanel");

                Assert.IsType<Style>(panel.FindResource("CurrentFocusValueTextStyle"));
                Assert.IsType<Style>(panel.FindResource("CurrentFocusSecondaryTextStyle"));
                AssertSectionTitleStyle(FindTextBlock(panel, "Current Focus"));
                AssertMutedTextStyle(FindTextBlock(panel, "Last DB write / Sync state"));
                AssertCurrentFocusValueTextStyle(FindByAutomationId<TextBlock>(panel, "LastDbWriteTimeText"));
                AssertCurrentFocusSecondaryTextStyle(FindByAutomationId<TextBlock>(panel, "LastSyncStatusText"));
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
    public void MetricCard_UsesSharedLabelTypography()
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

                TextBlock label = FindTextBlock(card, "Active Focus");

                AssertMetricLabelTextStyle(label);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void DashboardView_HostsChartsPanelAndPreservesChartContent()
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

                ChartsPanel panel = FindByAutomationId<ChartsPanel>(window, "ChartArea");
                IReadOnlySet<string> panelText = CollectText(panel);

                Assert.Contains("시간대별 Active Focus", panelText);
                Assert.Contains("앱별 집중 시간", panelText);
                Assert.Contains("도메인별 집중 시간", panelText);
                Assert.NotNull(FindByAutomationId<CartesianChart>(panel, "HourlyActivityChart"));
                Assert.NotNull(FindByAutomationId<CartesianChart>(panel, "AppUsageChart"));
                Assert.IsType<CartesianChart>(FindByAutomationId<FrameworkElement>(panel, "DomainUsageChart"));
                Assert.NotNull(FindByAutomationId<TextBlock>(panel, "HourlyActivityEmptyStateText"));
                Assert.NotNull(FindByAutomationId<TextBlock>(panel, "AppUsageEmptyStateText"));
                Assert.NotNull(FindByAutomationId<TextBlock>(panel, "DomainUsageEmptyStateText"));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void DashboardView_ChartsPanelUsesSharedSectionTitleTypography()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                ChartsPanel panel = FindByAutomationId<ChartsPanel>(window, "ChartArea");
                Assert.IsType<Style>(panel.FindResource("SectionTitleTextStyle"));

                AssertSectionTitleStyle(FindTextBlock(panel, "시간대별 Active Focus"));
                AssertSectionTitleStyle(FindTextBlock(panel, "앱별 집중 시간"));
                AssertSectionTitleStyle(FindTextBlock(panel, "도메인별 집중 시간"));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void DashboardView_ChartDetailButtonsSelectExpectedDetailsTabs()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                Button appDetails = FindByAutomationId<Button>(window, "AppChartDetailsButton");
                Button domainDetails = FindByAutomationId<Button>(window, "DomainChartDetailsButton");
                TabControl tabs = FindByAutomationId<TabControl>(window, "DashboardTabs");

                Assert.Same(dashboard.ViewModel.ShowAppFocusDetailsCommand, appDetails.Command);
                Assert.Same(dashboard.ViewModel.ShowDomainFocusDetailsCommand, domainDetails.Command);
                AssertCompactActionButton(appDetails);
                AssertCompactActionButton(domainDetails);

                Invoke(domainDetails);
                window.UpdateLayout();
                Assert.Equal(DetailsTab.WebSessions, dashboard.ViewModel.SelectedDetailsTab);
                Assert.Equal(1, tabs.SelectedIndex);

                Invoke(appDetails);
                window.UpdateLayout();
                Assert.Equal(DetailsTab.AppSessions, dashboard.ViewModel.SelectedDetailsTab);
                Assert.Equal(0, tabs.SelectedIndex);
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
    public void SettingsPanel_PreservesPrivacyControlsAndSafeDefaults()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);

                CheckBox collectionVisible = FindByAutomationId<CheckBox>(panel, "CollectionVisibleCheckBox");
                CheckBox windowTitleVisible = FindByAutomationId<CheckBox>(panel, "WindowTitleVisibleCheckBox");
                CheckBox pageTitleCapture = FindByAutomationId<CheckBox>(panel, "PageTitleCaptureCheckBox");
                CheckBox fullUrlCapture = FindByAutomationId<CheckBox>(panel, "FullUrlCaptureCheckBox");
                CheckBox domainOnlyStorage = FindByAutomationId<CheckBox>(panel, "DomainOnlyBrowserStorageCheckBox");
                TextBlock browserUrlPrivacy = FindByAutomationId<TextBlock>(panel, "BrowserUrlPrivacyText");

                Assert.Equal("Collection visible", collectionVisible.Content);
                Assert.True(collectionVisible.IsChecked);
                Assert.Equal("Capture window title", windowTitleVisible.Content);
                Assert.False(windowTitleVisible.IsChecked);
                Assert.Equal("Capture page title", pageTitleCapture.Content);
                Assert.False(pageTitleCapture.IsChecked);
                Assert.Equal("Full URL capture (off)", fullUrlCapture.Content);
                Assert.False(fullUrlCapture.IsEnabled);
                Assert.False(fullUrlCapture.IsChecked);
                Assert.Equal("Domain-only browser storage", domainOnlyStorage.Content);
                Assert.False(domainOnlyStorage.IsEnabled);
                Assert.True(domainOnlyStorage.IsChecked);
                Assert.Equal(
                    "Browser URL storage is domain-only by default. Full URLs require explicit future opt-in.",
                    browserUrlPrivacy.Text);
                Assert.True(dashboard.ViewModel.Settings.IsCollectionVisible);
                Assert.False(dashboard.ViewModel.Settings.IsWindowTitleVisible);
                Assert.False(dashboard.ViewModel.Settings.IsPageTitleCaptureEnabled);
                Assert.False(dashboard.ViewModel.Settings.IsFullUrlCaptureEnabled);
                Assert.True(dashboard.ViewModel.Settings.IsDomainOnlyBrowserStorageEnabled);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SettingsPanel_UsesSharedSectionHeadingTypography()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);

                Assert.IsType<Style>(panel.FindResource("SettingsSectionTitleTextStyle"));
                AssertSettingsSectionTitleStyle(FindTextBlock(panel, "Privacy"));
                AssertSettingsSectionTitleStyle(FindTextBlock(panel, "Sync"));
                AssertSettingsSectionTitleStyle(FindTextBlock(panel, "Runtime"));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SettingsPanel_UsesSharedMutedTextTypography()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);

                Assert.IsType<Style>(panel.FindResource("SettingsMutedTextStyle"));
                AssertSettingsMutedTextStyle(FindByAutomationId<TextBlock>(panel, "BrowserUrlPrivacyText"));
                AssertSettingsMutedTextStyle(FindByAutomationId<TextBlock>(panel, "SyncModeLabel"));
                AssertSettingsMutedTextStyle(FindTextBlock(panel, "Poll interval: 1 second"));
                AssertSettingsMutedTextStyle(FindTextBlock(panel, "Idle threshold: 5 minutes"));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SettingsPanel_UsesSharedWarningTextTypography()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);

                Assert.IsType<SolidColorBrush>(panel.FindResource("WarningTextBrush"));
                Assert.IsType<Style>(panel.FindResource("SettingsWarningTextStyle"));
                AssertSettingsWarningTextStyle(FindByAutomationId<TextBlock>(panel, "SyncStatusLabel"));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SettingsPanel_UsesSharedInputStyle()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);
                TextBox syncEndpoint = FindByAutomationId<TextBox>(panel, "SyncEndpointTextBox");

                Assert.IsType<Style>(panel.FindResource("SettingsInputTextBoxStyle"));
                Style style = Assert.IsType<Style>(syncEndpoint.Style);
                AssertStyleSetter(style, Control.FontSizeProperty, 13.0);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SettingsPanel_UsesSharedCheckBoxStyle()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);

                Assert.IsType<Style>(panel.FindResource("SettingsCheckBoxStyle"));
                AssertSettingsCheckBoxStyle(FindByAutomationId<CheckBox>(panel, "CollectionVisibleCheckBox"));
                AssertSettingsCheckBoxStyle(FindByAutomationId<CheckBox>(panel, "WindowTitleVisibleCheckBox"));
                AssertSettingsCheckBoxStyle(FindByAutomationId<CheckBox>(panel, "PageTitleCaptureCheckBox"));
                AssertSettingsCheckBoxStyle(FindByAutomationId<CheckBox>(panel, "FullUrlCaptureCheckBox"));
                AssertSettingsCheckBoxStyle(FindByAutomationId<CheckBox>(panel, "DomainOnlyBrowserStorageCheckBox"));
                AssertSettingsCheckBoxStyle(FindByAutomationId<CheckBox>(panel, "SyncEnabledCheckBox"));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SettingsPanel_PreservesSyncControlsAndTwoWayBinding()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);

                CheckBox syncEnabled = FindByAutomationId<CheckBox>(panel, "SyncEnabledCheckBox");
                TextBlock syncMode = FindByAutomationId<TextBlock>(panel, "SyncModeLabel");
                TextBlock syncStatus = FindByAutomationId<TextBlock>(panel, "SyncStatusLabel");
                TextBox syncEndpoint = FindByAutomationId<TextBox>(panel, "SyncEndpointTextBox");

                Assert.Equal("Sync enabled", syncEnabled.Content);
                Assert.False(syncEnabled.IsChecked);
                Assert.Equal("Local only", syncMode.Text);
                Assert.Equal("Sync is off. Data stays on this Windows device.", syncStatus.Text);
                Assert.Equal("No sync endpoint configured", syncEndpoint.Text);
                Assert.False(syncEndpoint.IsEnabled);

                syncEnabled.IsChecked = true;
                window.UpdateLayout();

                Assert.True(dashboard.ViewModel.Settings.IsSyncEnabled);
                Assert.Equal("Sync enabled", syncMode.Text);
                Assert.True(syncEndpoint.IsEnabled);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SettingsPanel_PreservesRuntimeAndStorageActions()
        => RunOnStaThread(() =>
        {
            var window = new MainWindow(CreateDashboard().ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);
                IReadOnlySet<string> text = CollectText(panel);

                Assert.Contains("Poll interval: 1 second", text);
                Assert.Contains("Idle threshold: 5 minutes", text);
                Button openDbFolder = FindByAutomationId<Button>(panel, "OpenLocalDbFolderButton");
                Button openLogsFolder = FindByAutomationId<Button>(panel, "OpenLogsFolderButton");
                Button clearLocalData = FindByAutomationId<Button>(panel, "ClearLocalDataButton");
                Assert.False(openDbFolder.IsEnabled);
                Assert.False(openLogsFolder.IsEnabled);
                Assert.Equal("Clear local data (disabled)", clearLocalData.Content);
                Assert.False(clearLocalData.IsEnabled);
                AssertReadableButton(openDbFolder);
                AssertReadableButton(openLogsFolder);
                AssertReadableButton(clearLocalData);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void EmptyState_RendersBoundTextWithTextAutomationId()
        => RunOnStaThread(() =>
        {
            var emptyState = new EmptyState
            {
                Text = "No data for selected period",
                TextAutomationId = "TestEmptyStateText"
            };
            var window = new Window { Content = emptyState };

            try
            {
                window.Show();
                window.UpdateLayout();

                TextBlock text = FindByAutomationId<TextBlock>(emptyState, "TestEmptyStateText");
                Assert.Equal("No data for selected period", text.Text);
                Style style = Assert.IsType<Style>(text.Style);
                AssertStyleSetter(style, TextBlock.FontSizeProperty, 13.0);

                Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
                var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
                Assert.Equal(Color.FromRgb(0x5A, 0x64, 0x72), foregroundBrush.Color);
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
                Assert.Contains("도메인별 집중 시간", CollectText(chartsPanel));
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

    private static void AssertCompactActionButton(Button button)
    {
        Assert.Equal(28.0, button.MinHeight);
        Assert.Equal(72.0, button.MinWidth);
        Assert.Equal(new Thickness(10, 0, 10, 0), button.Padding);
        Assert.Equal(12.0, button.FontSize);
    }

    private static void AssertBadgeUsesBrushes(
        FrameworkElement root,
        string automationId,
        string backgroundBrushKey,
        string borderBrushKey,
        string textBrushKey)
    {
        StatusBadge badge = FindByAutomationId<StatusBadge>(root, automationId);

        Assert.Same(root.FindResource(backgroundBrushKey), badge.Background);
        Assert.Same(root.FindResource(borderBrushKey), badge.BorderBrush);
        Assert.Same(root.FindResource(textBrushKey), badge.TextBrush);
    }

    private static void AssertSectionTitleStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 16.0);
        AssertStyleSetter(style, TextBlock.FontWeightProperty, FontWeights.SemiBold);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x16, 0x20, 0x33), foregroundBrush.Color);
    }

    private static void AssertSettingsSectionTitleStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 15.0);
        AssertStyleSetter(style, TextBlock.FontWeightProperty, FontWeights.SemiBold);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x16, 0x20, 0x33), foregroundBrush.Color);
    }

    private static void AssertSettingsMutedTextStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 13.0);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x5A, 0x64, 0x72), foregroundBrush.Color);
    }

    private static void AssertSettingsWarningTextStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 13.0);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x8A, 0x4B, 0x00), foregroundBrush.Color);
    }

    private static void AssertMutedTextStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 12.0);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x5A, 0x64, 0x72), foregroundBrush.Color);
    }

    private static void AssertBodyTextStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 13.0);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x16, 0x20, 0x33), foregroundBrush.Color);
    }

    private static void AssertCurrentFocusValueTextStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 14.0);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x16, 0x20, 0x33), foregroundBrush.Color);
    }

    private static void AssertCurrentFocusSecondaryTextStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 13.0);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x5A, 0x64, 0x72), foregroundBrush.Color);
    }

    private static void AssertMetricLabelTextStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontWeightProperty, FontWeights.SemiBold);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 13.0);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x16, 0x20, 0x33), foregroundBrush.Color);
    }

    private static void AssertStatusBadgeTextStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontWeightProperty, FontWeights.SemiBold);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 13.0);
    }

    private static void AssertSettingsCheckBoxStyle(CheckBox checkBox)
    {
        Style style = Assert.IsType<Style>(checkBox.Style);
        AssertStyleSetter(style, Control.FontSizeProperty, 14.0);
        AssertStyleSetter(style, FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 10));
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

    private static void AssertSessionDataGridContract(DataGrid dataGrid)
    {
        Assert.NotNull(dataGrid.Style);
        Assert.False(dataGrid.AutoGenerateColumns);
        Assert.False(dataGrid.CanUserAddRows);
        Assert.False(dataGrid.CanUserDeleteRows);
        Assert.Equal(DataGridHeadersVisibility.Column, dataGrid.HeadersVisibility);
        Assert.True(dataGrid.IsReadOnly);
        Assert.True(dataGrid.MinHeight >= 260);
        Assert.Equal(ScrollBarVisibility.Auto, ScrollViewer.GetHorizontalScrollBarVisibility(dataGrid));
    }

    private static SettingsPanel ShowSettingsPanel(Window window)
    {
        window.Show();
        window.UpdateLayout();

        TabControl tabs = FindByAutomationId<TabControl>(window, "DashboardTabs");
        tabs.SelectedIndex = 3;
        window.UpdateLayout();

        return FindByAutomationId<SettingsPanel>(window, "SettingsPanel");
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

    private static ResourceDictionary LoadStyleResource(string fileName)
        => Assert.IsType<ResourceDictionary>(
            Application.LoadComponent(new Uri(
                $"/Woong.MonitorStack.Windows.App;component/Styles/{fileName}",
                UriKind.Relative)));

    private static void AssertStyleSetter<T>(Style style, DependencyProperty property, T expectedValue)
    {
        Setter setter = FindSetter(style, property);
        T actualValue = Assert.IsType<T>(setter.Value);

        Assert.Equal(expectedValue, actualValue);
    }

    private static void AssertSolidBrush(ResourceDictionary resources, string key, Color expectedColor)
    {
        Assert.True(resources.Contains(key), $"Resource dictionary should contain `{key}`.");
        var brush = Assert.IsType<SolidColorBrush>(resources[key]);

        Assert.Equal(expectedColor, brush.Color);
    }

    private static Setter FindSetter(Style style, DependencyProperty property)
    {
        Style? current = style;

        while (current is not null)
        {
            foreach (Setter setter in current.Setters.OfType<Setter>())
            {
                if (setter.Property == property)
                {
                    return setter;
                }
            }

            current = current.BasedOn;
        }

        throw new InvalidOperationException($"Style '{style}' does not set '{property.Name}'.");
    }

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

    private static TextBlock FindTextBlock(DependencyObject root, string text)
    {
        TextBlock? match = FindVisualDescendants<TextBlock>(root)
            .FirstOrDefault(textBlock => string.Equals(textBlock.Text, text, StringComparison.Ordinal));

        return match ?? throw new InvalidOperationException($"Could not find TextBlock with text '{text}'.");
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

    private sealed class CountingCommand : System.Windows.Input.ICommand
    {
        public int ExecuteCount { get; private set; }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
            => true;

        public void Execute(object? parameter)
        {
            ExecuteCount++;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
