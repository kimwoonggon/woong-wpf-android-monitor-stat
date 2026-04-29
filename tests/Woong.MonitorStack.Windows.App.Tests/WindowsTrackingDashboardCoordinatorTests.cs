using System.IO;
using System.Text.Json;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.App.Dashboard;
using Woong.MonitorStack.Windows.Browser;
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
        using JsonDocument payload = JsonDocument.Parse(outbox.PayloadJson);
        JsonElement firstSession = payload.RootElement.GetProperty("sessions")[0];
        Assert.Equal(10, firstSession.GetProperty("processId").GetInt32());
        Assert.Equal("Code.exe", firstSession.GetProperty("processName").GetString());
        Assert.Equal(@"C:\Apps\Code.exe", firstSession.GetProperty("processPath").GetString());
        Assert.Equal(100, firstSession.GetProperty("windowHandle").GetInt64());
        Assert.True(firstSession.TryGetProperty("windowTitle", out JsonElement windowTitle));
        Assert.Equal(JsonValueKind.Null, windowTitle.ValueKind);
    }

    [Fact]
    public void PollOnce_WhenForegroundChanges_ReturnsLastPollAndLastDbWriteTimes()
    {
        var startedAtUtc = new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero);
        var clock = new MutableClock(startedAtUtc);
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
        clock.UtcNow = startedAtUtc.AddMinutes(10);
        foregroundReader.ForegroundWindow = new ForegroundWindowInfo(
            hwnd: 200,
            processId: 20,
            processName: "chrome.exe",
            executablePath: "C:\\Apps\\chrome.exe",
            windowTitle: "GitHub - Chrome");

        var pollSnapshot = coordinator.PollOnce();

        Assert.Equal(startedAtUtc, startSnapshot.LastPollAtUtc);
        Assert.Null(startSnapshot.LastDbWriteAtUtc);
        Assert.Equal(clock.UtcNow, pollSnapshot.LastPollAtUtc);
        Assert.Equal(clock.UtcNow, pollSnapshot.LastDbWriteAtUtc);
    }

    [Fact]
    public void PollOnce_WhenBrowserDomainChanges_PersistsCompletedWebSessionQueuesOutboxAndSignalsDashboardRefresh()
    {
        var startedAtUtc = new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero);
        var clock = new MutableClock(startedAtUtc);
        var foregroundReader = new MutableForegroundWindowReader(new ForegroundWindowInfo(
            hwnd: 200,
            processId: 20,
            processName: "chrome.exe",
            executablePath: "C:\\Apps\\chrome.exe",
            windowTitle: "GitHub - Chrome"));
        var browserReader = new MutableBrowserActivityReader(CreateBrowserSnapshot(
            capturedAtUtc: startedAtUtc,
            domain: "github.com",
            url: "https://github.com/org/repo?secret=1"));
        SqliteFocusSessionRepository focusRepository = CreateFocusRepository();
        SqliteWebSessionRepository webRepository = CreateWebSessionRepository();
        SqliteSyncOutboxRepository outboxRepository = CreateOutboxRepository();
        var coordinator = new WindowsTrackingDashboardCoordinator(
            () => new TrackingPoller(
                new ForegroundWindowCollector(foregroundReader, clock),
                new AlwaysActiveLastInputReader(),
                new IdleDetector(TimeSpan.FromMinutes(5)),
                new FocusSessionizer("windows-device-1", "Asia/Seoul")),
            focusRepository,
            webRepository,
            outboxRepository,
            clock,
            browserReader);

        coordinator.StartTracking();
        clock.UtcNow = startedAtUtc.AddMinutes(5);
        browserReader.Snapshot = CreateBrowserSnapshot(
            capturedAtUtc: clock.UtcNow,
            domain: "chatgpt.com",
            url: "https://chatgpt.com/codex");

        var pollSnapshot = coordinator.PollOnce();

        string chromeFocusSessionId = $"{foregroundReader.ForegroundWindow.ProcessId}:{foregroundReader.ForegroundWindow.Hwnd}:{startedAtUtc.ToUnixTimeMilliseconds()}";
        var saved = Assert.Single(webRepository.QueryByFocusSessionId(chromeFocusSessionId));
        Assert.Equal("github.com", saved.Domain);
        Assert.Null(saved.Url);
        Assert.Equal(300_000, saved.DurationMs);
        Assert.True(pollSnapshot.HasPersistedWebSession);
        Assert.Equal("chatgpt.com", pollSnapshot.CurrentBrowserDomain);
        Assert.Equal(clock.UtcNow, pollSnapshot.LastDbWriteAtUtc);

        SyncOutboxItem item = Assert.Single(outboxRepository.QueryAll());
        Assert.Equal("web_session", item.AggregateType);
        Assert.Equal(SyncOutboxStatus.Pending, item.Status);
        var request = JsonSerializer.Deserialize<UploadWebSessionsRequest>(
            item.PayloadJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(request);
        WebSessionUploadItem payload = Assert.Single(request.Sessions);
        Assert.Equal("windows-device-1", request.DeviceId);
        Assert.Equal("github.com", payload.Domain);
        Assert.Null(payload.Url);
        Assert.Equal(300_000, payload.DurationMs);
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
    public void SyncNow_WhenSyncIsOff_LeavesPendingOutboxRowsLocal()
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

        var result = coordinator.SyncNow(syncEnabled: false);

        Assert.Equal("Sync skipped. Enable sync to upload.", result.StatusText);
        SyncOutboxItem item = Assert.Single(outboxRepository.QueryAll());
        Assert.Equal(SyncOutboxStatus.Pending, item.Status);
        Assert.Null(item.SyncedAtUtc);
        Assert.Equal(0, item.RetryCount);
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

    private SqliteWebSessionRepository CreateWebSessionRepository()
    {
        var repository = new SqliteWebSessionRepository($"Data Source={_dbPath};Pooling=False");
        repository.Initialize();

        return repository;
    }

    private static BrowserActivitySnapshot CreateBrowserSnapshot(
        DateTimeOffset capturedAtUtc,
        string domain,
        string url)
        => new(
            capturedAtUtc,
            "Chrome",
            "chrome.exe",
            processId: 20,
            windowHandle: 200,
            windowTitle: null,
            tabTitle: "Hidden by privacy setting",
            url,
            domain,
            CaptureMethod.UIAutomationAddressBar,
            CaptureConfidence.High,
            isPrivateOrUnknown: false);

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

    private sealed class MutableBrowserActivityReader(BrowserActivitySnapshot? snapshot) : IBrowserActivityReader
    {
        public BrowserActivitySnapshot? Snapshot { get; set; } = snapshot;

        public BrowserActivitySnapshot? TryRead(ForegroundWindowSnapshot foregroundWindow)
            => Snapshot;
    }
}
