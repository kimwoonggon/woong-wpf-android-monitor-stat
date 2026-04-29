using System.Diagnostics.CodeAnalysis;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Browser;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.App.Browser;

public sealed class UiAutomationBrowserActivityReader(
    IBrowserProcessClassifier browserProcessClassifier,
    IBrowserAddressBarReader addressBarReader)
    : IBrowserActivityReader
{
    private readonly IBrowserProcessClassifier _browserProcessClassifier =
        browserProcessClassifier ?? throw new ArgumentNullException(nameof(browserProcessClassifier));
    private readonly IBrowserAddressBarReader _addressBarReader =
        addressBarReader ?? throw new ArgumentNullException(nameof(addressBarReader));

    public BrowserActivitySnapshot? TryRead(ForegroundWindowSnapshot foregroundWindow)
    {
        ArgumentNullException.ThrowIfNull(foregroundWindow);

        BrowserProcessClassification classification = _browserProcessClassifier.Classify(foregroundWindow.ProcessName);
        if (!classification.IsBrowser || classification.BrowserName is null)
        {
            return null;
        }

        string? address = _addressBarReader.TryReadAddress(foregroundWindow);
        string? webUrl = NormalizeWebUrl(address);
        if (webUrl is null)
        {
            return CreateWindowTitleOnlySnapshot(foregroundWindow, classification.BrowserName);
        }

        return new BrowserActivitySnapshot(
            foregroundWindow.TimestampUtc,
            classification.BrowserName,
            foregroundWindow.ProcessName,
            foregroundWindow.ProcessId,
            foregroundWindow.Hwnd.ToInt64(),
            foregroundWindow.WindowTitle,
            tabTitle: null,
            url: webUrl,
            domain: DomainNormalizer.ExtractRegistrableDomain(webUrl),
            CaptureMethod.UIAutomationAddressBar,
            CaptureConfidence.Medium,
            isPrivateOrUnknown: false);
    }

    private static BrowserActivitySnapshot CreateWindowTitleOnlySnapshot(
        ForegroundWindowSnapshot foregroundWindow,
        string browserName)
        => new(
            foregroundWindow.TimestampUtc,
            browserName,
            foregroundWindow.ProcessName,
            foregroundWindow.ProcessId,
            foregroundWindow.Hwnd.ToInt64(),
            foregroundWindow.WindowTitle,
            tabTitle: null,
            url: null,
            domain: null,
            CaptureMethod.WindowTitleOnly,
            CaptureConfidence.Low,
            isPrivateOrUnknown: null);

    private static string? NormalizeWebUrl(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        string trimmed = address.Trim();
        if (TryCreateWebUri(trimmed, out Uri? absoluteUri))
        {
            return absoluteUri.AbsoluteUri;
        }

        if (trimmed.Contains(' ', StringComparison.Ordinal) || !trimmed.Contains('.', StringComparison.Ordinal))
        {
            return null;
        }

        return TryCreateWebUri($"https://{trimmed}", out Uri? hostOnlyUri)
            ? hostOnlyUri.AbsoluteUri
            : null;
    }

    private static bool TryCreateWebUri(string value, [NotNullWhen(true)] out Uri? uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out uri) &&
            (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        uri = null;
        return false;
    }
}
