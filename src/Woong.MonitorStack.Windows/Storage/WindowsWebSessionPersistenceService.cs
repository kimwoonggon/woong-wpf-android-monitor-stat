using System.Globalization;
using System.Text.Json;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.Browser;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.Storage;

public sealed class WindowsWebSessionPersistenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SqliteWebSessionRepository _webSessionRepository;
    private readonly SqliteSyncOutboxRepository _outboxRepository;
    private readonly ISystemClock _clock;
    private readonly BrowserUrlStoragePolicy _storagePolicy;

    public WindowsWebSessionPersistenceService(
        SqliteWebSessionRepository webSessionRepository,
        SqliteSyncOutboxRepository outboxRepository,
        ISystemClock clock,
        BrowserUrlStoragePolicy storagePolicy = BrowserUrlStoragePolicy.DomainOnly)
    {
        _webSessionRepository = webSessionRepository ?? throw new ArgumentNullException(nameof(webSessionRepository));
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _storagePolicy = storagePolicy;
    }

    public WindowsWebSessionPersistenceResult SaveWebSession(WebSession session, string deviceId)
    {
        ArgumentNullException.ThrowIfNull(session);
        string requiredDeviceId = RequiredStorageText.Ensure(deviceId, nameof(deviceId));

        WebSession privacySafeSession = ApplyStoragePolicy(session);
        _webSessionRepository.Save(privacySafeSession);
        DateTimeOffset persistedAtUtc = _clock.UtcNow;
        string aggregateId = CreateAggregateId(privacySafeSession);
        _outboxRepository.Add(SyncOutboxItem.Pending(
            id: $"web-session:{aggregateId}",
            aggregateType: "web_session",
            aggregateId,
            payloadJson: CreatePayload(privacySafeSession, requiredDeviceId, aggregateId),
            createdAtUtc: persistedAtUtc));

        return new WindowsWebSessionPersistenceResult(privacySafeSession, aggregateId, persistedAtUtc);
    }

    private WebSession ApplyStoragePolicy(WebSession session)
        => _storagePolicy switch
        {
            BrowserUrlStoragePolicy.DomainOnly => Copy(session, url: null, pageTitle: null),
            BrowserUrlStoragePolicy.FullUrl => session,
            _ => throw new InvalidOperationException("Web session persistence requires domain-only or explicit full-URL storage.")
        };

    private static WebSession Copy(WebSession session, string? url, string? pageTitle)
        => new(
            session.FocusSessionId,
            session.BrowserFamily,
            url,
            session.Domain,
            pageTitle,
            session.Range,
            session.CaptureMethod,
            session.CaptureConfidence,
            session.IsPrivateOrUnknown);

    private static string CreatePayload(WebSession session, string deviceId, string aggregateId)
    {
        var request = new UploadWebSessionsRequest(
            deviceId,
            [
                new WebSessionUploadItem(
                    aggregateId,
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

    private static string CreateAggregateId(WebSession session)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{session.FocusSessionId}:{session.StartedAtUtc:yyyyMMddHHmmssfffffff}");
}

public sealed record WindowsWebSessionPersistenceResult(
    WebSession Session,
    string AggregateId,
    DateTimeOffset PersistedAtUtc);
