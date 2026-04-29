using System.Text.Json;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.Storage;

public sealed class WindowsFocusSessionPersistenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SqliteFocusSessionRepository _focusSessionRepository;
    private readonly SqliteSyncOutboxRepository _outboxRepository;
    private readonly ISystemClock _clock;

    public WindowsFocusSessionPersistenceService(
        SqliteFocusSessionRepository focusSessionRepository,
        SqliteSyncOutboxRepository outboxRepository,
        ISystemClock clock)
    {
        _focusSessionRepository = focusSessionRepository ?? throw new ArgumentNullException(nameof(focusSessionRepository));
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public WindowsFocusSessionPersistenceResult SaveFocusSession(FocusSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        FocusSession privacySafeSession = ClearWindowTitle(session);
        _focusSessionRepository.Save(privacySafeSession);
        DateTimeOffset persistedAtUtc = _clock.UtcNow;
        _outboxRepository.Add(SyncOutboxItem.Pending(
            id: $"focus-session:{privacySafeSession.ClientSessionId}",
            aggregateType: "focus_session",
            aggregateId: privacySafeSession.ClientSessionId,
            payloadJson: CreatePayload(privacySafeSession),
            createdAtUtc: persistedAtUtc));

        return new WindowsFocusSessionPersistenceResult(privacySafeSession, persistedAtUtc);
    }

    private static FocusSession ClearWindowTitle(FocusSession session)
        => session.WindowTitle is null
            ? session
            : FocusSession.FromUtc(
                session.ClientSessionId,
                session.DeviceId,
                session.PlatformAppKey,
                session.StartedAtUtc,
                session.EndedAtUtc,
                session.TimezoneId,
                session.IsIdle,
                session.Source,
                session.ProcessId,
                session.ProcessName,
                session.ProcessPath,
                session.WindowHandle,
                windowTitle: null);

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
                    windowTitle: null)
            ]);

        return JsonSerializer.Serialize(request, JsonOptions);
    }
}

public sealed record WindowsFocusSessionPersistenceResult(
    FocusSession Session,
    DateTimeOffset PersistedAtUtc);
