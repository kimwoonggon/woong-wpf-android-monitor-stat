using System.Windows;
using Woong.MonitorStack.Windows.App.Controls;
using Woong.MonitorStack.Windows.App.Views;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using static Woong.MonitorStack.Windows.App.Tests.WpfTestHelpers;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class HeaderStatusBarAccessibilityTests
{
    [Fact]
    public void HeaderStatusBar_StatusBadgesExposeReadableNamesMatchingStateText()
        => RunOnStaThread(() =>
        {
            var viewModel = new DashboardViewModel(
                new EmptyDashboardDataSource(),
                new FixedDashboardClock(new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero)),
                new DashboardOptions("Asia/Seoul"));
            var header = new HeaderStatusBar { DataContext = viewModel };
            var window = new Window { Content = header };

            try
            {
                window.Show();
                window.UpdateLayout();

                AssertAutomationName<StatusBadge>(header, "TrackingStatusBadge", "Tracking Stopped");
                AssertAutomationName<StatusBadge>(header, "SyncStatusBadge", "Sync Off");
                AssertAutomationName<StatusBadge>(header, "PrivacyStatusBadge", "Privacy Safe");

                viewModel.TrackingBadgeText = "Tracking Running";
                viewModel.SyncBadgeText = "Sync Error";
                viewModel.PrivacyBadgeText = "Privacy Custom";
                window.UpdateLayout();

                AssertAutomationName<StatusBadge>(header, "TrackingStatusBadge", "Tracking Running");
                AssertAutomationName<StatusBadge>(header, "SyncStatusBadge", "Sync Error");
                AssertAutomationName<StatusBadge>(header, "PrivacyStatusBadge", "Privacy Custom");
            }
            finally
            {
                window.Close();
            }
        });

    private sealed class EmptyDashboardDataSource : IDashboardDataSource
    {
        public IReadOnlyList<Woong.MonitorStack.Domain.Common.FocusSession> QueryFocusSessions(
            DateTimeOffset startedAtUtc,
            DateTimeOffset endedAtUtc)
            => [];

        public IReadOnlyList<Woong.MonitorStack.Domain.Common.WebSession> QueryWebSessions(
            DateTimeOffset startedAtUtc,
            DateTimeOffset endedAtUtc)
            => [];
    }

    private sealed class FixedDashboardClock(DateTimeOffset utcNow) : IDashboardClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
