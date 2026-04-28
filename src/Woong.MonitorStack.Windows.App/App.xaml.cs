using System.Windows;
using Woong.MonitorStack.Windows.App.Dashboard;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dashboardViewModel = new DashboardViewModel(
            new EmptyDashboardDataSource(),
            new SystemDashboardClock(),
            TimeZoneInfo.Local.Id);
        dashboardViewModel.SelectPeriod(DashboardPeriod.Today);

        var mainWindow = new MainWindow(dashboardViewModel);
        mainWindow.Show();
    }
}
