using System.Windows;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class MainWindowLifecycleCoordinatorTests
{
    [Fact]
    public void LifecycleEvents_PreserveLoadedPollingCloseFlushAndTrayMinimizeBehavior()
    {
        var trackingCoordinator = new RecordingTrackingCoordinator();
        var viewModel = new DashboardViewModel(
            new EmptyDashboardDataSource(),
            new FixedDashboardClock(new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero)),
            new DashboardOptions("Asia/Seoul"),
            trackingCoordinator);
        var ticker = new ManualTrackingTicker();
        var trayLifecycle = new RecordingTrayLifecycleService();
        var trayWindow = new FakeTrayWindow();
        var lifecycleCoordinator = new MainWindowLifecycleCoordinator(
            viewModel,
            new MainWindowStartupOptions(AutoStartTracking: true),
            ticker,
            trayLifecycle,
            trayWindow);

        lifecycleCoordinator.HandleLoaded();
        lifecycleCoordinator.HandleLoaded();
        ticker.RaiseTick();
        lifecycleCoordinator.MinimizeToTaskbar();
        lifecycleCoordinator.HandleClosing();
        lifecycleCoordinator.HandleClosed();
        ticker.RaiseTick();

        Assert.Same(trayWindow, trayLifecycle.RegisteredWindow);
        Assert.Same(trayWindow, trayLifecycle.MinimizedWindow);
        Assert.Equal(1, trackingCoordinator.StartTrackingCallCount);
        Assert.Equal(1, trackingCoordinator.PollOnceCallCount);
        Assert.Equal(1, trackingCoordinator.StopTrackingCallCount);
        Assert.False(ticker.IsRunning);
        Assert.Equal(0, ticker.SubscriberCount);
        Assert.Equal("Stopped", viewModel.TrackingStatusText);
    }

    private sealed class RecordingTrackingCoordinator : IDashboardTrackingCoordinator
    {
        public int StartTrackingCallCount { get; private set; }

        public int StopTrackingCallCount { get; private set; }

        public int PollOnceCallCount { get; private set; }

        public DashboardTrackingSnapshot StartTracking()
        {
            StartTrackingCallCount++;

            return CreateSnapshot(TimeSpan.Zero);
        }

        public DashboardTrackingSnapshot StopTracking()
        {
            StopTrackingCallCount++;

            return DashboardTrackingSnapshot.Empty;
        }

        public DashboardTrackingSnapshot PollOnce()
        {
            PollOnceCallCount++;

            return CreateSnapshot(TimeSpan.FromSeconds(1));
        }

        public DashboardSyncResult SyncNow(bool syncEnabled)
            => new("Sync skipped. Enable sync to upload.");

        private static DashboardTrackingSnapshot CreateSnapshot(TimeSpan duration)
            => new(
                AppName: "Code.exe",
                ProcessName: "Code.exe",
                WindowTitle: "Project - Visual Studio Code",
                CurrentSessionDuration: duration,
                LastPersistedSession: null,
                LastPollAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));
    }

    private sealed class ManualTrackingTicker : ITrackingTicker
    {
        private EventHandler? _tick;

        public event EventHandler? Tick
        {
            add => _tick += value;
            remove => _tick -= value;
        }

        public bool IsRunning { get; private set; }

        public int SubscriberCount
            => _tick?.GetInvocationList().Length ?? 0;

        public void Start()
            => IsRunning = true;

        public void Stop()
            => IsRunning = false;

        public void RaiseTick()
            => _tick?.Invoke(this, EventArgs.Empty);
    }

    private sealed class RecordingTrayLifecycleService : IWindowsTrayLifecycleService
    {
        public bool IsExplicitExitRequested { get; private set; }

        public ITrayLifecycleWindow? RegisteredWindow { get; private set; }

        public ITrayLifecycleWindow? MinimizedWindow { get; private set; }

        public void RegisterWindow(ITrayLifecycleWindow window)
            => RegisteredWindow = window;

        public void MinimizeToTaskbar(ITrayLifecycleWindow window)
            => MinimizedWindow = window;

        public void RestoreFromTray(ITrayLifecycleWindow window)
        {
        }

        public void RequestExplicitExit(ITrayLifecycleWindow? window)
            => IsExplicitExitRequested = true;
    }

    private sealed class FakeTrayWindow : ITrayLifecycleWindow
    {
        public bool ShowInTaskbar { get; set; } = true;

        public WindowState WindowState { get; set; } = WindowState.Normal;

        public void Hide()
        {
        }

        public void Show()
        {
        }

        public void Activate()
        {
        }

        public void Close()
        {
        }
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
