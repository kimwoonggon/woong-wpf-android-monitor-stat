using System.Text.Json;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public sealed class AcceptanceTrackingDashboardCoordinator : IDashboardTrackingCoordinator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly AcceptanceTrackingStep[] Steps =
    [
        new(
            FocusSessionId: "acceptance-code-focus",
            AppName: "Code.exe",
            ProcessName: "Code.exe",
            ProcessId: 10,
            ProcessPath: @"C:\Acceptance\Code.exe",
            WindowHandle: 100,
            WindowTitle: "Project Alpha - Visual Studio Code",
            StartOffset: TimeSpan.FromMinutes(0),
            EndOffset: TimeSpan.FromMinutes(3),
            BrowserFamily: null,
            Domain: null),
        new(
            FocusSessionId: "acceptance-chrome-primary-focus",
            AppName: "chrome.exe",
            ProcessName: "chrome.exe",
            ProcessId: 20,
            ProcessPath: @"C:\Acceptance\Chrome-A\chrome.exe",
            WindowHandle: 200,
            WindowTitle: "YouTube - Google Chrome",
            StartOffset: TimeSpan.FromMinutes(3),
            EndOffset: TimeSpan.FromMinutes(4),
            BrowserFamily: "Chrome",
            Domain: "youtube.com"),
        new(
            FocusSessionId: "acceptance-chrome-primary-focus",
            AppName: "chrome.exe",
            ProcessName: "chrome.exe",
            ProcessId: 20,
            ProcessPath: @"C:\Acceptance\Chrome-A\chrome.exe",
            WindowHandle: 200,
            WindowTitle: "GitHub Repo - Google Chrome",
            StartOffset: TimeSpan.FromMinutes(4),
            EndOffset: TimeSpan.FromMinutes(6),
            BrowserFamily: "Chrome",
            Domain: "github.com"),
        new(
            FocusSessionId: "acceptance-chrome-primary-focus",
            AppName: "chrome.exe",
            ProcessName: "chrome.exe",
            ProcessId: 20,
            ProcessPath: @"C:\Acceptance\Chrome-A\chrome.exe",
            WindowHandle: 200,
            WindowTitle: "ChatGPT - Google Chrome",
            StartOffset: TimeSpan.FromMinutes(6),
            EndOffset: TimeSpan.FromMinutes(9),
            BrowserFamily: "Chrome",
            Domain: "chatgpt.com"),
        new(
            FocusSessionId: "acceptance-chrome-docs-focus",
            AppName: "chrome.exe",
            ProcessName: "chrome.exe",
            ProcessId: 22,
            ProcessPath: @"C:\Acceptance\Chrome-B\chrome.exe",
            WindowHandle: 202,
            WindowTitle: "Learn Microsoft - Google Chrome",
            StartOffset: TimeSpan.FromMinutes(9),
            EndOffset: TimeSpan.FromMinutes(12),
            BrowserFamily: "Chrome",
            Domain: "learn.microsoft.com"),
        new(
            FocusSessionId: "acceptance-notepad-focus",
            AppName: "notepad.exe",
            ProcessName: "notepad.exe",
            ProcessId: 30,
            ProcessPath: @"C:\Windows\System32\notepad.exe",
            WindowHandle: 300,
            WindowTitle: "Untitled - Notepad",
            StartOffset: TimeSpan.FromMinutes(12),
            EndOffset: TimeSpan.FromMinutes(14),
            BrowserFamily: null,
            Domain: null),
        new(
            FocusSessionId: "acceptance-explorer-focus",
            AppName: "explorer.exe",
            ProcessName: "explorer.exe",
            ProcessId: 40,
            ProcessPath: @"C:\Windows\explorer.exe",
            WindowHandle: 400,
            WindowTitle: "Downloads - File Explorer",
            StartOffset: TimeSpan.FromMinutes(14),
            EndOffset: TimeSpan.FromMinutes(15),
            BrowserFamily: null,
            Domain: null)
    ];

    private readonly SqliteFocusSessionRepository _focusSessionRepository;
    private readonly SqliteWebSessionRepository _webSessionRepository;
    private readonly SqliteSyncOutboxRepository _outboxRepository;
    private readonly AcceptanceTrackingScenarioClock _clock;
    private readonly string _deviceId;
    private readonly string _timezoneId;
    private readonly HashSet<string> _persistedFocusSessionIds = [];
    private readonly HashSet<int> _persistedWebStepIndexes = [];
    private bool _isRunning;
    private int _currentStepIndex = -1;

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
        _currentStepIndex = 0;
        _persistedFocusSessionIds.Clear();
        _persistedWebStepIndexes.Clear();
        _clock.UtcNow = _clock.ScenarioStartedAtUtc;

        return ToCurrentSnapshot(
            Steps[_currentStepIndex],
            TimeSpan.Zero,
            lastPersistedSession: null,
            lastDbWriteAtUtc: null,
            hasPersistedWebSession: false);
    }

    public DashboardTrackingSnapshot StopTracking()
    {
        if (!_isRunning || _currentStepIndex < 0)
        {
            return DashboardTrackingSnapshot.Empty;
        }

        AcceptanceTrackingStep step = Steps[_currentStepIndex];
        bool persistedWebSession = PersistWebSessionIfNeeded(_currentStepIndex);
        FocusSession? persistedSession = PersistFocusSessionIfNeeded(step.FocusSessionId, _currentStepIndex);
        _isRunning = false;
        _clock.UtcNow = _clock.ScenarioStartedAtUtc + step.EndOffset;

        return ToCurrentSnapshot(
            step,
            step.EndOffset - step.StartOffset,
            persistedSession is null ? null : ToPersistedSnapshot(persistedSession),
            persistedSession?.EndedAtUtc ?? (persistedWebSession ? _clock.ScenarioStartedAtUtc + step.EndOffset : null),
            hasPersistedWebSession: persistedWebSession);
    }

    public DashboardTrackingSnapshot PollOnce()
    {
        if (!_isRunning || _currentStepIndex < 0)
        {
            return DashboardTrackingSnapshot.Empty;
        }

        if (_currentStepIndex >= Steps.Length - 1)
        {
            AcceptanceTrackingStep finalStep = Steps[_currentStepIndex];
            _clock.UtcNow = _clock.ScenarioStartedAtUtc + finalStep.StartOffset + TimeSpan.FromSeconds(30);

            return ToCurrentSnapshot(
                finalStep,
                _clock.UtcNow - (_clock.ScenarioStartedAtUtc + finalStep.StartOffset),
                lastPersistedSession: null,
                lastDbWriteAtUtc: null,
                hasPersistedWebSession: false);
        }

        int previousStepIndex = _currentStepIndex;
        AcceptanceTrackingStep previousStep = Steps[previousStepIndex];
        AcceptanceTrackingStep nextStep = Steps[previousStepIndex + 1];
        bool persistedWebSession = PersistWebSessionIfNeeded(previousStepIndex);
        FocusSession? persistedSession = previousStep.FocusSessionId == nextStep.FocusSessionId
            ? null
            : PersistFocusSessionIfNeeded(previousStep.FocusSessionId, previousStepIndex);
        _currentStepIndex++;
        AcceptanceTrackingStep currentStep = Steps[_currentStepIndex];
        _clock.UtcNow = _clock.ScenarioStartedAtUtc + currentStep.StartOffset;

        return ToCurrentSnapshot(
            currentStep,
            TimeSpan.Zero,
            persistedSession is null ? null : ToPersistedSnapshot(persistedSession),
            persistedSession?.EndedAtUtc ?? (persistedWebSession ? _clock.ScenarioStartedAtUtc + previousStep.EndOffset : null),
            hasPersistedWebSession: persistedWebSession);
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

    private FocusSession? PersistFocusSessionIfNeeded(string focusSessionId, int upToStepIndex)
    {
        if (upToStepIndex < 0 || upToStepIndex >= Steps.Length || !_persistedFocusSessionIds.Add(focusSessionId))
        {
            return null;
        }

        FocusSession focusSession = CreateFocusSession(focusSessionId, upToStepIndex);
        PersistFocusSession(focusSession);

        return focusSession;
    }

    private FocusSession CreateFocusSession(string focusSessionId, int upToStepIndex)
    {
        AcceptanceTrackingStep firstStep = Steps.First(step => step.FocusSessionId == focusSessionId);
        DateTimeOffset startedAtUtc = _clock.ScenarioStartedAtUtc
            + Steps
                .Where((step, index) => index <= upToStepIndex && step.FocusSessionId == focusSessionId)
                .Min(step => step.StartOffset);
        DateTimeOffset endedAtUtc = _clock.ScenarioStartedAtUtc
            + Steps
                .Where((step, index) => index <= upToStepIndex && step.FocusSessionId == focusSessionId)
                .Max(step => step.EndOffset);

        return FocusSession.FromUtc(
            focusSessionId,
            _deviceId,
            firstStep.AppName,
            startedAtUtc,
            endedAtUtc,
            _timezoneId,
            isIdle: false,
            source: "acceptance_fake_foreground",
            processId: firstStep.ProcessId,
            processName: firstStep.ProcessName,
            processPath: firstStep.ProcessPath,
            windowHandle: firstStep.WindowHandle,
            windowTitle: firstStep.WindowTitle);
    }

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

    private bool PersistWebSessionIfNeeded(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= Steps.Length || !_persistedWebStepIndexes.Add(stepIndex))
        {
            return false;
        }

        AcceptanceTrackingStep step = Steps[stepIndex];
        if (string.IsNullOrWhiteSpace(step.Domain) || string.IsNullOrWhiteSpace(step.BrowserFamily))
        {
            return false;
        }

        PersistWebSession(
            $"{step.FocusSessionId}-web-{stepIndex}",
            new WebSession(
                step.FocusSessionId,
                step.BrowserFamily,
                url: null,
                step.Domain,
                pageTitle: null,
                TimeRange.FromUtc(
                    _clock.ScenarioStartedAtUtc + step.StartOffset,
                    _clock.ScenarioStartedAtUtc + step.EndOffset),
                captureMethod: "FakeTestData",
                captureConfidence: "High",
                isPrivateOrUnknown: false));
        return true;
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

    private DashboardTrackingSnapshot ToCurrentSnapshot(
        AcceptanceTrackingStep step,
        TimeSpan currentSessionDuration,
        DashboardPersistedSessionSnapshot? lastPersistedSession,
        DateTimeOffset? lastDbWriteAtUtc,
        bool hasPersistedWebSession)
        => new(
            AppName: step.AppName,
            ProcessName: step.ProcessName,
            WindowTitle: step.WindowTitle,
            CurrentSessionDuration: currentSessionDuration,
            LastPersistedSession: lastPersistedSession,
            CurrentBrowserDomain: step.Domain,
            BrowserCaptureStatus: step.Domain is null
                ? DashboardBrowserCaptureStatus.Unavailable
                : DashboardBrowserCaptureStatus.ExtensionConnected,
            LastPollAtUtc: _clock.UtcNow,
            LastDbWriteAtUtc: lastDbWriteAtUtc,
            HasPersistedWebSession: hasPersistedWebSession);

    private sealed record AcceptanceTrackingStep(
        string FocusSessionId,
        string AppName,
        string ProcessName,
        int ProcessId,
        string ProcessPath,
        long WindowHandle,
        string WindowTitle,
        TimeSpan StartOffset,
        TimeSpan EndOffset,
        string? BrowserFamily,
        string? Domain);
}
