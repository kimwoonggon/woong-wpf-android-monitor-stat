using System.Windows;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public sealed class WpfDashboardApplicationLifetime : IDashboardApplicationLifetime
{
    public void RequestExit()
    {
        if (Application.Current?.MainWindow is MainWindow mainWindow)
        {
            mainWindow.Close();
            return;
        }

        Application.Current?.Shutdown();
    }
}

