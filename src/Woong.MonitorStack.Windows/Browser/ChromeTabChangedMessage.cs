using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Browser;

public sealed record ChromeTabChangedMessage
{
    private ChromeTabChangedMessage(
        int windowId,
        int tabId,
        string url,
        string title,
        string domain,
        DateTimeOffset observedAtUtc)
    {
        WindowId = windowId;
        TabId = tabId;
        Url = EnsureText(url, nameof(url));
        Title = EnsureText(title, nameof(title));
        Domain = EnsureText(domain, nameof(domain));
        ObservedAtUtc = observedAtUtc.ToUniversalTime();
    }

    public int WindowId { get; }

    public int TabId { get; }

    public string Url { get; }

    public string Title { get; }

    public string Domain { get; }

    public DateTimeOffset ObservedAtUtc { get; }

    public static ChromeTabChangedMessage FromExtensionPayload(
        int windowId,
        int tabId,
        string url,
        string title,
        DateTimeOffset observedAtUtc)
        => new(
            windowId,
            tabId,
            url,
            title,
            DomainNormalizer.ExtractRegistrableDomain(url),
            observedAtUtc);

    private static string EnsureText(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value must not be empty.", parameterName)
            : value;
}
