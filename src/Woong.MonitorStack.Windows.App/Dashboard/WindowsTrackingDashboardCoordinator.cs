using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Browser;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using Woong.MonitorStack.Windows.Storage;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public sealed class WindowsTrackingDashboardCoordinator : IDashboardTrackingCoordinator
{
    private readonly Func<TrackingPoller> _trackingPollerFactory;
    private readonly WindowsFocusSessionPersistenceService _focusSessionPersistenceService;
    private readonly WindowsWebSessionPersistenceService? _webSessionPersistenceService;
    private readonly ISystemClock _clock;
    private readonly IBrowserActivityReader? _browserActivityReader;
    private readonly IBrowserUrlSanitizer _browserUrlSanitizer;
    private readonly BrowserUrlStoragePolicy _browserStoragePolicy;
    private TrackingPoller? _trackingPoller;
    private BrowserWebSessionizer? _webSessionizer;
    private string? _webSessionizerFocusSessionId;
    private bool _isRunning;
    private DateTimeOffset? _lastDbWriteAtUtc;

    public WindowsTrackingDashboardCoordinator(
        Func<TrackingPoller> trackingPollerFactory,
        SqliteFocusSessionRepository focusSessionRepository,
        SqliteSyncOutboxRepository outboxRepository,
        ISystemClock clock)
        : this(
            trackingPollerFactory,
            new WindowsFocusSessionPersistenceService(focusSessionRepository, outboxRepository, clock),
            webSessionPersistenceService: null,
            clock,
            browserActivityReader: null)
    {
    }

    public WindowsTrackingDashboardCoordinator(
        Func<TrackingPoller> trackingPollerFactory,
        SqliteFocusSessionRepository focusSessionRepository,
        SqliteWebSessionRepository? webSessionRepository,
        SqliteSyncOutboxRepository outboxRepository,
        ISystemClock clock,
        IBrowserActivityReader? browserActivityReader,
        IBrowserUrlSanitizer? browserUrlSanitizer = null,
        BrowserUrlStoragePolicy browserStoragePolicy = BrowserUrlStoragePolicy.DomainOnly)
        : this(
            trackingPollerFactory,
            new WindowsFocusSessionPersistenceService(focusSessionRepository, outboxRepository, clock),
            webSessionRepository is null
                ? null
                : new WindowsWebSessionPersistenceService(webSessionRepository, outboxRepository, clock, browserStoragePolicy),
            clock,
            browserActivityReader,
            browserUrlSanitizer,
            browserStoragePolicy)
    {
    }

    public WindowsTrackingDashboardCoordinator(
        Func<TrackingPoller> trackingPollerFactory,
        WindowsFocusSessionPersistenceService focusSessionPersistenceService,
        WindowsWebSessionPersistenceService? webSessionPersistenceService,
        ISystemClock clock,
        IBrowserActivityReader? browserActivityReader,
        IBrowserUrlSanitizer? browserUrlSanitizer = null,
        BrowserUrlStoragePolicy browserStoragePolicy = BrowserUrlStoragePolicy.DomainOnly)
    {
        _trackingPollerFactory = trackingPollerFactory ?? throw new ArgumentNullException(nameof(trackingPollerFactory));
        _focusSessionPersistenceService = focusSessionPersistenceService ?? throw new ArgumentNullException(nameof(focusSessionPersistenceService));
        _webSessionPersistenceService = webSessionPersistenceService;
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _browserActivityReader = browserActivityReader;
        _browserUrlSanitizer = browserUrlSanitizer ?? new BrowserUrlSanitizer();
        _browserStoragePolicy = browserStoragePolicy;
    }

    public DashboardTrackingSnapshot StartTracking()
    {
        _trackingPoller = _trackingPollerFactory();
        _isRunning = true;
        DateTimeOffset pollAtUtc = _clock.UtcNow;
        FocusSessionizerResult result = _trackingPoller.Poll();
        DashboardPersistedSessionSnapshot? persisted = PersistIfPresent(result.ClosedSession);
        BrowserPersistenceResult browserPersistence = PersistBrowserActivity(result);

        return ToSnapshot(
            result.CurrentSession,
            persisted,
            pollAtUtc,
            ResolveSnapshotDbWriteTime(persisted, browserPersistence.HasPersistedWebSession),
            browserPersistence.CurrentDomain,
            browserPersistence.CurrentWebSessionStartedAtUtc,
            browserPersistence.CurrentWebSessionDuration,
            browserPersistence.CaptureStatus,
            browserPersistence.HasPersistedWebSession);
    }

    public DashboardTrackingSnapshot StopTracking()
    {
        if (!_isRunning)
        {
            return DashboardTrackingSnapshot.Empty;
        }

        DateTimeOffset pollAtUtc = _clock.UtcNow;
        FocusSessionizerResult result = RequirePoller().Poll();
        DashboardPersistedSessionSnapshot? persisted = PersistIfPresent(result.ClosedSession);
        bool closedFocusWebSession = CompleteCurrentWebSessionForClosedFocus(result.ClosedSession);
        BrowserPersistenceResult browserPersistence = PersistBrowserActivity(result);
        bool stoppedWebSession = CompleteCurrentWebSession(pollAtUtc, result.CurrentSession.DeviceId);
        persisted = Persist(result.CurrentSession);
        _isRunning = false;
        _trackingPoller = null;
        _webSessionizer = null;
        _webSessionizerFocusSessionId = null;

        return ToSnapshot(
            result.CurrentSession,
            persisted,
            pollAtUtc,
            _lastDbWriteAtUtc,
            browserPersistence.CurrentDomain,
            browserPersistence.CurrentWebSessionStartedAtUtc,
            browserPersistence.CurrentWebSessionDuration,
            browserPersistence.CaptureStatus,
            closedFocusWebSession || browserPersistence.HasPersistedWebSession || stoppedWebSession);
    }

    public DashboardTrackingSnapshot PollOnce()
    {
        if (!_isRunning)
        {
            return DashboardTrackingSnapshot.Empty;
        }

        DateTimeOffset pollAtUtc = _clock.UtcNow;
        FocusSessionizerResult result = RequirePoller().Poll();
        DashboardPersistedSessionSnapshot? persisted = PersistIfPresent(result.ClosedSession);
        bool closedFocusWebSession = CompleteCurrentWebSessionForClosedFocus(result.ClosedSession);
        BrowserPersistenceResult browserPersistence = PersistBrowserActivity(result);
        bool hasPersistedWebSession = closedFocusWebSession || browserPersistence.HasPersistedWebSession;

        return ToSnapshot(
            result.CurrentSession,
            persisted,
            pollAtUtc,
            ResolveSnapshotDbWriteTime(persisted, hasPersistedWebSession),
            browserPersistence.CurrentDomain,
            browserPersistence.CurrentWebSessionStartedAtUtc,
            browserPersistence.CurrentWebSessionDuration,
            browserPersistence.CaptureStatus,
            hasPersistedWebSession);
    }

    public DashboardSyncResult SyncNow(bool syncEnabled)
        => syncEnabled
            ? new DashboardSyncResult("Sync requested. Pending local outbox rows are ready for upload.")
            : new DashboardSyncResult("Sync skipped. Enable sync to upload.");

    private DashboardPersistedSessionSnapshot? PersistIfPresent(FocusSession? session)
        => session is null ? null : Persist(session);

    private TrackingPoller RequirePoller()
        => _trackingPoller ?? throw new InvalidOperationException("Tracking has not been started.");

    private DashboardPersistedSessionSnapshot Persist(FocusSession session)
    {
        WindowsFocusSessionPersistenceResult result = _focusSessionPersistenceService.SaveFocusSession(session);
        _lastDbWriteAtUtc = result.PersistedAtUtc;
        FocusSession persistedSession = result.Session;

        return new DashboardPersistedSessionSnapshot(
            AppName: persistedSession.PlatformAppKey,
            ProcessName: persistedSession.PlatformAppKey,
            EndedAtUtc: persistedSession.EndedAtUtc,
            Duration: TimeSpan.FromMilliseconds(persistedSession.DurationMs));
    }

    private BrowserPersistenceResult PersistBrowserActivity(FocusSessionizerResult result)
    {
        if (_webSessionPersistenceService is null ||
            _browserActivityReader is null ||
            result.ForegroundWindow is null)
        {
            return BrowserPersistenceResult.Empty;
        }

        BrowserActivitySnapshot? rawSnapshot;
        try
        {
            rawSnapshot = _browserActivityReader.TryRead(result.ForegroundWindow);
        }
        catch (Exception)
        {
            return BrowserPersistenceResult.Error;
        }

        if (rawSnapshot is null)
        {
            return BrowserPersistenceResult.Empty;
        }

        BrowserActivitySnapshot sanitizedSnapshot = _browserUrlSanitizer.Sanitize(rawSnapshot, _browserStoragePolicy);
        BrowserWebSessionizer sessionizer = GetWebSessionizer(result.CurrentSession.ClientSessionId);
        IReadOnlyList<WebSession> completedSessions = sessionizer.Apply(sanitizedSnapshot);
        foreach (WebSession session in completedSessions)
        {
            PersistWebSession(session, result.CurrentSession.DeviceId);
        }

        WebSession? currentPreview = sessionizer.PreviewCurrent(sanitizedSnapshot.CapturedAtUtc);
        return new BrowserPersistenceResult(
            sanitizedSnapshot.Domain,
            currentPreview?.StartedAtUtc,
            currentPreview is null ? TimeSpan.Zero : currentPreview.Range.Duration,
            MapBrowserCaptureStatus(sanitizedSnapshot),
            completedSessions.Count > 0);
    }

    private BrowserWebSessionizer GetWebSessionizer(string focusSessionId)
    {
        if (_webSessionizer is null || !string.Equals(_webSessionizerFocusSessionId, focusSessionId, StringComparison.Ordinal))
        {
            _webSessionizer = new BrowserWebSessionizer(focusSessionId);
            _webSessionizerFocusSessionId = focusSessionId;
        }

        return _webSessionizer;
    }

    private void PersistWebSession(WebSession session, string deviceId)
    {
        if (_webSessionPersistenceService is null)
        {
            return;
        }

        WindowsWebSessionPersistenceResult result = _webSessionPersistenceService.SaveWebSession(session, deviceId);
        _lastDbWriteAtUtc = result.PersistedAtUtc;
    }

    private bool CompleteCurrentWebSession(DateTimeOffset endedAtUtc, string deviceId)
    {
        if (_webSessionizer is null)
        {
            return false;
        }

        IReadOnlyList<WebSession> completedSessions = _webSessionizer.CompleteCurrent(endedAtUtc);
        foreach (WebSession session in completedSessions)
        {
            PersistWebSession(session, deviceId);
        }

        return completedSessions.Count > 0;
    }

    private bool CompleteCurrentWebSessionForClosedFocus(FocusSession? closedSession)
    {
        if (closedSession is null ||
            _webSessionizer is null ||
            !string.Equals(_webSessionizerFocusSessionId, closedSession.ClientSessionId, StringComparison.Ordinal))
        {
            return false;
        }

        bool completed = CompleteCurrentWebSession(closedSession.EndedAtUtc, closedSession.DeviceId);
        _webSessionizer = null;
        _webSessionizerFocusSessionId = null;
        return completed;
    }

    private DateTimeOffset? ResolveSnapshotDbWriteTime(
        DashboardPersistedSessionSnapshot? persistedSession,
        bool hasPersistedWebSession)
        => persistedSession is null && !hasPersistedWebSession
            ? null
            : _lastDbWriteAtUtc;

    private static DashboardTrackingSnapshot ToSnapshot(
        FocusSession session,
        DashboardPersistedSessionSnapshot? persistedSession,
        DateTimeOffset lastPollAtUtc,
        DateTimeOffset? lastDbWriteAtUtc,
        string? currentBrowserDomain = null,
        DateTimeOffset? currentWebSessionStartedAtUtc = null,
        TimeSpan currentWebSessionDuration = default,
        DashboardBrowserCaptureStatus browserCaptureStatus = DashboardBrowserCaptureStatus.Unavailable,
        bool hasPersistedWebSession = false)
        => new(
            AppName: session.PlatformAppKey,
            ProcessName: session.ProcessName ?? session.PlatformAppKey,
            WindowTitle: session.WindowTitle,
            CurrentSessionDuration: TimeSpan.FromMilliseconds(session.DurationMs),
            LastPersistedSession: persistedSession,
            CurrentBrowserDomain: currentBrowserDomain,
            CurrentWebSessionStartedAtUtc: currentWebSessionStartedAtUtc,
            CurrentWebSessionDuration: currentWebSessionDuration,
            BrowserCaptureStatus: browserCaptureStatus,
            LastPollAtUtc: lastPollAtUtc,
            LastDbWriteAtUtc: lastDbWriteAtUtc,
            HasPersistedWebSession: hasPersistedWebSession);

    private static DashboardBrowserCaptureStatus MapBrowserCaptureStatus(BrowserActivitySnapshot snapshot)
        => snapshot.CaptureMethod switch
        {
            CaptureMethod.BrowserExtensionFuture => DashboardBrowserCaptureStatus.ExtensionConnected,
            CaptureMethod.UIAutomationAddressBar => DashboardBrowserCaptureStatus.UiAutomationFallbackActive,
            _ => DashboardBrowserCaptureStatus.Unavailable
        };

    private sealed record BrowserPersistenceResult(
        string? CurrentDomain,
        DateTimeOffset? CurrentWebSessionStartedAtUtc,
        TimeSpan CurrentWebSessionDuration,
        DashboardBrowserCaptureStatus CaptureStatus,
        bool HasPersistedWebSession)
    {
        public static BrowserPersistenceResult Empty { get; } = new(
            null,
            null,
            TimeSpan.Zero,
            DashboardBrowserCaptureStatus.Unavailable,
            HasPersistedWebSession: false);

        public static BrowserPersistenceResult Error { get; } = new(
            null,
            null,
            TimeSpan.Zero,
            DashboardBrowserCaptureStatus.Error,
            HasPersistedWebSession: false);
    }
}
