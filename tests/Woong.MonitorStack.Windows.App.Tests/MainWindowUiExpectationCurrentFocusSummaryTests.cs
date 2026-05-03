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
                AssertMutedTextStyle(FindTextBlock(panel, "Last DB write time"));
                AssertMutedTextStyle(FindTextBlock(panel, "Sync state"));
                AssertBodyTextStyle(FindByAutomationId<TextBlock>(panel, "LastDbWriteTimeText"));
                AssertBodyTextStyle(FindByAutomationId<TextBlock>(panel, "LastSyncStatusText"));
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
}
