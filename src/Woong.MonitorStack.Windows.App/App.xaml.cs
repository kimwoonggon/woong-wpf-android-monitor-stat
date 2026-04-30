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
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _host = Host
            .CreateDefaultBuilder(e.Args)
            .ConfigureServices(services => services.AddWindowsApp(new DashboardOptions(TimeZoneInfo.Local.Id)))
            .Build();
        await _host.StartAsync();

        _host.Services.GetRequiredService<IWindowsAppStartupService>().Start();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogUnhandledException("DispatcherUnhandledException", e.Exception);
        e.Handled = _host is not null;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            LogUnhandledException("UnhandledException", exception);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogUnhandledException("UnobservedTaskException", e.Exception);
        e.SetObserved();
    }

    private void LogUnhandledException(string operation, Exception exception)
    {
        try
        {
            _host?.Services
                .GetService<IDashboardRuntimeLogSink>()
                ?.WriteException(operation, exception);
        }
        catch (Exception)
        {
        }
    }
}
