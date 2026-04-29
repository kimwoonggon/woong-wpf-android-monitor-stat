using System.Text.Json;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using Woong.MonitorStack.Windows.Storage;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public sealed class WindowsTrackingDashboardCoordinator : IDashboardTrackingCoordinator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Func<TrackingPoller> _trackingPollerFactory;
    private readonly SqliteFocusSessionRepository _focusSessionRepository;
    private readonly SqliteSyncOutboxRepository _outboxRepository;
    private readonly ISystemClock _clock;
    private TrackingPoller? _trackingPoller;
    private bool _isRunning;
    private DateTimeOffset? _lastDbWriteAtUtc;

    public WindowsTrackingDashboardCoordinator(
        Func<TrackingPoller> trackingPollerFactory,
        SqliteFocusSessionRepository focusSessionRepository,
        SqliteSyncOutboxRepository outboxRepository,
        ISystemClock clock)
    {
        _trackingPollerFactory = trackingPollerFactory ?? throw new ArgumentNullException(nameof(trackingPollerFactory));
        _focusSessionRepository = focusSessionRepository ?? throw new ArgumentNullException(nameof(focusSessionRepository));
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public DashboardTrackingSnapshot StartTracking()
    {
        _trackingPoller = _trackingPollerFactory();
        _isRunning = true;
        DateTimeOffset pollAtUtc = _clock.UtcNow;
        FocusSessionizerResult result = _trackingPoller.Poll();
        DashboardPersistedSessionSnapshot? persisted = PersistIfPresent(result.ClosedSession);

        return ToSnapshot(result.CurrentSession, persisted, pollAtUtc, persisted is null ? null : _lastDbWriteAtUtc);
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
        persisted = Persist(result.CurrentSession);
        _isRunning = false;
        _trackingPoller = null;

        return ToSnapshot(result.CurrentSession, persisted, pollAtUtc, _lastDbWriteAtUtc);
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

        return ToSnapshot(result.CurrentSession, persisted, pollAtUtc, persisted is null ? null : _lastDbWriteAtUtc);
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

    private static DashboardTrackingSnapshot ToSnapshot(
        FocusSession session,
        DashboardPersistedSessionSnapshot? persistedSession,
        DateTimeOffset lastPollAtUtc,
        DateTimeOffset? lastDbWriteAtUtc)
        => new(
            AppName: session.PlatformAppKey,
            ProcessName: session.ProcessName ?? session.PlatformAppKey,
            WindowTitle: session.WindowTitle,
            CurrentSessionDuration: TimeSpan.FromMilliseconds(session.DurationMs),
            LastPersistedSession: persistedSession,
            LastPollAtUtc: lastPollAtUtc,
            LastDbWriteAtUtc: lastDbWriteAtUtc);

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
}
