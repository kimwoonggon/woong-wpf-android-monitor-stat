using System.Windows;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class WpfDashboardApplicationLifetimeTests
{
    [Fact]
    public void RequestExit_WhenCurrentMainWindowIsMainWindow_DelegatesExplicitExitWithWpfWindow()
        => WpfTestHelpers.RunOnStaThread(() =>
        {
            var mainWindow = new MainWindow(CreateViewModel(), new NoopTrackingTicker());
            var trayLifecycle = new RecordingTrayLifecycleService();
            var lifetime = new WpfDashboardApplicationLifetime(trayLifecycle, () => mainWindow);

            try
            {
                lifetime.RequestExit();

                Assert.Equal(1, trayLifecycle.RequestExplicitExitCallCount);
                Assert.NotNull(trayLifecycle.RequestedWindow);
                Assert.IsType<WpfTrayLifecycleWindow>(trayLifecycle.RequestedWindow);
            }
            finally
            {
                mainWindow.Close();
            }
        });

    [Fact]
    public void RequestExit_WhenCurrentMainWindowIsNotCompatible_DelegatesExplicitExitWithNullWindow()
        => WpfTestHelpers.RunOnStaThread(() =>
        {
            var fallbackWindow = new Window();
            var trayLifecycle = new RecordingTrayLifecycleService();
            var lifetime = new WpfDashboardApplicationLifetime(trayLifecycle, () => fallbackWindow);

            try
            {
                lifetime.RequestExit();

                Assert.Equal(1, trayLifecycle.RequestExplicitExitCallCount);
                Assert.Null(trayLifecycle.RequestedWindow);
            }
            finally
            {
                fallbackWindow.Close();
            }
        });

    private static DashboardViewModel CreateViewModel()
        => new(
            new EmptyDashboardDataSource(),
            new FixedDashboardClock(new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero)),
            new DashboardOptions("Asia/Seoul"));

    private sealed class RecordingTrayLifecycleService : IWindowsTrayLifecycleService
    {
        public bool IsExplicitExitRequested { get; private set; }

        public int RequestExplicitExitCallCount { get; private set; }

        public ITrayLifecycleWindow? RequestedWindow { get; private set; }

        public void RegisterWindow(ITrayLifecycleWindow window)
        {
        }

        public void MinimizeToTaskbar(ITrayLifecycleWindow window)
        {
        }

        public void RestoreFromTray(ITrayLifecycleWindow window)
        {
        }

        public void RequestExplicitExit(ITrayLifecycleWindow? window)
        {
            IsExplicitExitRequested = true;
            RequestExplicitExitCallCount++;
            RequestedWindow = window;
        }
    }

    private sealed class NoopTrackingTicker : ITrackingTicker
    {
        public event EventHandler? Tick
        {
            add { }
            remove { }
        }

        public bool IsRunning { get; private set; }

        public void Start()
            => IsRunning = true;

        public void Stop()
            => IsRunning = false;
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
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
