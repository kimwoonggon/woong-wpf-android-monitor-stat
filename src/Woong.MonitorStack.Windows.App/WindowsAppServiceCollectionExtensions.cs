using System.IO;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Woong.MonitorStack.Windows.App.Browser;
using Woong.MonitorStack.Windows.App.Dashboard;
using Woong.MonitorStack.Windows.Browser;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using Woong.MonitorStack.Windows.Storage;
using Woong.MonitorStack.Windows.Sync;
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
        services.AddSingleton<IWindowsTrayIcon, WindowsNotifyIcon>();
        services.AddSingleton<IWindowsTrayLifecycleService, WindowsTrayLifecycleService>();
        services.AddSingleton<IDashboardApplicationLifetime, WpfDashboardApplicationLifetime>();
        services.AddDashboardPresentation(options.SyncOptions);
        services.AddWindowsInfrastructure(options);
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
        => services.AddDashboardPresentation(WindowsAppSyncOptions.LocalOnly);

    private static IServiceCollection AddDashboardPresentation(this IServiceCollection services, WindowsAppSyncOptions syncOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(syncOptions);

        services.AddSingleton<IDashboardClock, SystemDashboardClock>();
        services.AddSingleton<IDashboardTrackingCoordinator, NoopDashboardTrackingCoordinator>();
        services.AddSingleton<IDashboardChartDetailsPresenter, DashboardChartDetailsWindowPresenter>();
        services.AddTransient(provider => CreateDashboardViewModel(provider, syncOptions));

        return services;
    }

    private static DashboardViewModel CreateDashboardViewModel(
        IServiceProvider provider,
        WindowsAppSyncOptions syncOptions)
    {
        DashboardViewModel viewModel = ActivatorUtilities.CreateInstance<DashboardViewModel>(provider);
        viewModel.Settings.SyncEndpointText = syncOptions.EndpointDisplayText;

        return viewModel;
    }

    public static IServiceCollection AddWindowsInfrastructure(this IServiceCollection services)
        => services.AddWindowsInfrastructure(WindowsAppSyncOptions.LocalOnly);

    private static IServiceCollection AddWindowsInfrastructure(this IServiceCollection services, WindowsAppOptions options)
        => services.AddWindowsInfrastructure(options.SyncOptions);

    private static IServiceCollection AddWindowsInfrastructure(this IServiceCollection services, WindowsAppSyncOptions syncOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(syncOptions);

        services.AddWindowsCaptureServices();
        services.AddBrowserCaptureServices();
        services.AddTrackingPipelineServices();
        services.AddStorageServices();
        services.AddSyncServices(syncOptions);
        services.AddDashboardAdapterServices();

        return services;
    }

    private static IServiceCollection AddWindowsCaptureServices(this IServiceCollection services)
    {
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IForegroundWindowReader, WindowsForegroundWindowReader>();
        services.AddSingleton<ILastInputReader, WindowsLastInputReader>();

        return services;
    }

    private static IServiceCollection AddBrowserCaptureServices(this IServiceCollection services)
    {
        services.AddSingleton<IBrowserProcessClassifier, BrowserProcessClassifier>();
        services.AddSingleton<IBrowserUrlSanitizer, BrowserUrlSanitizer>();
        services.AddSingleton<IBrowserAddressBarReader, WindowsUiAutomationAddressBarReader>();
        services.AddSingleton<IBrowserActivityReader, UiAutomationBrowserActivityReader>();

        return services;
    }

    private static IServiceCollection AddTrackingPipelineServices(this IServiceCollection services)
    {
        services.AddSingleton(provider => new ForegroundWindowCollector(
            provider.GetRequiredService<IForegroundWindowReader>(),
            provider.GetRequiredService<ISystemClock>()));
        services.AddSingleton(provider => new IdleDetector(provider.GetRequiredService<WindowsAppOptions>().IdleThreshold));
        services.AddTransient(provider => new FocusSessionizer(
            provider.GetRequiredService<WindowsAppOptions>().DeviceId,
            provider.GetRequiredService<DashboardOptions>().TimeZoneId));
        services.AddTransient<TrackingPoller>();
        services.AddSingleton<Func<TrackingPoller>>(provider => () => provider.GetRequiredService<TrackingPoller>());

        return services;
    }

    private static IServiceCollection AddStorageServices(this IServiceCollection services)
    {
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

        return services;
    }

    private static IServiceCollection AddDashboardAdapterServices(this IServiceCollection services)
    {
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
            BrowserUrlStoragePolicy.DomainOnly,
            provider.GetService<IWindowsSyncApiClient>() is null
                ? null
                : provider.GetRequiredService<WindowsSyncWorker>()));

        return services;
    }

    private static IServiceCollection AddSyncServices(this IServiceCollection services, WindowsAppSyncOptions syncOptions)
    {
        services.TryAddSingleton<IWindowsUserDataProtector, DpapiWindowsUserDataProtector>();
        services.TryAddSingleton<IWindowsSyncTokenStore>(provider => new FileWindowsSyncTokenStore(
            BuildSyncTokenFilePath(provider.GetService<WindowsAppOptions>()),
            provider.GetRequiredService<IWindowsUserDataProtector>()));

        if (syncOptions.IsUploadConfigured)
        {
            services.TryAddSingleton(syncOptions.CreateClientOptions());
            services.TryAddSingleton(provider => new HttpClient
            {
                BaseAddress = provider.GetRequiredService<WindowsSyncClientOptions>().ServerBaseUri
            });
            services.TryAddSingleton<IWindowsSyncApiClient, HttpWindowsSyncApiClient>();
        }

        services.AddSingleton<WindowsSyncWorker>();

        return services;
    }

    private static string BuildSyncTokenFilePath(WindowsAppOptions? options)
    {
        string? databaseDirectory = options is null
            ? null
            : Path.GetDirectoryName(options.LocalDatabasePath);
        string tokenDirectory = string.IsNullOrWhiteSpace(databaseDirectory)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WoongMonitorStack")
            : databaseDirectory;

        return Path.Combine(tokenDirectory, "windows-sync-device-token.dat");
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
