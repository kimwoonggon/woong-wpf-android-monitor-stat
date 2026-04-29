using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Woong.MonitorStack.Windows.App.Dashboard;
using Woong.MonitorStack.Windows.Browser;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class WindowsAppCompositionTests
{
    [Fact]
    public void AddWindowsApp_ResolvesMainWindowWithDashboardViewModel()
        => RunOnStaThread(() =>
        {
            var services = new ServiceCollection();
            services.AddWindowsApp(new DashboardOptions("Asia/Seoul"));

            using ServiceProvider provider = services.BuildServiceProvider();
            var window = provider.GetRequiredService<MainWindow>();

            try
            {
                Assert.IsType<DashboardViewModel>(window.DataContext);
                Assert.Equal("Woong Monitor Stack", window.Title);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void AddWindowsApp_RegistersWindowsTrackingCoordinatorAndSqliteDashboardDataSource()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        try
        {
            var services = new ServiceCollection();
            services.AddWindowsApp(new WindowsAppOptions(
                new DashboardOptions("Asia/Seoul"),
                deviceId: "windows-device-1",
                localDatabaseConnectionString: $"Data Source={dbPath};Pooling=False",
                idleThreshold: TimeSpan.FromMinutes(5)));

            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.IsType<WindowsTrackingDashboardCoordinator>(
                provider.GetRequiredService<IDashboardTrackingCoordinator>());
            Assert.IsType<SqliteDashboardDataSource>(
                provider.GetRequiredService<IDashboardDataSource>());
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void AddWindowsApp_RegistersBrowserActivityReaderForImmediateDomainCapture()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        try
        {
            var services = new ServiceCollection();
            services.AddWindowsApp(new WindowsAppOptions(
                new DashboardOptions("Asia/Seoul"),
                deviceId: "windows-device-1",
                localDatabaseConnectionString: $"Data Source={dbPath};Pooling=False",
                idleThreshold: TimeSpan.FromMinutes(5)));

            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.IsAssignableFrom<IBrowserActivityReader>(
                provider.GetRequiredService<IBrowserActivityReader>());
            Assert.IsType<BrowserUrlSanitizer>(
                provider.GetRequiredService<IBrowserUrlSanitizer>());
            Assert.IsType<WindowsTrackingDashboardCoordinator>(
                provider.GetRequiredService<IDashboardTrackingCoordinator>());
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw failure;
        }
    }
}
