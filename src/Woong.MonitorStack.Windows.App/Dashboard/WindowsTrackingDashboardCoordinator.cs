using System.Globalization;
using System.Text.Json;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.Browser;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using Woong.MonitorStack.Windows.Storage;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public sealed class WindowsTrackingDashboardCoordinator : IDashboardTrackingCoordinator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Func<TrackingPoller> _trackingPollerFactory;
    private readonly SqliteFocusSessionRepository _focusSessionRepository;
    private readonly SqliteWebSessionRepository? _webSessionRepository;
    private readonly SqliteSyncOutboxRepository _outboxRepository;
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
            focusSessionRepository,
            webSessionRepository: null,
            outboxRepository,
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
    {
        _trackingPollerFactory = trackingPollerFactory ?? throw new ArgumentNullException(nameof(trackingPollerFactory));
        _focusSessionRepository = focusSessionRepository ?? throw new ArgumentNullException(nameof(focusSessionRepository));
        _webSessionRepository = webSessionRepository;
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
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
            ResolveSnapshotDbWriteTime(persisted, browserPersistence),
            browserPersistence.CurrentDomain,
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
        BrowserPersistenceResult browserPersistence = PersistBrowserActivity(result);
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
            browserPersistence.CaptureStatus,
            browserPersistence.HasPersistedWebSession);
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
        BrowserPersistenceResult browserPersistence = PersistBrowserActivity(result);

        return ToSnapshot(
            result.CurrentSession,
            persisted,
            pollAtUtc,
            ResolveSnapshotDbWriteTime(persisted, browserPersistence),
            browserPersistence.CurrentDomain,
            browserPersistence.CaptureStatus,
            browserPersistence.HasPersistedWebSession);
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
        _focusSessionRepository.Save(session);
        _lastDbWriteAtUtc = _clock.UtcNow;
        _outboxRepository.Add(SyncOutboxItem.Pending(
            id: $"focus-session:{session.ClientSessionId}",
            aggregateType: "focus_session",
            aggregateId: session.ClientSessionId,
            payloadJson: CreatePayload(session),
            createdAtUtc: _lastDbWriteAtUtc.Value));

        return new DashboardPersistedSessionSnapshot(
            AppName: session.PlatformAppKey,
            ProcessName: session.PlatformAppKey,
            EndedAtUtc: session.EndedAtUtc,
            Duration: TimeSpan.FromMilliseconds(session.DurationMs));
    }

    private BrowserPersistenceResult PersistBrowserActivity(FocusSessionizerResult result)
    {
        if (_webSessionRepository is null ||
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

        return new BrowserPersistenceResult(
            sanitizedSnapshot.Domain,
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
        if (_webSessionRepository is null)
        {
            return;
        }

        _webSessionRepository.Save(session);
        _lastDbWriteAtUtc = _clock.UtcNow;
        string aggregateId = CreateWebSessionAggregateId(session);
        _outboxRepository.Add(SyncOutboxItem.Pending(
            id: $"web-session:{aggregateId}",
            aggregateType: "web_session",
            aggregateId,
            payloadJson: CreatePayload(session, deviceId),
            createdAtUtc: _lastDbWriteAtUtc.Value));
    }

    private DateTimeOffset? ResolveSnapshotDbWriteTime(
        DashboardPersistedSessionSnapshot? persistedSession,
        BrowserPersistenceResult browserPersistence)
        => persistedSession is null && !browserPersistence.HasPersistedWebSession
            ? null
            : _lastDbWriteAtUtc;

    private static DashboardTrackingSnapshot ToSnapshot(
        FocusSession session,
        DashboardPersistedSessionSnapshot? persistedSession,
        DateTimeOffset lastPollAtUtc,
        DateTimeOffset? lastDbWriteAtUtc,
        string? currentBrowserDomain = null,
        DashboardBrowserCaptureStatus browserCaptureStatus = DashboardBrowserCaptureStatus.Unavailable,
        bool hasPersistedWebSession = false)
        => new(
            AppName: session.PlatformAppKey,
            ProcessName: session.ProcessName ?? session.PlatformAppKey,
            WindowTitle: session.WindowTitle,
            CurrentSessionDuration: TimeSpan.FromMilliseconds(session.DurationMs),
            LastPersistedSession: persistedSession,
            CurrentBrowserDomain: currentBrowserDomain,
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

    private static string CreatePayload(FocusSession session)
    {
        var request = new UploadFocusSessionsRequest(
            session.DeviceId,
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
            ]);

        return JsonSerializer.Serialize(request, JsonOptions);
    }

    private string CreatePayload(WebSession session, string deviceId)
    {
        var request = new UploadWebSessionsRequest(
            deviceId,
            [
                new WebSessionUploadItem(
                    CreateWebSessionAggregateId(session),
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
            ]);

        return JsonSerializer.Serialize(request, JsonOptions);
    }

    private static string CreateWebSessionAggregateId(WebSession session)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{session.FocusSessionId}:{session.StartedAtUtc:yyyyMMddHHmmssfffffff}");

    private sealed record BrowserPersistenceResult(
        string? CurrentDomain,
        DashboardBrowserCaptureStatus CaptureStatus,
        bool HasPersistedWebSession)
    {
        public static BrowserPersistenceResult Empty { get; } = new(
            null,
            DashboardBrowserCaptureStatus.Unavailable,
            HasPersistedWebSession: false);

        public static BrowserPersistenceResult Error { get; } = new(
            null,
            DashboardBrowserCaptureStatus.Error,
            HasPersistedWebSession: false);
    }
}
