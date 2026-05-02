using System.Windows;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public sealed class WpfDashboardApplicationLifetime : IDashboardApplicationLifetime
{
    private readonly IWindowsTrayLifecycleService _trayLifecycle;

    public WpfDashboardApplicationLifetime(IWindowsTrayLifecycleService trayLifecycle)
    {
        _trayLifecycle = trayLifecycle;
    }

    public void RequestExit()
    {
        if (Application.Current?.MainWindow is MainWindow mainWindow)
        {
            _trayLifecycle.RequestExplicitExit(new WpfTrayLifecycleWindow(mainWindow));
            return;
        }

        _trayLifecycle.RequestExplicitExit(window: null);
    }
}
