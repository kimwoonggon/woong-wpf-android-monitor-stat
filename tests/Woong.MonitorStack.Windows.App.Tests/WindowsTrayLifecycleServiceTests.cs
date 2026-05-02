using System.Windows;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class WindowsTrayLifecycleServiceTests
{
    [Fact]
    public void MinimizeToTaskbar_HidesWindowFromTaskbarShowsTrayAndWritesLifecycleEvent()
    {
        var window = new FakeTrayWindow();
        var trayIcon = new FakeTrayIcon();
        var runtimeLogSink = new RecordingRuntimeLogSink();
        var service = new WindowsTrayLifecycleService(trayIcon, runtimeLogSink);

        service.MinimizeToTaskbar(window);

        Assert.Equal(1, window.HideCallCount);
        Assert.False(window.ShowInTaskbar);
        Assert.Equal(WindowState.Minimized, window.WindowState);
        Assert.True(trayIcon.IsVisible);
        DashboardRuntimeLogEvent logEvent = Assert.Single(runtimeLogSink.Events);
        Assert.Equal("Window hidden to notification area", logEvent.EventType);
    }

    [Fact]
    public void RestoreFromTray_ShowsActivatesWindowAndKeepsTrayAvailable()
    {
        var window = new FakeTrayWindow
        {
            ShowInTaskbar = false,
            WindowState = WindowState.Minimized
        };
        var trayIcon = new FakeTrayIcon { IsVisible = true };
        var service = new WindowsTrayLifecycleService(trayIcon, new RecordingRuntimeLogSink());

        service.RestoreFromTray(window);

        Assert.Equal(1, window.ShowCallCount);
        Assert.Equal(1, window.ActivateCallCount);
        Assert.True(window.ShowInTaskbar);
        Assert.Equal(WindowState.Normal, window.WindowState);
        Assert.True(trayIcon.IsVisible);
    }

    [Fact]
    public void RequestExplicitExit_ClosesWindowDisposesTrayAndDoesNotMinimizeToTaskbar()
    {
        var window = new FakeTrayWindow();
        var trayIcon = new FakeTrayIcon { IsVisible = true };
        var runtimeLogSink = new RecordingRuntimeLogSink();
        var service = new WindowsTrayLifecycleService(trayIcon, runtimeLogSink);

        service.RequestExplicitExit(window);

        Assert.True(service.IsExplicitExitRequested);
        Assert.Equal(1, window.CloseCallCount);
        Assert.Equal(0, window.HideCallCount);
        Assert.True(trayIcon.IsDisposed);
        Assert.Contains(runtimeLogSink.Events, logEvent => logEvent.EventType == "Explicit exit requested");
    }

    [Fact]
    public void MinimizeToTaskbar_WhenRuntimeLogSinkThrows_DoesNotThrowAndWritesFallbackDiagnostic()
    {
        var fallbackMessages = new List<string>();
        var service = new WindowsTrayLifecycleService(
            new FakeTrayIcon(),
            new ThrowingRuntimeLogSink(),
            fallbackMessages.Add);

        Exception? thrown = Record.Exception(() => service.MinimizeToTaskbar(new FakeTrayWindow()));

        Assert.Null(thrown);
        string fallbackMessage = Assert.Single(fallbackMessages);
        Assert.Contains("Window hidden to notification area", fallbackMessage, StringComparison.Ordinal);
        Assert.Contains("Runtime log sink failed", fallbackMessage, StringComparison.Ordinal);
    }

    private sealed class FakeTrayWindow : ITrayLifecycleWindow
    {
        public bool ShowInTaskbar { get; set; } = true;

        public WindowState WindowState { get; set; } = WindowState.Normal;

        public int HideCallCount { get; private set; }

        public int ShowCallCount { get; private set; }

        public int ActivateCallCount { get; private set; }

        public int CloseCallCount { get; private set; }

        public void Hide()
            => HideCallCount++;

        public void Show()
            => ShowCallCount++;

        public void Activate()
            => ActivateCallCount++;

        public void Close()
            => CloseCallCount++;
    }

    private sealed class FakeTrayIcon : IWindowsTrayIcon
    {
        public bool IsVisible { get; set; }

        public bool IsDisposed { get; private set; }

        public event EventHandler? RestoreRequested;

        public void RaiseRestoreRequested()
            => RestoreRequested?.Invoke(this, EventArgs.Empty);

        public void Dispose()
            => IsDisposed = true;
    }

    private sealed class RecordingRuntimeLogSink : IDashboardRuntimeLogSink
    {
        public string LogPath { get; } = "D:\\logs\\windows-runtime.log";

        public List<DashboardRuntimeLogEvent> Events { get; } = [];

        public void WriteEvent(DashboardRuntimeLogEvent logEvent)
            => Events.Add(logEvent);

        public void WriteException(string operation, Exception exception)
        {
        }

        public DashboardRuntimeLogFolderOpenResult OpenLogFolder()
            => new(true, "D:\\logs", "Opened runtime log folder.");
    }

    private sealed class ThrowingRuntimeLogSink : IDashboardRuntimeLogSink
    {
        public string LogPath { get; } = "D:\\logs\\windows-runtime.log";

        public void WriteEvent(DashboardRuntimeLogEvent logEvent)
            => throw new InvalidOperationException("Runtime log is locked.");

        public void WriteException(string operation, Exception exception)
        {
        }

        public DashboardRuntimeLogFolderOpenResult OpenLogFolder()
            => new(false, "D:\\logs", "Could not open runtime log folder.");
    }
}
