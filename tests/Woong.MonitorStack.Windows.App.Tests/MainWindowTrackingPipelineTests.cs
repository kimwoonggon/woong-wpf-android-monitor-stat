using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.App.Dashboard;
using Woong.MonitorStack.Windows.Browser;
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
                Assert.Contains(viewModel.SummaryCards, card => card.Label == "Active Focus" && card.Value == "5m");
                Assert.Contains(viewModel.SummaryCards, card => card.Label == "Foreground" && card.Value == "5m");
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

    [Fact]
    public void CurrentSessionDuration_WhenPollTicks_AdvancesBeyondZero()
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
            var viewModel = new DashboardViewModel(
                new SqliteDashboardDataSource(focusRepository, CreateWebRepository()),
                clock,
                new DashboardOptions("Asia/Seoul"),
                new WindowsTrackingDashboardCoordinator(
                    () => new TrackingPoller(
                        new ForegroundWindowCollector(foregroundReader, clock),
                        new AlwaysActiveLastInputReader(),
                        new IdleDetector(TimeSpan.FromMinutes(5)),
                        new FocusSessionizer("windows-device-1", "Asia/Seoul")),
                    focusRepository,
                    CreateOutboxRepository(),
                    clock));
            var window = new MainWindow(viewModel);

            try
            {
                window.Show();
                window.UpdateLayout();
                InvokeButton(FindByAutomationId<Button>(window, "StartTrackingButton"));

                clock.UtcNow = clock.UtcNow.AddSeconds(12);
                WaitForDispatcherTimerTick();
                window.UpdateLayout();

                Assert.Equal("Running", FindByAutomationId<TextBlock>(window, "TrackingStatusText").Text);
                Assert.Equal("00:00:12", FindByAutomationId<TextBlock>(window, "CurrentSessionDurationText").Text);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void MainWindow_WithFakeBrowserPipeline_ShowsGithubAndChatgptInWebSessions()
        => RunOnStaThread(() =>
        {
            var now = new DateTimeOffset(2026, 4, 28, 0, 12, 0, TimeSpan.Zero);
            SqliteFocusSessionRepository focusRepository = CreateFocusRepository();
            SqliteWebSessionRepository webRepository = CreateWebRepository();
            focusRepository.Save(FocusSession.FromUtc(
                "focus-browser-1",
                "windows-device-1",
                "chrome.exe",
                now.AddMinutes(-12),
                now,
                "Asia/Seoul",
                isIdle: false,
                "foreground_window"));
            var webSessionizer = new BrowserWebSessionizer("focus-browser-1");
            PersistCompletedWebSessions(webRepository, webSessionizer.Apply(CreateBrowserSnapshot(
                now.AddMinutes(-11),
                "https://github.com/org/repo",
                "github.com",
                "Repository")));
            PersistCompletedWebSessions(webRepository, webSessionizer.Apply(CreateBrowserSnapshot(
                now.AddMinutes(-6),
                "https://chatgpt.com/codex",
                "chatgpt.com",
                "ChatGPT")));
            PersistCompletedWebSessions(webRepository, webSessionizer.Apply(CreateBrowserSnapshot(
                now.AddMinutes(-1),
                "https://learn.microsoft.com/dotnet",
                "microsoft.com",
                ".NET")));
            var viewModel = new DashboardViewModel(
                new SqliteDashboardDataSource(focusRepository, webRepository),
                new MutableClock(now),
                new DashboardOptions("Asia/Seoul"));
            var window = new MainWindow(viewModel);

            try
            {
                window.Show();
                window.UpdateLayout();
                InvokeButton(FindByAutomationId<Button>(window, "RefreshButton"));
                window.UpdateLayout();

                Assert.Contains(viewModel.RecentWebSessions, row => row.Domain == "github.com");
                Assert.Contains(viewModel.RecentWebSessions, row => row.Domain == "chatgpt.com");
                Assert.Contains(
                    FindByAutomationId<DataGrid>(window, "RecentWebSessionsList").ItemsSource.Cast<DashboardWebSessionRow>(),
                    row => row.Domain == "github.com");
                Assert.Contains(
                    FindByAutomationId<DataGrid>(window, "RecentWebSessionsList").ItemsSource.Cast<DashboardWebSessionRow>(),
                    row => row.Domain == "chatgpt.com");
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void MainWindow_AtMinimumSize_KeepsTabsReachableOrProvidesScrolling()
        => RunOnStaThread(() =>
        {
            var viewModel = new DashboardViewModel(
                new EmptyDashboardDataSource(),
                new MutableClock(new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero)),
                new DashboardOptions("Asia/Seoul"));
            var window = new MainWindow(viewModel)
            {
                Width = 860,
                Height = 560
            };

            try
            {
                window.Show();
                window.UpdateLayout();

                var scrollViewer = FindVisualDescendant<ScrollViewer>(window);
                Assert.Equal(ScrollBarVisibility.Auto, scrollViewer.VerticalScrollBarVisibility);
                Assert.Equal(ScrollBarVisibility.Auto, scrollViewer.HorizontalScrollBarVisibility);
                Assert.NotNull(FindByAutomationId<TabControl>(window, "DashboardTabs"));
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

    private static void WaitForDispatcherTimerTick()
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1_100)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            frame.Continue = false;
        };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    private static void PersistCompletedWebSessions(
        SqliteWebSessionRepository repository,
        IEnumerable<WebSession> completedSessions)
    {
        foreach (WebSession session in completedSessions)
        {
            repository.Save(session);
        }
    }

    private static BrowserActivitySnapshot CreateBrowserSnapshot(
        DateTimeOffset capturedAtUtc,
        string url,
        string domain,
        string tabTitle)
        => new(
            capturedAtUtc,
            browserName: "Chrome",
            processName: "chrome.exe",
            processId: 20,
            windowHandle: 200,
            windowTitle: "Chrome",
            tabTitle: tabTitle,
            url: url,
            domain: domain,
            CaptureMethod.FakeTestData,
            CaptureConfidence.High,
            isPrivateOrUnknown: false);

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

    private static T FindVisualDescendant<T>(DependencyObject root)
        where T : DependencyObject
    {
        if (root is T current)
        {
            return current;
        }

        foreach (DependencyObject child in GetChildren(root))
        {
            try
            {
                return FindVisualDescendant<T>(child);
            }
            catch (InvalidOperationException)
            {
            }
        }

        throw new InvalidOperationException($"Could not find visual descendant {typeof(T).Name}.");
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

    private sealed class EmptyDashboardDataSource : IDashboardDataSource
    {
        public IReadOnlyList<FocusSession> QueryFocusSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => [];

        public IReadOnlyList<WebSession> QueryWebSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => [];
    }
}
