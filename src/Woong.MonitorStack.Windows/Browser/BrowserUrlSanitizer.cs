using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Browser;

public sealed class BrowserUrlSanitizer : IBrowserUrlSanitizer
{
    public BrowserActivitySnapshot Sanitize(
        BrowserActivitySnapshot snapshot,
        BrowserUrlStoragePolicy storagePolicy)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return storagePolicy switch
        {
            BrowserUrlStoragePolicy.Off => Copy(
                snapshot,
                url: null,
                tabTitle: null,
                domain: null,
                captureMethod: CaptureMethod.None,
                captureConfidence: CaptureConfidence.Unknown),
            BrowserUrlStoragePolicy.DomainOnly => Copy(
                snapshot,
                url: null,
                tabTitle: null,
                domain: NormalizeDomain(snapshot.Domain, snapshot.Url),
                captureMethod: snapshot.CaptureMethod,
                captureConfidence: snapshot.CaptureConfidence),
            BrowserUrlStoragePolicy.FullUrl => Copy(
                snapshot,
                url: StripFragment(snapshot.Url),
                tabTitle: snapshot.TabTitle,
                domain: NormalizeDomain(snapshot.Domain, snapshot.Url),
                captureMethod: snapshot.CaptureMethod,
                captureConfidence: snapshot.CaptureConfidence),
            _ => throw new ArgumentOutOfRangeException(nameof(storagePolicy), storagePolicy, "Unknown browser URL storage policy.")
        };
    }

    private static BrowserActivitySnapshot Copy(
        BrowserActivitySnapshot snapshot,
        string? url,
        string? tabTitle,
        string? domain,
        CaptureMethod captureMethod,
        CaptureConfidence captureConfidence)
        => new(
            snapshot.CapturedAtUtc,
            snapshot.BrowserName,
            snapshot.ProcessName,
            snapshot.ProcessId,
            snapshot.WindowHandle,
            snapshot.WindowTitle,
            tabTitle,
            url,
            domain,
            captureMethod,
            captureConfidence,
            snapshot.IsPrivateOrUnknown);

    private static string? NormalizeDomain(string? domain, string? url)
    {
        string? candidate = !string.IsNullOrWhiteSpace(domain) ? domain : url;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        try
        {
            return DomainNormalizer.ExtractRegistrableDomain(candidate);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string? StripFragment(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return null;
        }

        var builder = new UriBuilder(uri)
        {
            Fragment = string.Empty
        };
        return builder.Uri.AbsoluteUri;
    }
}
