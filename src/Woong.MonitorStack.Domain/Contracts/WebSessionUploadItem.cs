namespace Woong.MonitorStack.Domain.Contracts;

public sealed record WebSessionUploadItem
{
    public WebSessionUploadItem(
        string focusSessionId,
        string browserFamily,
        string? url,
        string domain,
        string? pageTitle,
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc,
        long durationMs)
    {
        FocusSessionId = RequiredContractText.Ensure(focusSessionId, nameof(focusSessionId));
        BrowserFamily = RequiredContractText.Ensure(browserFamily, nameof(browserFamily));
        Url = NormalizeOptional(url);
        Domain = RequiredContractText.Ensure(domain, nameof(domain));
        PageTitle = NormalizeOptional(pageTitle);
        StartedAtUtc = startedAtUtc.ToUniversalTime();
        EndedAtUtc = endedAtUtc.ToUniversalTime();
        DurationMs = durationMs > 0 ? durationMs : throw new ArgumentOutOfRangeException(nameof(durationMs));
    }

    public string FocusSessionId { get; }

    public string BrowserFamily { get; }

    public string? Url { get; }

    public string Domain { get; }

    public string? PageTitle { get; }

    public DateTimeOffset StartedAtUtc { get; }

    public DateTimeOffset EndedAtUtc { get; }

    public long DurationMs { get; }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
