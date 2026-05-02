using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using Forms = System.Windows.Forms;
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

public sealed class WindowsNotifyIcon : IWindowsTrayIcon
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Icon _icon;
    private bool _disposed;

    public WindowsNotifyIcon()
    {
        _icon = WoongTrayIconFactory.CreateIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Woong Monitor Stack",
            Icon = _icon,
            Visible = false,
            ContextMenuStrip = BuildContextMenu()
        };
        _notifyIcon.DoubleClick += OnRestoreRequested;
    }

    public bool IsVisible
    {
        get => _notifyIcon.Visible;
        set => _notifyIcon.Visible = value;
    }

    public event EventHandler? RestoreRequested;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.DoubleClick -= OnRestoreRequested;
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _icon.Dispose();
    }

    private Forms.ContextMenuStrip BuildContextMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        _ = menu.Items.Add("Show Woong Monitor Stack", null, (_, _) => RestoreRequested?.Invoke(this, EventArgs.Empty));

        return menu;
    }

    private void OnRestoreRequested(object? sender, EventArgs e)
        => RestoreRequested?.Invoke(this, EventArgs.Empty);
}

public static class WoongTrayIconFactory
{
    public static Icon CreateIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);

        using var background = new SolidBrush(Color.FromArgb(31, 41, 55));
        graphics.FillRectangle(background, 3, 3, 26, 26);

        using var accent = new SolidBrush(Color.FromArgb(34, 197, 94));
        graphics.FillRectangle(accent, 8, 20, 4, 5);
        graphics.FillRectangle(accent, 14, 13, 4, 12);
        graphics.FillRectangle(accent, 20, 8, 4, 17);

        using var foreground = new SolidBrush(Color.White);
        graphics.FillRectangle(foreground, 8, 8, 9, 2);
        graphics.FillRectangle(foreground, 8, 13, 5, 2);

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using Icon icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            _ = DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
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
        window.ShowInTaskbar = false;
        window.Hide();
        _trayIcon.IsVisible = true;
        WriteLifecycleEvent("Window hidden to notification area");
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
