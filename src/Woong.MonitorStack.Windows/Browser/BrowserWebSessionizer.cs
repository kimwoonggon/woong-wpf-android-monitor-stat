using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Browser;

public sealed class BrowserWebSessionizer
{
    private readonly string _focusSessionId;
    private ChromeTabChangedMessage? _current;

    public BrowserWebSessionizer(string focusSessionId)
    {
        _focusSessionId = string.IsNullOrWhiteSpace(focusSessionId)
            ? throw new ArgumentException("Value must not be empty.", nameof(focusSessionId))
            : focusSessionId;
    }

    public IReadOnlyList<WebSession> Apply(ChromeTabChangedMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_current is null)
        {
            _current = message;
            return [];
        }

        if (IsDuplicate(_current, message))
        {
            return [];
        }

        var completed = WebSession.FromUtc(
            focusSessionId: _focusSessionId,
            browserFamily: _current.BrowserFamily,
            url: _current.Url,
            pageTitle: _current.Title,
            startedAtUtc: _current.ObservedAtUtc,
            endedAtUtc: message.ObservedAtUtc);

        _current = message;
        return [completed];
    }

    private static bool IsDuplicate(ChromeTabChangedMessage current, ChromeTabChangedMessage next)
        => current.WindowId == next.WindowId
            && current.TabId == next.TabId
            && string.Equals(current.Url, next.Url, StringComparison.Ordinal)
            && string.Equals(current.Title, next.Title, StringComparison.Ordinal)
            && string.Equals(current.Domain, next.Domain, StringComparison.Ordinal)
            && string.Equals(current.BrowserFamily, next.BrowserFamily, StringComparison.Ordinal);
}
