using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.Browser;

public sealed class ChromeNativeMessageIngestionFlow
{
    private readonly SqliteBrowserRawEventRepository _rawEvents;
    private readonly SqliteWebSessionRepository _webSessions;
    private readonly BrowserWebSessionizer _sessionizer;

    public ChromeNativeMessageIngestionFlow(
        SqliteBrowserRawEventRepository rawEvents,
        SqliteWebSessionRepository webSessions,
        BrowserWebSessionizer sessionizer)
    {
        _rawEvents = rawEvents ?? throw new ArgumentNullException(nameof(rawEvents));
        _webSessions = webSessions ?? throw new ArgumentNullException(nameof(webSessions));
        _sessionizer = sessionizer ?? throw new ArgumentNullException(nameof(sessionizer));
    }

    public async Task IngestAsync(Stream nativeMessageStream, CancellationToken cancellationToken)
    {
        var message = await ChromeNativeMessageReceiver
            .ReadNextAsync(nativeMessageStream, cancellationToken)
            .ConfigureAwait(false);
        if (message is null)
        {
            return;
        }

        _rawEvents.Save(message);
        foreach (var session in _sessionizer.Apply(message))
        {
            _webSessions.Save(session);
        }
    }
}
