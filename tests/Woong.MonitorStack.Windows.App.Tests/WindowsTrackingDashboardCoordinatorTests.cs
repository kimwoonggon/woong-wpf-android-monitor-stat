using System.IO;
using Woong.MonitorStack.Windows.App.Dashboard;
using Woong.MonitorStack.Windows.Storage;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class WindowsTrackingDashboardCoordinatorTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    [Fact]
    public void PollOnce_WhenForegroundChanges_PersistsClosedSessionAndQueuesOutbox()
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));
        var foregroundReader = new MutableForegroundWindowReader(new ForegroundWindowInfo(
            hwnd: 100,
            processId: 10,
            processName: "Code.exe",
            executablePath: "C:\\Apps\\Code.exe",
            windowTitle: "Project - Visual Studio Code"));
        SqliteFocusSessionRepository focusRepository = CreateFocusRepository();
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

        var startSnapshot = coordinator.StartTracking();
        clock.UtcNow = clock.UtcNow.AddMinutes(10);
        foregroundReader.ForegroundWindow = new ForegroundWindowInfo(
            hwnd: 200,
            processId: 20,
            processName: "chrome.exe",
            executablePath: "C:\\Apps\\chrome.exe",
            windowTitle: "GitHub - Chrome");

        var chromeSnapshot = coordinator.PollOnce();

        Assert.Equal("Code.exe", startSnapshot.AppName);
        Assert.Equal("chrome.exe", chromeSnapshot.AppName);
        Assert.Equal("Code.exe persisted at 09:10 for 10m", chromeSnapshot.LastPersistedSession?.ToDisplayText("Asia/Seoul"));
        var saved = Assert.Single(focusRepository.QueryByRange(
            new DateTimeOffset(2026, 4, 27, 23, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero)));
        Assert.Equal("Code.exe", saved.PlatformAppKey);
        Assert.Equal(600_000, saved.DurationMs);
        SyncOutboxItem outbox = Assert.Single(outboxRepository.QueryAll());
        Assert.Equal("focus_session", outbox.AggregateType);
        Assert.Equal(saved.ClientSessionId, outbox.AggregateId);
        Assert.Contains(saved.ClientSessionId, outbox.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public void StopTracking_FlushesCurrentSessionToSqliteAndOutbox()
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));
        var foregroundReader = new MutableForegroundWindowReader(new ForegroundWindowInfo(
            hwnd: 100,
            processId: 10,
            processName: "Code.exe",
            executablePath: "C:\\Apps\\Code.exe",
            windowTitle: "Project - Visual Studio Code"));
        SqliteFocusSessionRepository focusRepository = CreateFocusRepository();
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

        coordinator.StartTracking();
        clock.UtcNow = clock.UtcNow.AddMinutes(2);

        var stopped = coordinator.StopTracking();

        Assert.Equal("Code.exe", stopped.LastPersistedSession?.AppName);
        Assert.Single(focusRepository.QueryByRange(
            new DateTimeOffset(2026, 4, 27, 23, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero)));
        Assert.Single(outboxRepository.QueryAll());
    }

    [Fact]
    public void StartAfterStop_DoesNotExtendPreviousStoppedSession()
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));
        var foregroundReader = new MutableForegroundWindowReader(new ForegroundWindowInfo(
            hwnd: 100,
            processId: 10,
            processName: "Code.exe",
            executablePath: "C:\\Apps\\Code.exe",
            windowTitle: "Project - Visual Studio Code"));
        SqliteFocusSessionRepository focusRepository = CreateFocusRepository();
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

        coordinator.StartTracking();
        clock.UtcNow = clock.UtcNow.AddMinutes(2);
        coordinator.StopTracking();
        clock.UtcNow = clock.UtcNow.AddMinutes(5);

        var restarted = coordinator.StartTracking();

        Assert.Equal("00:00:00", FormatClockDuration(restarted.CurrentSessionDuration));
        Assert.Single(focusRepository.QueryByRange(
            new DateTimeOffset(2026, 4, 27, 23, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero)));
    }

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

    private SqliteSyncOutboxRepository CreateOutboxRepository()
    {
        var repository = new SqliteSyncOutboxRepository($"Data Source={_dbPath};Pooling=False");
        repository.Initialize();

        return repository;
    }

    private static string FormatClockDuration(TimeSpan duration)
        => $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";

    private sealed class MutableClock(DateTimeOffset utcNow) : ISystemClock
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
