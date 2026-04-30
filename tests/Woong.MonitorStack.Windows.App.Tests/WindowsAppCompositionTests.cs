using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Woong.MonitorStack.Domain.Common;
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
    public void AppStartup_CodeBehindDelegatesWindowInitializationToStartupService()
    {
        string appStartupSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Woong.MonitorStack.Windows.App",
            "App.xaml.cs"));

        Assert.Contains("Host", appStartupSource, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<IWindowsAppStartupService>()", appStartupSource, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<MainWindow>", appStartupSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectPeriod", appStartupSource, StringComparison.Ordinal);
        Assert.DoesNotContain(".Show()", appStartupSource, StringComparison.Ordinal);
    }

    [Fact]
    public void WindowsAppStartupService_Start_SelectsTodayAndShowsMainWindow()
        => RunOnStaThread(() =>
        {
            var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
            var dataSource = new RecordingDashboardDataSource();
            var viewModel = new DashboardViewModel(
                dataSource,
                new FixedDashboardClock(now),
                new DashboardOptions("Asia/Seoul"));
            viewModel.SelectPeriod(DashboardPeriod.LastHour);
            var window = new MainWindow(viewModel, new NoopTrackingTicker());
            var startupService = new WindowsAppStartupService(window);

            try
            {
                startupService.Start();

                Assert.True(window.IsVisible);
                Assert.Equal(DashboardPeriod.Today, viewModel.SelectedPeriod);
                Assert.Equal(new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero), dataSource.LastFocusQueryStartedAtUtc);
                Assert.Equal(now, dataSource.LastFocusQueryEndedAtUtc);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void AddWindowsApp_RegistersStartupService()
        => RunOnStaThread(() =>
        {
            var services = new ServiceCollection();
            services.AddWindowsApp(new DashboardOptions("Asia/Seoul"));

            using ServiceProvider provider = services.BuildServiceProvider();
            var window = provider.GetRequiredService<MainWindow>();

            try
            {
                Assert.IsType<WindowsAppStartupService>(
                    provider.GetRequiredService<IWindowsAppStartupService>());
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

    [Fact]
    public void AddWindowsApp_RegistersDispatcherTrackingTicker()
        => RunOnStaThread(() =>
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

                Assert.IsType<DispatcherTrackingTicker>(
                    provider.GetRequiredService<ITrackingTicker>());
            }
            finally
            {
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }
            }
        });

    [Fact]
    public void AddWindowsApp_RegistersFileRuntimeLogSink()
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

            IDashboardRuntimeLogSink sink = provider.GetRequiredService<IDashboardRuntimeLogSink>();
            Assert.IsType<FileDashboardRuntimeLogSink>(sink);
            Assert.Equal(Path.Combine(Path.GetDirectoryName(dbPath)!, "logs", "windows-runtime.log"), sink.LogPath);
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
    public void DispatcherTrackingTicker_WhenIntervalEnvironmentVariableIsSet_UsesConfiguredInterval()
        => RunOnStaThread(() =>
        {
            string? previousValue = Environment.GetEnvironmentVariable(DispatcherTrackingTicker.IntervalEnvironmentVariable);
            try
            {
                Environment.SetEnvironmentVariable(DispatcherTrackingTicker.IntervalEnvironmentVariable, "3000");

                var ticker = new DispatcherTrackingTicker();

                Assert.Equal(TimeSpan.FromSeconds(3), ticker.Interval);
            }
            finally
            {
                Environment.SetEnvironmentVariable(DispatcherTrackingTicker.IntervalEnvironmentVariable, previousValue);
            }
        });

    [Fact]
    public void AddWindowsApp_WhenSampleDashboardMode_RegistersDeterministicSampleDashboardDataSource()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        try
        {
            var services = new ServiceCollection();
            services.AddWindowsApp(new WindowsAppOptions(
                new DashboardOptions("Asia/Seoul"),
                deviceId: "windows-device-1",
                localDatabaseConnectionString: $"Data Source={dbPath};Pooling=False",
                idleThreshold: TimeSpan.FromMinutes(5),
                acceptanceMode: WindowsAppAcceptanceMode.SampleDashboard,
                autoStartTracking: false));

            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.IsType<SampleDashboardDataSource>(
                provider.GetRequiredService<IDashboardDataSource>());
            Assert.IsType<NoopDashboardTrackingCoordinator>(
                provider.GetRequiredService<IDashboardTrackingCoordinator>());

            IReadOnlyList<Domain.Common.FocusSession> focusSessions = provider
                .GetRequiredService<IDashboardDataSource>()
                .QueryFocusSessions(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
            Assert.Contains(focusSessions, session => session.PlatformAppKey == "chrome.exe");
            Assert.Contains(focusSessions, session => session.PlatformAppKey == "Code.exe");
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

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Woong.MonitorStack.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private sealed class RecordingDashboardDataSource : IDashboardDataSource
    {
        public DateTimeOffset LastFocusQueryStartedAtUtc { get; private set; }

        public DateTimeOffset LastFocusQueryEndedAtUtc { get; private set; }

        public IReadOnlyList<FocusSession> QueryFocusSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
        {
            LastFocusQueryStartedAtUtc = startedAtUtc;
            LastFocusQueryEndedAtUtc = endedAtUtc;

            return [];
        }

        public IReadOnlyList<WebSession> QueryWebSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => [];
    }

    private sealed class FixedDashboardClock(DateTimeOffset utcNow) : IDashboardClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class NoopTrackingTicker : ITrackingTicker
    {
        public event EventHandler? Tick
        {
            add { }
            remove { }
        }

        public bool IsRunning { get; private set; }

        public void Start()
        {
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }
    }
}
