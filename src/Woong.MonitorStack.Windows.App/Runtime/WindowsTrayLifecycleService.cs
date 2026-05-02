using System.Diagnostics;
using System.Windows;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public interface ITrayLifecycleWindow
{
    bool ShowInTaskbar { get; set; }

    WindowState WindowState { get; set; }

    void Hide();

    void Show();

    void Activate();

    void Close();
}

public interface IWindowsTrayIcon : IDisposable
{
    bool IsVisible { get; set; }

    event EventHandler? RestoreRequested;
}

public sealed class NoopWindowsTrayIcon : IWindowsTrayIcon
{
    public bool IsVisible { get; set; }

    public event EventHandler? RestoreRequested
    {
        add { }
        remove { }
    }

    public void Dispose()
    {
        IsVisible = false;
    }
}

public interface IWindowsTrayLifecycleService
{
    bool IsExplicitExitRequested { get; }

    void RegisterWindow(ITrayLifecycleWindow window);

    void MinimizeToTaskbar(ITrayLifecycleWindow window);

    void RestoreFromTray(ITrayLifecycleWindow window);

    void RequestExplicitExit(ITrayLifecycleWindow? window);
}

public sealed class WindowsTrayLifecycleService : IWindowsTrayLifecycleService
{
    private readonly IWindowsTrayIcon _trayIcon;
    private readonly IDashboardRuntimeLogSink _runtimeLogSink;
    private readonly Action<string> _fallbackDiagnostic;
    private ITrayLifecycleWindow? _registeredWindow;

    public WindowsTrayLifecycleService(
        IWindowsTrayIcon trayIcon,
        IDashboardRuntimeLogSink runtimeLogSink,
        Action<string>? fallbackDiagnostic = null)
    {
        _trayIcon = trayIcon ?? throw new ArgumentNullException(nameof(trayIcon));
        _runtimeLogSink = runtimeLogSink ?? throw new ArgumentNullException(nameof(runtimeLogSink));
        _fallbackDiagnostic = fallbackDiagnostic ?? (message => Trace.WriteLine(message));
        _trayIcon.RestoreRequested += OnTrayRestoreRequested;
    }

    public bool IsExplicitExitRequested { get; private set; }

    public void RegisterWindow(ITrayLifecycleWindow window)
        => _registeredWindow = window ?? throw new ArgumentNullException(nameof(window));

    public void MinimizeToTaskbar(ITrayLifecycleWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        window.WindowState = WindowState.Minimized;
        window.ShowInTaskbar = true;
        _trayIcon.IsVisible = true;
        WriteLifecycleEvent("Window minimized to taskbar");
    }

    public void RestoreFromTray(ITrayLifecycleWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        window.Show();
        window.WindowState = WindowState.Normal;
        window.ShowInTaskbar = true;
        _trayIcon.IsVisible = true;
        window.Activate();
        WriteLifecycleEvent("Window restored from tray");
    }

    public void RequestExplicitExit(ITrayLifecycleWindow? window)
    {
        IsExplicitExitRequested = true;
        WriteLifecycleEvent("Explicit exit requested");
        _trayIcon.IsVisible = false;
        _trayIcon.Dispose();

        if (window is not null)
        {
            window.Close();
        }
        else
        {
            Application.Current?.Shutdown();
        }
    }

    private void OnTrayRestoreRequested(object? sender, EventArgs e)
    {
        if (_registeredWindow is not null)
        {
            RestoreFromTray(_registeredWindow);
        }
    }

    private void WriteLifecycleEvent(string eventType)
    {
        try
        {
            _runtimeLogSink.WriteEvent(new DashboardRuntimeLogEvent(
                DateTimeOffset.UtcNow,
                eventType,
                "",
                "",
                eventType));
        }
        catch (Exception exception)
        {
            _fallbackDiagnostic($"{eventType}: Runtime log sink failed with {exception.GetType().Name}: {exception.Message}");
        }
    }
}

public sealed class WpfTrayLifecycleWindow : ITrayLifecycleWindow
{
    private readonly Window _window;

    public WpfTrayLifecycleWindow(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    public bool ShowInTaskbar
    {
        get => _window.ShowInTaskbar;
        set => _window.ShowInTaskbar = value;
    }

    public WindowState WindowState
    {
        get => _window.WindowState;
        set => _window.WindowState = value;
    }

    public void Hide()
        => _window.Hide();

    public void Show()
        => _window.Show();

    public void Activate()
        => _window.Activate();

    public void Close()
        => _window.Close();
}
