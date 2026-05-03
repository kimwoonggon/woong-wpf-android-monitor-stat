using System.Windows;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public sealed class WpfDashboardApplicationLifetime : IDashboardApplicationLifetime
{
    private readonly IWindowsTrayLifecycleService _trayLifecycle;
    private readonly Func<Window?> _currentMainWindow;

    public WpfDashboardApplicationLifetime(IWindowsTrayLifecycleService trayLifecycle)
        : this(trayLifecycle, () => Application.Current?.MainWindow)
    {
    }

    public WpfDashboardApplicationLifetime(
        IWindowsTrayLifecycleService trayLifecycle,
        Func<Window?> currentMainWindow)
    {
        _trayLifecycle = trayLifecycle ?? throw new ArgumentNullException(nameof(trayLifecycle));
        _currentMainWindow = currentMainWindow ?? throw new ArgumentNullException(nameof(currentMainWindow));
    }

    public void RequestExit()
    {
        if (_currentMainWindow() is MainWindow mainWindow)
        {
            _trayLifecycle.RequestExplicitExit(new WpfTrayLifecycleWindow(mainWindow));
            return;
        }

        _trayLifecycle.RequestExplicitExit(window: null);
    }
}
