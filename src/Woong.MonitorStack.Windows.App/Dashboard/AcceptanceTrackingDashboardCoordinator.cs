using System.Text.Json;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public sealed class AcceptanceTrackingDashboardCoordinator : IDashboardTrackingCoordinator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string CodeSessionId = "acceptance-code-focus";
    private const string ChromeSessionId = "acceptance-chrome-focus";
    private const string GithubWebSessionId = "acceptance-github-web";
    private const string ChatGptWebSessionId = "acceptance-chatgpt-web";

    private readonly SqliteFocusSessionRepository _focusSessionRepository;
    private readonly SqliteWebSessionRepository _webSessionRepository;
    private readonly SqliteSyncOutboxRepository _outboxRepository;
    private readonly AcceptanceTrackingScenarioClock _clock;
    private readonly string _deviceId;
    private readonly string _timezoneId;
    private bool _isRunning;
    private bool _generatedForegroundChange;
    private bool _stopped;

    public AcceptanceTrackingDashboardCoordinator(
        SqliteFocusSessionRepository focusSessionRepository,
        SqliteWebSessionRepository webSessionRepository,
        SqliteSyncOutboxRepository outboxRepository,
        AcceptanceTrackingScenarioClock clock,
        string deviceId,
        string timezoneId)
    {
        _focusSessionRepository = focusSessionRepository ?? throw new ArgumentNullException(nameof(focusSessionRepository));
        _webSessionRepository = webSessionRepository ?? throw new ArgumentNullException(nameof(webSessionRepository));
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _deviceId = string.IsNullOrWhiteSpace(deviceId)
            ? throw new ArgumentException("Device id must not be empty.", nameof(deviceId))
            : deviceId;
        _timezoneId = string.IsNullOrWhiteSpace(timezoneId)
            ? throw new ArgumentException("Timezone id must not be empty.", nameof(timezoneId))
            : timezoneId;
    }

    public DashboardTrackingSnapshot StartTracking()
    {
        _isRunning = true;
        _generatedForegroundChange = false;
        _stopped = false;
        _clock.UtcNow = _clock.ScenarioStartedAtUtc;

        return new DashboardTrackingSnapshot(
            AppName: "Code.exe",
            ProcessName: "Code.exe",
            WindowTitle: null,
            CurrentSessionDuration: TimeSpan.Zero,
            LastPersistedSession: null);
    }

    public DashboardTrackingSnapshot StopTracking()
    {
        if (!_isRunning)
        {
            return DashboardTrackingSnapshot.Empty;
        }

        if (!_generatedForegroundChange)
        {
            _ = PollOnce();
        }

        DateTimeOffset endedAtUtc = _clock.ScenarioStartedAtUtc.AddMinutes(15);
        _clock.UtcNow = endedAtUtc;
        FocusSession chromeSession = CreateChromeSession();
        PersistFocusSession(chromeSession);
        PersistWebSessions();
        _isRunning = false;
        _stopped = true;

        return new DashboardTrackingSnapshot(
            AppName: "chrome.exe",
            ProcessName: "chrome.exe",
            WindowTitle: null,
            CurrentSessionDuration: TimeSpan.FromMinutes(10),
            LastPersistedSession: ToPersistedSnapshot(chromeSession));
    }

    public DashboardTrackingSnapshot PollOnce()
    {
        if (!_isRunning)
        {
            return DashboardTrackingSnapshot.Empty;
        }

        if (_generatedForegroundChange)
        {
            _clock.UtcNow = _clock.ScenarioStartedAtUtc.AddMinutes(10);

            return new DashboardTrackingSnapshot(
                AppName: "chrome.exe",
                ProcessName: "chrome.exe",
                WindowTitle: null,
                CurrentSessionDuration: TimeSpan.FromMinutes(5),
                LastPersistedSession: null);
        }

        _clock.UtcNow = _clock.ScenarioStartedAtUtc.AddMinutes(5);
        FocusSession codeSession = CreateCodeSession();
        PersistFocusSession(codeSession);
        _generatedForegroundChange = true;

        return new DashboardTrackingSnapshot(
            AppName: "chrome.exe",
            ProcessName: "chrome.exe",
            WindowTitle: null,
            CurrentSessionDuration: TimeSpan.Zero,
            LastPersistedSession: ToPersistedSnapshot(codeSession));
    }

    public DashboardSyncResult SyncNow(bool syncEnabled)
    {
        if (!syncEnabled)
        {
            return new DashboardSyncResult("Sync skipped. Enable sync to upload.");
        }

        IReadOnlyList<SyncOutboxItem> pendingItems = _outboxRepository
            .QueryAll()
            .Where(item => item.Status is SyncOutboxStatus.Pending or SyncOutboxStatus.Failed)
            .ToList();
        foreach (SyncOutboxItem item in pendingItems)
        {
            _outboxRepository.MarkSynced(item.Id, _clock.UtcNow);
        }

        return new DashboardSyncResult($"Fake sync completed. Synced {pendingItems.Count} local outbox row(s).");
    }

    private FocusSession CreateCodeSession()
        => FocusSession.FromUtc(
            CodeSessionId,
            _deviceId,
            "Code.exe",
            _clock.ScenarioStartedAtUtc,
            _clock.ScenarioStartedAtUtc.AddMinutes(5),
            _timezoneId,
            isIdle: false,
            source: "acceptance_fake_foreground",
            processId: 10,
            processName: "Code.exe",
            processPath: @"C:\Acceptance\Code.exe",
            windowHandle: 100,
            windowTitle: null);

    private FocusSession CreateChromeSession()
        => FocusSession.FromUtc(
            ChromeSessionId,
            _deviceId,
            "chrome.exe",
            _clock.ScenarioStartedAtUtc.AddMinutes(5),
            _clock.ScenarioStartedAtUtc.AddMinutes(15),
            _timezoneId,
            isIdle: false,
            source: "acceptance_fake_foreground",
            processId: 20,
            processName: "chrome.exe",
            processPath: @"C:\Acceptance\chrome.exe",
            windowHandle: 200,
            windowTitle: null);

    private void PersistFocusSession(FocusSession session)
    {
        _focusSessionRepository.Save(session);
        _outboxRepository.Add(SyncOutboxItem.Pending(
            id: $"focus-session:{session.ClientSessionId}",
            aggregateType: "focus_session",
            aggregateId: session.ClientSessionId,
            payloadJson: JsonSerializer.Serialize(
                new UploadFocusSessionsRequest(
                    _deviceId,
                    [
                        new FocusSessionUploadItem(
                            session.ClientSessionId,
                            session.PlatformAppKey,
                            session.StartedAtUtc,
                            session.EndedAtUtc,
                            session.DurationMs,
                            session.LocalDate,
                            session.TimezoneId,
                            session.IsIdle,
                            session.Source,
                            session.ProcessId,
                            session.ProcessName,
                            session.ProcessPath,
                            session.WindowHandle,
                            session.WindowTitle)
                    ]),
                JsonOptions),
            createdAtUtc: _clock.UtcNow));
    }

    private void PersistWebSessions()
    {
        if (_stopped)
        {
            return;
        }

        PersistWebSession(
            GithubWebSessionId,
            new WebSession(
                ChromeSessionId,
                "Chrome",
                url: null,
                "github.com",
                pageTitle: null,
                TimeRange.FromUtc(
                    _clock.ScenarioStartedAtUtc.AddMinutes(5),
                    _clock.ScenarioStartedAtUtc.AddMinutes(10)),
                captureMethod: "FakeTestData",
                captureConfidence: "High",
                isPrivateOrUnknown: false));
        PersistWebSession(
            ChatGptWebSessionId,
            new WebSession(
                ChromeSessionId,
                "Chrome",
                url: null,
                "chatgpt.com",
                pageTitle: null,
                TimeRange.FromUtc(
                    _clock.ScenarioStartedAtUtc.AddMinutes(10),
                    _clock.ScenarioStartedAtUtc.AddMinutes(15)),
                captureMethod: "FakeTestData",
                captureConfidence: "High",
                isPrivateOrUnknown: false));
    }

    private void PersistWebSession(string outboxId, WebSession session)
    {
        _webSessionRepository.Save(session);
        _outboxRepository.Add(SyncOutboxItem.Pending(
            id: $"web-session:{outboxId}",
            aggregateType: "web_session",
            aggregateId: outboxId,
            payloadJson: JsonSerializer.Serialize(
                new UploadWebSessionsRequest(
                    _deviceId,
                    [
                        new WebSessionUploadItem(
                            outboxId,
                            session.FocusSessionId,
                            session.BrowserFamily,
                            session.Url,
                            session.Domain,
                            session.PageTitle,
                            session.StartedAtUtc,
                            session.EndedAtUtc,
                            session.DurationMs,
                            session.CaptureMethod,
                            session.CaptureConfidence,
                            session.IsPrivateOrUnknown)
                    ]),
                JsonOptions),
            createdAtUtc: _clock.UtcNow));
    }

    private DashboardPersistedSessionSnapshot ToPersistedSnapshot(FocusSession session)
        => new(
            AppName: session.PlatformAppKey,
            ProcessName: session.ProcessName ?? session.PlatformAppKey,
            EndedAtUtc: session.EndedAtUtc,
            Duration: TimeSpan.FromMilliseconds(session.DurationMs));
}
