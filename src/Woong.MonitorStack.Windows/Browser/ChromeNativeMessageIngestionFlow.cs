using System.Globalization;
using System.Text.Json;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.Browser;

public sealed class ChromeNativeMessageIngestionFlow
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SqliteBrowserRawEventRepository? _rawEvents;
    private readonly SqliteWebSessionRepository? _webSessions;
    private readonly SqliteBrowserIngestionRepository? _ingestionRepository;
    private readonly BrowserWebSessionizer _sessionizer;
    private readonly SqliteSyncOutboxRepository? _outbox;
    private readonly string? _deviceId;
    private readonly IBrowserUrlSanitizer _urlSanitizer;
    private readonly BrowserUrlStoragePolicy _storagePolicy;
    private readonly BrowserRawEventRetentionService? _rawEventRetention;

    public ChromeNativeMessageIngestionFlow(
        SqliteBrowserRawEventRepository rawEvents,
        SqliteWebSessionRepository webSessions,
        BrowserWebSessionizer sessionizer)
        : this(rawEvents, webSessions, outbox: null, deviceId: null, sessionizer)
    {
    }

    public ChromeNativeMessageIngestionFlow(
        SqliteBrowserRawEventRepository rawEvents,
        SqliteWebSessionRepository webSessions,
        SqliteSyncOutboxRepository? outbox,
        string? deviceId,
        BrowserWebSessionizer sessionizer)
        : this(
            rawEvents,
            webSessions,
            outbox,
            deviceId,
            sessionizer,
            new BrowserUrlSanitizer(),
            BrowserUrlStoragePolicy.FullUrl)
    {
    }

    public ChromeNativeMessageIngestionFlow(
        SqliteBrowserRawEventRepository rawEvents,
        SqliteWebSessionRepository webSessions,
        SqliteSyncOutboxRepository? outbox,
        string? deviceId,
        BrowserWebSessionizer sessionizer,
        IBrowserUrlSanitizer urlSanitizer,
        BrowserUrlStoragePolicy storagePolicy,
        BrowserRawEventRetentionService? rawEventRetention = null)
    {
        _rawEvents = rawEvents ?? throw new ArgumentNullException(nameof(rawEvents));
        _webSessions = webSessions ?? throw new ArgumentNullException(nameof(webSessions));
        _ingestionRepository = null;
        _sessionizer = sessionizer ?? throw new ArgumentNullException(nameof(sessionizer));
        _outbox = outbox;
        _deviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId;
        _urlSanitizer = urlSanitizer ?? throw new ArgumentNullException(nameof(urlSanitizer));
        _storagePolicy = storagePolicy;
        _rawEventRetention = rawEventRetention;
    }

    public ChromeNativeMessageIngestionFlow(
        SqliteBrowserIngestionRepository ingestionRepository,
        SqliteSyncOutboxRepository? outbox,
        string? deviceId,
        BrowserWebSessionizer sessionizer,
        IBrowserUrlSanitizer urlSanitizer,
        BrowserUrlStoragePolicy storagePolicy,
        BrowserRawEventRetentionPolicy? rawEventRetentionPolicy = null)
    {
        _rawEvents = null;
        _webSessions = null;
        _ingestionRepository = ingestionRepository ?? throw new ArgumentNullException(nameof(ingestionRepository));
        _sessionizer = sessionizer ?? throw new ArgumentNullException(nameof(sessionizer));
        _outbox = outbox;
        _deviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId;
        _urlSanitizer = urlSanitizer ?? throw new ArgumentNullException(nameof(urlSanitizer));
        _storagePolicy = storagePolicy;
        _rawEventRetention = null;
        RawEventRetentionPolicy = rawEventRetentionPolicy;
    }

    private BrowserRawEventRetentionPolicy? RawEventRetentionPolicy { get; }

    public async Task IngestAsync(Stream nativeMessageStream, CancellationToken cancellationToken)
        => _ = await TryIngestNextAsync(nativeMessageStream, cancellationToken).ConfigureAwait(false);

    public async Task<bool> TryIngestNextAsync(Stream nativeMessageStream, CancellationToken cancellationToken)
    {
        var message = await ChromeNativeMessageReceiver
            .ReadNextAsync(nativeMessageStream, cancellationToken)
            .ConfigureAwait(false);
        if (message is null)
        {
            return false;
        }

        BrowserActivitySnapshot sanitized = _urlSanitizer.Sanitize(ToSnapshot(message), _storagePolicy);
        BrowserRawEventRecord rawEvent = ToRawEvent(message, sanitized);
        if (_ingestionRepository is not null)
        {
            DateTimeOffset? retentionCutoffUtc = RawEventRetentionPolicy?.CutoffFor(sanitized.CapturedAtUtc);
            return _ingestionRepository.IngestInsertedRawEvent(
                rawEvent,
                retentionCutoffUtc,
                () => _sessionizer.Apply(sanitized),
                CreateOutboxItemIfConfigured);
        }

        _rawEvents!.Save(rawEvent);
        _ = _rawEventRetention?.PruneExpired(sanitized.CapturedAtUtc);
        foreach (var session in _sessionizer.Apply(sanitized))
        {
            _webSessions!.Save(session);
            EnqueueIfConfigured(session);
        }

        return true;
    }

    private void EnqueueIfConfigured(WebSession session)
    {
        SyncOutboxItem? outboxItem = CreateOutboxItemIfConfigured(session);
        if (outboxItem is not null)
        {
            _outbox!.Add(outboxItem);
        }
    }

    private SyncOutboxItem? CreateOutboxItemIfConfigured(WebSession session)
    {
        if (_outbox is null || _deviceId is null)
        {
            return null;
        }

        string aggregateId = CreateAggregateId(session);
        return SyncOutboxItem.Pending(
            id: $"web-session:{aggregateId}",
            aggregateType: "web_session",
            aggregateId,
            payloadJson: CreatePayload(session, aggregateId),
            createdAtUtc: session.EndedAtUtc);
    }

    private string CreatePayload(WebSession session, string aggregateId)
    {
        var request = new UploadWebSessionsRequest(
            _deviceId!,
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

    private static BrowserActivitySnapshot ToSnapshot(ChromeTabChangedMessage message)
        => new(
            message.ObservedAtUtc,
            message.BrowserFamily,
            processName: $"{message.BrowserFamily}.exe",
            processId: null,
            windowHandle: message.WindowId,
            windowTitle: null,
            tabTitle: message.Title,
            url: message.Url,
            domain: message.Domain,
            CaptureMethod.BrowserExtensionFuture,
            CaptureConfidence.High,
            isPrivateOrUnknown: false);

    private static BrowserRawEventRecord ToRawEvent(
        ChromeTabChangedMessage message,
        BrowserActivitySnapshot sanitized)
        => new(
            message.BrowserFamily,
            message.WindowId,
            message.TabId,
            sanitized.Url,
            sanitized.TabTitle,
            sanitized.Domain,
            sanitized.CapturedAtUtc,
            message.ClientEventId);
}
