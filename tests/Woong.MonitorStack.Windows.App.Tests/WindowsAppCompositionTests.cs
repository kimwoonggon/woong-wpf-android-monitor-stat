using Microsoft.Extensions.DependencyInjection;
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
