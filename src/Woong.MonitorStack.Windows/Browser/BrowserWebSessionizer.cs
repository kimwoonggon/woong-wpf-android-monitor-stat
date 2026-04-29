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

        var completed = new WebSession(
            _focusSessionId,
            _current.BrowserName,
            _current.Url,
            _current.Domain!,
            _current.TabTitle,
            TimeRange.FromUtc(_current.CapturedAtUtc, snapshot.CapturedAtUtc),
            _current.CaptureMethod.ToString(),
            _current.CaptureConfidence.ToString(),
            _current.IsPrivateOrUnknown);

        _current = HasWebIdentity(snapshot) ? snapshot : null;
        return [completed];
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

        return
        [
            new WebSession(
                _focusSessionId,
                current.BrowserName,
                current.Url,
                current.Domain!,
                current.TabTitle,
                TimeRange.FromUtc(current.CapturedAtUtc, endedAtUtc),
                current.CaptureMethod.ToString(),
                current.CaptureConfidence.ToString(),
                current.IsPrivateOrUnknown)
        ];
    }

    private static bool HasWebIdentity(BrowserActivitySnapshot snapshot)
        => !string.IsNullOrWhiteSpace(snapshot.Domain);

    private static bool IsDuplicate(BrowserActivitySnapshot current, BrowserActivitySnapshot next)
        => current.WindowHandle == next.WindowHandle
            && string.Equals(current.Url, next.Url, StringComparison.Ordinal)
            && string.Equals(current.TabTitle, next.TabTitle, StringComparison.Ordinal)
            && string.Equals(current.Domain, next.Domain, StringComparison.Ordinal)
            && string.Equals(current.BrowserName, next.BrowserName, StringComparison.Ordinal);
}
