using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class MainWindowSmokeTests
{
    [Fact]
    public void MainWindow_ConstructsWithDashboardViewModel()
        => RunOnStaThread(() =>
        {
            var viewModel = new DashboardViewModel(
                new EmptyDataSource(),
                new FixedClock(new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero)),
                "Asia/Seoul");
            viewModel.SelectPeriod(DashboardPeriod.Today);

            var window = new MainWindow(viewModel);

            try
            {
                Assert.Same(viewModel, window.DataContext);
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

    private sealed class EmptyDataSource : IDashboardDataSource
    {
        public IReadOnlyList<FocusSession> QueryFocusSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => [];

        public IReadOnlyList<WebSession> QueryWebSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => [];
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IDashboardClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
