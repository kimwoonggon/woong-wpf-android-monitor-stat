using Microsoft.Extensions.DependencyInjection;
using Woong.MonitorStack.Windows.App.Dashboard;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public static class WindowsAppServiceCollectionExtensions
{
    public static IServiceCollection AddWindowsApp(
        this IServiceCollection services,
        DashboardOptions dashboardOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(dashboardOptions);

        services.AddSingleton(dashboardOptions);
        services.AddDashboardPresentation();
        services.AddWindowsInfrastructure();
        services.AddSingleton<MainWindow>();

        return services;
    }

    public static IServiceCollection AddDashboardPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IDashboardClock, SystemDashboardClock>();
        services.AddTransient<DashboardViewModel>();

        return services;
    }

    public static IServiceCollection AddWindowsInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IDashboardDataSource, EmptyDashboardDataSource>();

        return services;
    }
}
