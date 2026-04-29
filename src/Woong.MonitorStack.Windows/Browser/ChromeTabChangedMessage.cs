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
        string browserFamily,
        DateTimeOffset observedAtUtc)
    {
        WindowId = windowId;
        TabId = tabId;
        Url = EnsureText(url, nameof(url));
        Title = title.Trim();
        Domain = EnsureText(domain, nameof(domain));
        BrowserFamily = EnsureText(browserFamily, nameof(browserFamily));
        ObservedAtUtc = observedAtUtc.ToUniversalTime();
    }

    public string BrowserFamily { get; }

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
        => FromExtensionPayload(windowId, tabId, url, title, observedAtUtc, "Chrome");

    public static ChromeTabChangedMessage FromExtensionPayload(
        int windowId,
        int tabId,
        string url,
        string title,
        DateTimeOffset observedAtUtc,
        string browserFamily)
        => new(
            windowId,
            tabId,
            url,
            title,
            DomainNormalizer.ExtractRegistrableDomain(url),
            browserFamily,
            observedAtUtc);

    private static string EnsureText(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value must not be empty.", parameterName)
            : value;
}
