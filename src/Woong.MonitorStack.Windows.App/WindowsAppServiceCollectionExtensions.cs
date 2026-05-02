using Microsoft.Extensions.DependencyInjection;
using Woong.MonitorStack.Windows.App.Browser;
using Woong.MonitorStack.Windows.App.Dashboard;
using Woong.MonitorStack.Windows.Browser;
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
        services.AddSingleton(new WindowsLocalDatabaseState(options.LocalDatabasePath));
        services.AddSingleton(options.DashboardOptions);
        services.AddSingleton<IDashboardRuntimeLogSink>(new FileDashboardRuntimeLogSink(options.RuntimeLogPath));
        services.AddSingleton<IWindowsTrayIcon, NoopWindowsTrayIcon>();
        services.AddSingleton<IWindowsTrayLifecycleService, WindowsTrayLifecycleService>();
        services.AddSingleton<IDashboardApplicationLifetime, WpfDashboardApplicationLifetime>();
        services.AddDashboardPresentation();
        services.AddWindowsInfrastructure();
        if (options.AcceptanceMode == WindowsAppAcceptanceMode.TrackingPipeline)
        {
            services.AddTrackingPipelineAcceptanceMode();
        }
        else if (options.AcceptanceMode == WindowsAppAcceptanceMode.SampleDashboard)
        {
            services.AddSampleDashboardMode();
        }

        services.AddTransient<ITrackingTicker, DispatcherTrackingTicker>();
        services.AddSingleton(provider => new MainWindow(
            provider.GetRequiredService<DashboardViewModel>(),
            new MainWindowStartupOptions(provider.GetRequiredService<WindowsAppOptions>().AutoStartTracking),
            provider.GetRequiredService<ITrackingTicker>(),
            provider.GetRequiredService<IWindowsTrayLifecycleService>()));
        services.AddSingleton<IWindowsAppStartupService, WindowsAppStartupService>();

        return services;
    }

    public static IServiceCollection AddDashboardPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IDashboardClock, SystemDashboardClock>();
        services.AddSingleton<IDashboardTrackingCoordinator, NoopDashboardTrackingCoordinator>();
        services.AddSingleton<IDashboardChartDetailsPresenter, DashboardChartDetailsWindowPresenter>();
        services.AddTransient<DashboardViewModel>();

        return services;
    }

    public static IServiceCollection AddWindowsInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IForegroundWindowReader, WindowsForegroundWindowReader>();
        services.AddSingleton<ILastInputReader, WindowsLastInputReader>();
        services.AddSingleton<IBrowserProcessClassifier, BrowserProcessClassifier>();
        services.AddSingleton<IBrowserUrlSanitizer, BrowserUrlSanitizer>();
        services.AddSingleton<IBrowserAddressBarReader, WindowsUiAutomationAddressBarReader>();
        services.AddSingleton<IBrowserActivityReader, UiAutomationBrowserActivityReader>();
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
                () => provider.GetRequiredService<WindowsLocalDatabaseState>().ConnectionString);
            repository.Initialize();
            return repository;
        });
        services.AddSingleton(provider =>
        {
            var repository = new SqliteWebSessionRepository(
                () => provider.GetRequiredService<WindowsLocalDatabaseState>().ConnectionString);
            repository.Initialize();
            return repository;
        });
        services.AddSingleton(provider =>
        {
            var repository = new SqliteSyncOutboxRepository(
                () => provider.GetRequiredService<WindowsLocalDatabaseState>().ConnectionString);
            repository.Initialize();
            return repository;
        });
        services.AddSingleton<ISyncOutboxRepository>(provider => provider.GetRequiredService<SqliteSyncOutboxRepository>());
        services.AddSingleton<WindowsFocusSessionPersistenceService>();
        services.AddSingleton(provider => new WindowsWebSessionPersistenceService(
            provider.GetRequiredService<SqliteWebSessionRepository>(),
            provider.GetRequiredService<SqliteSyncOutboxRepository>(),
            provider.GetRequiredService<ISystemClock>(),
            BrowserUrlStoragePolicy.DomainOnly));
        services.AddSingleton<IDashboardDataSource, SqliteDashboardDataSource>();
        services.AddSingleton<IWindowsDatabaseFilePicker, WindowsDatabaseFilePicker>();
        services.AddSingleton<IDashboardDatabaseController, WindowsLocalDatabaseController>();
        services.AddSingleton<IDashboardTrackingCoordinator>(provider => new WindowsTrackingDashboardCoordinator(
            provider.GetRequiredService<Func<TrackingPoller>>(),
            provider.GetRequiredService<WindowsFocusSessionPersistenceService>(),
            provider.GetRequiredService<WindowsWebSessionPersistenceService>(),
            provider.GetRequiredService<ISystemClock>(),
            provider.GetRequiredService<IBrowserActivityReader>(),
            provider.GetRequiredService<IBrowserUrlSanitizer>(),
            BrowserUrlStoragePolicy.DomainOnly));

        return services;
    }

    private static IServiceCollection AddTrackingPipelineAcceptanceMode(this IServiceCollection services)
    {
        services.AddSingleton<AcceptanceTrackingScenarioClock>();
        services.AddSingleton<IDashboardClock>(provider => provider.GetRequiredService<AcceptanceTrackingScenarioClock>());
        services.AddSingleton<IDashboardTrackingCoordinator>(provider =>
        {
            WindowsAppOptions options = provider.GetRequiredService<WindowsAppOptions>();

            return new AcceptanceTrackingDashboardCoordinator(
                provider.GetRequiredService<SqliteFocusSessionRepository>(),
                provider.GetRequiredService<SqliteWebSessionRepository>(),
                provider.GetRequiredService<SqliteSyncOutboxRepository>(),
                provider.GetRequiredService<AcceptanceTrackingScenarioClock>(),
                options.DeviceId,
                options.DashboardOptions.TimeZoneId);
        });

        return services;
    }

    private static IServiceCollection AddSampleDashboardMode(this IServiceCollection services)
    {
        services.AddSingleton<IDashboardClock>(new SampleDashboardClock(SampleDashboardDataSource.SampleNowUtc));
        services.AddSingleton<IDashboardDataSource, SampleDashboardDataSource>();
        services.AddSingleton<IDashboardTrackingCoordinator, NoopDashboardTrackingCoordinator>();

        return services;
    }
}
