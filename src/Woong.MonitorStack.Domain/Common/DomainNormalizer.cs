namespace Woong.MonitorStack.Domain.Common;

public static class DomainNormalizer
{
    private static readonly HashSet<string> TwoPartPublicSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "co.kr",
        "or.kr",
        "go.kr",
        "ac.kr",
        "co.uk",
        "com.au"
    };

    public static string ExtractRegistrableDomain(string urlOrHost)
    {
        if (string.IsNullOrWhiteSpace(urlOrHost))
        {
            throw new ArgumentException("Value must not be empty.", nameof(urlOrHost));
        }

        var host = ExtractHost(urlOrHost.Trim()).TrimEnd('.').ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
        {
            host = host[4..];
        }

        var labels = host.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (labels.Length <= 2)
        {
            return string.Join('.', labels);
        }

        var suffix = string.Join('.', labels[^2], labels[^1]);
        return TwoPartPublicSuffixes.Contains(suffix)
            ? string.Join('.', labels[^3], labels[^2], labels[^1])
            : suffix;
    }

    private static string ExtractHost(string urlOrHost)
    {
        var candidate = Uri.TryCreate(urlOrHost, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri
            : Uri.TryCreate($"https://{urlOrHost}", UriKind.Absolute, out var hostOnlyUri)
                ? hostOnlyUri
                : throw new ArgumentException("Value must be a URL or host.", nameof(urlOrHost));

        return candidate.Host;
    }
}
