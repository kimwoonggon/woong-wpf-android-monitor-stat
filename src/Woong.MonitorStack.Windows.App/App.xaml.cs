using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host
            .CreateDefaultBuilder(e.Args)
            .ConfigureServices(services => services.AddWindowsApp(new DashboardOptions(TimeZoneInfo.Local.Id)))
            .Build();
        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        if (mainWindow.DataContext is DashboardViewModel dashboardViewModel)
        {
            dashboardViewModel.SelectPeriod(DashboardPeriod.Today);
        }

        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
