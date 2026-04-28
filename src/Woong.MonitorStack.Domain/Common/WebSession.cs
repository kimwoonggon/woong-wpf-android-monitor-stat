namespace Woong.MonitorStack.Domain.Common;

public sealed record WebSession
{
    public WebSession(
        string focusSessionId,
        string browserFamily,
        string url,
        string domain,
        string pageTitle,
        TimeRange range)
    {
        FocusSessionId = RequiredText.Ensure(focusSessionId, nameof(focusSessionId));
        BrowserFamily = RequiredText.Ensure(browserFamily, nameof(browserFamily));
        Url = RequiredText.Ensure(url, nameof(url));
        Domain = RequiredText.Ensure(domain, nameof(domain));
        PageTitle = RequiredText.Ensure(pageTitle, nameof(pageTitle));
        Range = range;
    }

    public string FocusSessionId { get; }

    public string BrowserFamily { get; }

    public string Url { get; }

    public string Domain { get; }

    public string PageTitle { get; }

    public TimeRange Range { get; }

    public DateTimeOffset StartedAtUtc => Range.StartedAtUtc;

    public DateTimeOffset EndedAtUtc => Range.EndedAtUtc;

    public long DurationMs => Convert.ToInt64(Range.Duration.TotalMilliseconds);

    public static WebSession FromUtc(
        string focusSessionId,
        string browserFamily,
        string url,
        string pageTitle,
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc)
        => new(
            focusSessionId,
            browserFamily,
            url,
            DomainNormalizer.ExtractRegistrableDomain(url),
            pageTitle,
            TimeRange.FromUtc(startedAtUtc, endedAtUtc));
}
