using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public interface IWindowsAppStartupService
{
    void Start();
}

public sealed class WindowsAppStartupService(MainWindow mainWindow) : IWindowsAppStartupService
{
    public void Start()
    {
        if (mainWindow.DataContext is DashboardViewModel dashboardViewModel)
        {
            dashboardViewModel.SelectPeriod(DashboardPeriod.Today);
        }

        mainWindow.Show();
    }
}
