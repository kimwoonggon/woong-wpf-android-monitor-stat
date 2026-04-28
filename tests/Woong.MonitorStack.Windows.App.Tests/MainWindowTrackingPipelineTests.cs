using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Woong.MonitorStack.Windows.App.Dashboard;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using Woong.MonitorStack.Windows.Storage;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class MainWindowTrackingPipelineTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    [Fact]
    public void StartStopButtons_PersistForegroundSessionsAndDashboardRendersFromSqlite()
        => RunOnStaThread(() =>
        {
            var clock = new MutableClock(new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));
            var foregroundReader = new MutableForegroundWindowReader(new ForegroundWindowInfo(
                hwnd: 100,
                processId: 10,
                processName: "Code.exe",
                executablePath: "C:\\Apps\\Code.exe",
                windowTitle: "Project - Visual Studio Code"));
            SqliteFocusSessionRepository focusRepository = CreateFocusRepository();
            SqliteWebSessionRepository webRepository = CreateWebRepository();
            SqliteSyncOutboxRepository outboxRepository = CreateOutboxRepository();
            var coordinator = new WindowsTrackingDashboardCoordinator(
                () => new TrackingPoller(
                    new ForegroundWindowCollector(foregroundReader, clock),
                    new AlwaysActiveLastInputReader(),
                    new IdleDetector(TimeSpan.FromMinutes(5)),
                    new FocusSessionizer("windows-device-1", "Asia/Seoul")),
                focusRepository,
                outboxRepository,
                clock);
            var viewModel = new DashboardViewModel(
                new SqliteDashboardDataSource(focusRepository, webRepository),
                clock,
                new DashboardOptions("Asia/Seoul"),
                coordinator);
            var window = new MainWindow(viewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                InvokeButton(FindByAutomationId<Button>(window, "StartTrackingButton"));
                window.UpdateLayout();
                Assert.Equal("Running", FindByAutomationId<TextBlock>(window, "TrackingStatusText").Text);
                Assert.Equal("Code.exe", FindByAutomationId<TextBlock>(window, "CurrentAppNameText").Text);

                clock.UtcNow = clock.UtcNow.AddMinutes(5);
                foregroundReader.ForegroundWindow = new ForegroundWindowInfo(
                    hwnd: 200,
                    processId: 20,
                    processName: "chrome.exe",
                    executablePath: "C:\\Apps\\chrome.exe",
                    windowTitle: "GitHub - Chrome");

                InvokeButton(FindByAutomationId<Button>(window, "StopTrackingButton"));
                window.UpdateLayout();

                IReadOnlyList<Woong.MonitorStack.Domain.Common.FocusSession> persistedSessions =
                    focusRepository.QueryByRange(
                        new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
                        new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero));
                Assert.Contains(persistedSessions, session => session.PlatformAppKey == "Code.exe" && session.DurationMs == 300_000);
                Assert.Contains(persistedSessions, session => session.PlatformAppKey == "chrome.exe");
                Assert.Equal(2, outboxRepository.QueryAll().Count);

                Assert.Equal("Stopped", FindByAutomationId<TextBlock>(window, "TrackingStatusText").Text);
                Assert.Equal("chrome.exe", FindByAutomationId<TextBlock>(window, "CurrentAppNameText").Text);
                Assert.Equal("Code.exe", viewModel.TopAppName);
                Assert.Contains(viewModel.SummaryCards, card => card.Label == "Active" && card.Value == "5m");
                Assert.Contains(viewModel.RecentSessions, row => row.AppName == "Code.exe" && row.Duration == "5m");
                Assert.Contains(
                    FindByAutomationId<DataGrid>(window, "RecentAppSessionsList").ItemsSource.Cast<DashboardSessionRow>(),
                    row => row.AppName == "Code.exe" && row.Duration == "5m");
            }
            finally
            {
                window.Close();
            }
        });

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private SqliteFocusSessionRepository CreateFocusRepository()
    {
        var repository = new SqliteFocusSessionRepository($"Data Source={_dbPath};Pooling=False");
        repository.Initialize();

        return repository;
    }

    private SqliteWebSessionRepository CreateWebRepository()
    {
        var repository = new SqliteWebSessionRepository($"Data Source={_dbPath};Pooling=False");
        repository.Initialize();

        return repository;
    }

    private SqliteSyncOutboxRepository CreateOutboxRepository()
    {
        var repository = new SqliteSyncOutboxRepository($"Data Source={_dbPath};Pooling=False");
        repository.Initialize();

        return repository;
    }

    private static void InvokeButton(Button button)
    {
        Assert.True(button.IsEnabled);
        var peer = new ButtonAutomationPeer(button);
        var invokeProvider = (IInvokeProvider?)peer.GetPattern(PatternInterface.Invoke)
            ?? throw new InvalidOperationException($"Button `{AutomationProperties.GetAutomationId(button)}` does not expose Invoke.");

        invokeProvider.Invoke();
        DrainDispatcher();
    }

    private static void DrainDispatcher()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private static T FindByAutomationId<T>(DependencyObject root, string automationId)
        where T : DependencyObject
    {
        if (root is T candidate && AutomationProperties.GetAutomationId(root) == automationId)
        {
            return candidate;
        }

        foreach (DependencyObject child in GetChildren(root))
        {
            try
            {
                return FindByAutomationId<T>(child, automationId);
            }
            catch (InvalidOperationException)
            {
            }
        }

        throw new InvalidOperationException($"Could not find {typeof(T).Name} with AutomationId '{automationId}'.");
    }

    private static IEnumerable<DependencyObject> GetChildren(DependencyObject root)
    {
        int visualChildCount = 0;
        try
        {
            visualChildCount = VisualTreeHelper.GetChildrenCount(root);
        }
        catch (InvalidOperationException)
        {
        }

        for (var index = 0; index < visualChildCount; index++)
        {
            yield return VisualTreeHelper.GetChild(root, index);
        }

        foreach (object child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is DependencyObject dependencyObject)
            {
                yield return dependencyObject;
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

    private sealed class MutableClock(DateTimeOffset utcNow) : ISystemClock, IDashboardClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    private sealed class MutableForegroundWindowReader(ForegroundWindowInfo foregroundWindow) : IForegroundWindowReader
    {
        public ForegroundWindowInfo ForegroundWindow { get; set; } = foregroundWindow;

        public ForegroundWindowInfo ReadForegroundWindow()
            => ForegroundWindow;
    }

    private sealed class AlwaysActiveLastInputReader : ILastInputReader
    {
        public DateTimeOffset ReadLastInputAtUtc(DateTimeOffset nowUtc)
            => nowUtc;
    }
}
