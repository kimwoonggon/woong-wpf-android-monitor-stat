using Microsoft.Extensions.DependencyInjection;
using Woong.MonitorStack.Windows.App.Dashboard;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using Woong.MonitorStack.Windows.Storage;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.App;

public static class WindowsAppServiceCollectionExtensions
{
    public static IServiceCollection AddWindowsApp(
        this IServiceCollection services,
        DashboardOptions dashboardOptions)
        => services.AddWindowsApp(WindowsAppOptions.CreateDefault(dashboardOptions));

    public static IServiceCollection AddWindowsApp(
        this IServiceCollection services,
        WindowsAppOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.AddSingleton(options.DashboardOptions);
        services.AddDashboardPresentation();
        services.AddWindowsInfrastructure();
        services.AddSingleton<MainWindow>();

        return services;
    }

    public static IServiceCollection AddDashboardPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IDashboardClock, SystemDashboardClock>();
        services.AddSingleton<IDashboardTrackingCoordinator, NoopDashboardTrackingCoordinator>();
        services.AddTransient<DashboardViewModel>();

        return services;
    }

    public static IServiceCollection AddWindowsInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IForegroundWindowReader, WindowsForegroundWindowReader>();
        services.AddSingleton<ILastInputReader, WindowsLastInputReader>();
        services.AddSingleton(provider => new ForegroundWindowCollector(
            provider.GetRequiredService<IForegroundWindowReader>(),
            provider.GetRequiredService<ISystemClock>()));
        services.AddSingleton(provider => new IdleDetector(provider.GetRequiredService<WindowsAppOptions>().IdleThreshold));
        services.AddTransient(provider => new FocusSessionizer(
            provider.GetRequiredService<WindowsAppOptions>().DeviceId,
            provider.GetRequiredService<DashboardOptions>().TimeZoneId));
        services.AddTransient<TrackingPoller>();
        services.AddSingleton<Func<TrackingPoller>>(provider => () => provider.GetRequiredService<TrackingPoller>());
        services.AddSingleton(provider =>
        {
            var repository = new SqliteFocusSessionRepository(
                provider.GetRequiredService<WindowsAppOptions>().LocalDatabaseConnectionString);
            repository.Initialize();
            return repository;
        });
        services.AddSingleton(provider =>
        {
            var repository = new SqliteWebSessionRepository(
                provider.GetRequiredService<WindowsAppOptions>().LocalDatabaseConnectionString);
            repository.Initialize();
            return repository;
        });
        services.AddSingleton(provider =>
        {
            var repository = new SqliteSyncOutboxRepository(
                provider.GetRequiredService<WindowsAppOptions>().LocalDatabaseConnectionString);
            repository.Initialize();
            return repository;
        });
        services.AddSingleton<ISyncOutboxRepository>(provider => provider.GetRequiredService<SqliteSyncOutboxRepository>());
        services.AddSingleton<IDashboardDataSource, SqliteDashboardDataSource>();
        services.AddSingleton<IDashboardTrackingCoordinator, WindowsTrackingDashboardCoordinator>();

        return services;
    }
}
