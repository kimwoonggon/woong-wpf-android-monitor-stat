using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
        DateTimeOffset observedAtUtc,
        string clientEventId)
    {
        WindowId = windowId;
        TabId = tabId;
        Url = EnsureText(url, nameof(url));
        Title = title.Trim();
        Domain = EnsureText(domain, nameof(domain));
        BrowserFamily = EnsureText(browserFamily, nameof(browserFamily));
        ObservedAtUtc = observedAtUtc.ToUniversalTime();
        ClientEventId = EnsureText(clientEventId, nameof(clientEventId));
    }

    public string ClientEventId { get; }

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
        string browserFamily,
        string? clientEventId = null)
    {
        string domain = DomainNormalizer.ExtractRegistrableDomain(url);
        string eventId = string.IsNullOrWhiteSpace(clientEventId)
            ? DeriveMetadataOnlyClientEventId(browserFamily, windowId, tabId, domain, observedAtUtc)
            : clientEventId.Trim();

        return new(
            windowId,
            tabId,
            url,
            title,
            domain,
            browserFamily,
            observedAtUtc,
            eventId);
    }

    private static string DeriveMetadataOnlyClientEventId(
        string browserFamily,
        int windowId,
        int tabId,
        string domain,
        DateTimeOffset observedAtUtc)
    {
        string stableMetadata = string.Join(
            "\n",
            browserFamily.Trim(),
            windowId.ToString(CultureInfo.InvariantCulture),
            tabId.ToString(CultureInfo.InvariantCulture),
            domain.Trim().ToUpperInvariant(),
            observedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(stableMetadata));
        return $"chrome-active-tab:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static string EnsureText(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value must not be empty.", parameterName)
            : value;
}
