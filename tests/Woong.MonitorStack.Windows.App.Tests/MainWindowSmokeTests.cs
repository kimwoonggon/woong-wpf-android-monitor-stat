using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Interop;
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
                new DashboardOptions("Asia/Seoul"));
            viewModel.SelectPeriod(DashboardPeriod.Today);

            var window = new MainWindow(viewModel);

            try
            {
                Assert.Same(viewModel, window.DataContext);
                Assert.Equal("Woong Monitor Stack", window.Title);
                Assert.True(window.ShowInTaskbar);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void MainWindow_SystemCloseButton_HidesToNotificationAreaWithoutClosing()
        => RunOnStaThread(() =>
        {
            var viewModel = new DashboardViewModel(
                new EmptyDataSource(),
                new FixedClock(new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero)),
                new DashboardOptions("Asia/Seoul"));
            var window = new MainWindow(viewModel);
            var closed = false;
            window.Closed += (_, _) => closed = true;

            try
            {
                window.Show();
                window.UpdateLayout();

                SendMessage(new WindowInteropHelper(window).Handle, 0x0112, 0xF060, 0);
                window.UpdateLayout();

                Assert.False(closed);
                Assert.False(window.IsVisible);
                Assert.False(window.ShowInTaskbar);
                Assert.Equal(WindowState.Minimized, window.WindowState);
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

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

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
