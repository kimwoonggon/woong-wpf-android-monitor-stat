using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Browser;

public sealed class BrowserWebSessionizer
    : IWebSessionizer
{
    private readonly string _focusSessionId;
    private BrowserActivitySnapshot? _current;

    public BrowserWebSessionizer(string focusSessionId)
    {
        _focusSessionId = string.IsNullOrWhiteSpace(focusSessionId)
            ? throw new ArgumentException("Value must not be empty.", nameof(focusSessionId))
            : focusSessionId;
    }

    public IReadOnlyList<WebSession> Apply(ChromeTabChangedMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return Apply(new BrowserActivitySnapshot(
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
            isPrivateOrUnknown: false));
    }

    public IReadOnlyList<WebSession> Apply(BrowserActivitySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (_current is null)
        {
            _current = HasWebIdentity(snapshot) ? snapshot : null;
            return [];
        }

        if (IsDuplicate(_current, snapshot))
        {
            return [];
        }

        WebSession completed = CreateSession(_current, snapshot.CapturedAtUtc);

        _current = HasWebIdentity(snapshot) ? snapshot : null;
        return [completed];
    }

    public WebSession? PreviewCurrent(DateTimeOffset endedAtUtc)
    {
        if (_current is null || endedAtUtc <= _current.CapturedAtUtc)
        {
            return null;
        }

        return CreateSession(_current, endedAtUtc);
    }

    public IReadOnlyList<WebSession> CompleteCurrent(DateTimeOffset endedAtUtc)
    {
        if (_current is null)
        {
            return [];
        }

        BrowserActivitySnapshot current = _current;
        _current = null;
        if (endedAtUtc <= current.CapturedAtUtc)
        {
            return [];
        }

        return [CreateSession(current, endedAtUtc)];
    }

    private static bool HasWebIdentity(BrowserActivitySnapshot snapshot)
        => !string.IsNullOrWhiteSpace(snapshot.Domain);

    private WebSession CreateSession(BrowserActivitySnapshot snapshot, DateTimeOffset endedAtUtc)
        => new(
            _focusSessionId,
            snapshot.BrowserName,
            snapshot.Url,
            snapshot.Domain!,
            snapshot.TabTitle,
            TimeRange.FromUtc(snapshot.CapturedAtUtc, endedAtUtc),
            snapshot.CaptureMethod.ToString(),
            snapshot.CaptureConfidence.ToString(),
            snapshot.IsPrivateOrUnknown);

    private static bool IsDuplicate(BrowserActivitySnapshot current, BrowserActivitySnapshot next)
        => current.WindowHandle == next.WindowHandle
            && string.Equals(current.Url, next.Url, StringComparison.Ordinal)
            && string.Equals(current.TabTitle, next.TabTitle, StringComparison.Ordinal)
            && string.Equals(current.Domain, next.Domain, StringComparison.Ordinal)
            && string.Equals(current.BrowserName, next.BrowserName, StringComparison.Ordinal);
}
