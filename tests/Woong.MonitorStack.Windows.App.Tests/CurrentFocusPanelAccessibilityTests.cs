using System.Windows;
using System.Windows.Controls;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.App.Views;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using static Woong.MonitorStack.Windows.App.Tests.WpfTestHelpers;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class CurrentFocusPanelAccessibilityTests
{
    [Fact]
    public void CurrentFocusPanel_RuntimeStateValuesExposeReadableAutomationNames()
        => RunOnStaThread(() =>
        {
            var panel = new CurrentFocusPanel();
            var window = new Window { Content = panel };

            try
            {
                window.Show();
                window.UpdateLayout();

                AssertAutomationName<TextBlock>(panel, "TrackingStatusText", "Tracking state");
                AssertAutomationName<TextBlock>(panel, "CurrentAppNameText", "Current app");
                AssertAutomationName<TextBlock>(panel, "CurrentProcessNameText", "Current process");
                AssertAutomationName<TextBlock>(panel, "CurrentWindowTitleText", "Current window title");
                AssertAutomationName<TextBlock>(panel, "CurrentBrowserDomainText", "Current browser domain");
                AssertAutomationName<TextBlock>(panel, "CurrentSessionDurationText", "Current session duration");
                AssertAutomationName<TextBlock>(panel, "LastPersistedSessionText", "Last persisted session");
                AssertAutomationName<TextBlock>(panel, "LastPollTimeText", "Last poll time");
                AssertAutomationName<TextBlock>(panel, "BrowserCaptureStatusText", "Browser capture");
                AssertAutomationName<TextBlock>(panel, "LastDbWriteTimeText", "Last DB write time");
                AssertAutomationName<TextBlock>(panel, "LastSyncStatusText", "Sync state");
                Assert.Equal("◷", FindByAutomationId<TextBlock>(panel, "TrackingStatusIcon").Text);
                Assert.Equal("▣", FindByAutomationId<TextBlock>(panel, "CurrentAppNameIcon").Text);
                Assert.Equal("□", FindByAutomationId<TextBlock>(panel, "CurrentProcessNameIcon").Text);
                Assert.Equal("▭", FindByAutomationId<TextBlock>(panel, "CurrentWindowTitleIcon").Text);
                Assert.Equal("◎", FindByAutomationId<TextBlock>(panel, "CurrentBrowserDomainIcon").Text);
                Assert.Equal("◴", FindByAutomationId<TextBlock>(panel, "CurrentSessionDurationIcon").Text);
                Assert.Equal("▤", FindByAutomationId<TextBlock>(panel, "LastPersistedSessionIcon").Text);
                Assert.Equal("◷", FindByAutomationId<TextBlock>(panel, "LastPollTimeIcon").Text);
                Assert.Equal("◎", FindByAutomationId<TextBlock>(panel, "BrowserCaptureStatusIcon").Text);
                Assert.Equal("▥", FindByAutomationId<TextBlock>(panel, "LastDbWriteTimeIcon").Text);
                Assert.Equal("⇅", FindByAutomationId<TextBlock>(panel, "LastSyncStatusIcon").Text);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void MainWindow_TrackingRuntimeStateRemainsSelectableAcrossStartSyncAndStop()
        => RunOnStaThread(() =>
        {
            var now = new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero);
            var coordinator = new FakeTrackingCoordinator(now);
            var viewModel = new DashboardViewModel(
                new EmptyDashboardDataSource(),
                new FixedDashboardClock(now),
                new DashboardOptions("Asia/Seoul"),
                coordinator);
            var window = new MainWindow(viewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                AssertAutomationName<Button>(window, "StartTrackingButton", "Start tracking");
                AssertAutomationName<Button>(window, "StopTrackingButton", "Stop tracking");
                AssertAutomationName<Button>(window, "SyncNowButton", "Sync now");
                AssertAutomationName<TextBlock>(window, "TrackingStatusText", "Tracking state");
                Assert.Equal("Stopped", FindByAutomationId<TextBlock>(window, "TrackingStatusText").Text);

                ExecuteCommand(FindByAutomationId<Button>(window, "StartTrackingButton"));
                window.UpdateLayout();

                Assert.Equal("Running", FindByAutomationId<TextBlock>(window, "TrackingStatusText").Text);
                Assert.Equal("Code.exe", FindByAutomationId<TextBlock>(window, "CurrentAppNameText").Text);
                Assert.Equal("Code.exe", FindByAutomationId<TextBlock>(window, "CurrentProcessNameText").Text);
                Assert.Equal("github.com", FindByAutomationId<TextBlock>(window, "CurrentBrowserDomainText").Text);
                AssertAutomationName<TextBlock>(window, "LastSyncStatusText", "Sync state");
                Assert.Equal(
                    "Sync skipped. Enable sync to upload.",
                    FindByAutomationId<TextBlock>(window, "LastSyncStatusText").Text);

                ExecuteCommand(FindByAutomationId<Button>(window, "SyncNowButton"));
                window.UpdateLayout();

                Assert.Equal(2, coordinator.SyncAttempts);
                Assert.Equal(
                    "Sync skipped. Enable sync to upload.",
                    FindByAutomationId<TextBlock>(window, "LastSyncStatusText").Text);

                ExecuteCommand(FindByAutomationId<Button>(window, "StopTrackingButton"));
                window.UpdateLayout();

                Assert.Equal("Stopped", FindByAutomationId<TextBlock>(window, "TrackingStatusText").Text);
                Assert.Contains("persisted", FindByAutomationId<TextBlock>(window, "LastPersistedSessionText").Text);
                Assert.Equal("Code.exe", FindByAutomationId<TextBlock>(window, "CurrentAppNameText").Text);
            }
            finally
            {
                window.Close();
            }
        });

    private static void ExecuteCommand(Button button)
    {
        Assert.NotNull(button.Command);
        Assert.True(button.Command.CanExecute(button.CommandParameter));
        button.Command.Execute(button.CommandParameter);
    }

    private sealed class EmptyDashboardDataSource : IDashboardDataSource
    {
        public IReadOnlyList<FocusSession> QueryFocusSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => [];

        public IReadOnlyList<WebSession> QueryWebSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => [];
    }

    private sealed class FixedDashboardClock(DateTimeOffset utcNow) : IDashboardClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private sealed class FakeTrackingCoordinator(DateTimeOffset now) : IDashboardTrackingCoordinator
    {
        public int SyncAttempts { get; private set; }

        public DashboardTrackingSnapshot StartTracking()
            => new(
                AppName: "Code.exe",
                ProcessName: "Code.exe",
                WindowTitle: "Project - Visual Studio Code",
                CurrentSessionDuration: TimeSpan.Zero,
                LastPersistedSession: null,
                CurrentBrowserDomain: "github.com",
                DashboardBrowserCaptureStatus.ExtensionConnected,
                LastPollAtUtc: now);

        public DashboardTrackingSnapshot StopTracking()
            => new(
                AppName: "Code.exe",
                ProcessName: "Code.exe",
                WindowTitle: "Project - Visual Studio Code",
                CurrentSessionDuration: TimeSpan.FromMinutes(5),
                LastPersistedSession: new DashboardPersistedSessionSnapshot(
                    "Code.exe",
                    "Code.exe",
                    now.AddMinutes(5),
                    TimeSpan.FromMinutes(5)),
                CurrentBrowserDomain: "github.com",
                DashboardBrowserCaptureStatus.ExtensionConnected,
                LastPollAtUtc: now.AddMinutes(5),
                LastDbWriteAtUtc: now.AddMinutes(5));

        public DashboardTrackingSnapshot PollOnce()
            => StartTracking();

        public DashboardSyncResult SyncNow(bool syncEnabled)
        {
            SyncAttempts++;

            return new DashboardSyncResult("Sync skipped. Enable sync to upload.");
        }
    }
}
