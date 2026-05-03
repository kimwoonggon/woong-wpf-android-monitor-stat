using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed class DashboardCurrentActivityMapper
{
    public const string BrowserDomainUnavailableText = "No browser domain yet. Connect browser capture; app focus is tracked.";

    private readonly DashboardRowMapper _rowMapper;

    public DashboardCurrentActivityMapper(TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        _rowMapper = new DashboardRowMapper(timeZone);
    }

    public DashboardCurrentActivityPresentation Map(
        DashboardTrackingSnapshot snapshot,
        bool isWindowTitleVisible)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        string? capturedWindowTitle = isWindowTitleVisible ? snapshot.WindowTitle : null;
        DateTimeOffset? lastDbWriteAtUtc = snapshot.LastDbWriteAtUtc
            ?? snapshot.LastPersistedSession?.EndedAtUtc
            ?? (snapshot.HasPersistedWebSession ? snapshot.LastPollAtUtc : null);

        return new DashboardCurrentActivityPresentation(
            TextOrDefault(snapshot.AppName, "No current app"),
            TextOrDefault(snapshot.ProcessName, "No process"),
            capturedWindowTitle,
            FormatCurrentWindowTitle(capturedWindowTitle, isWindowTitleVisible),
            FormatBrowserDomain(snapshot.CurrentBrowserDomain),
            FormatBrowserCaptureStatus(snapshot.BrowserCaptureStatus),
            FormatClockDuration(snapshot.CurrentSessionDuration),
            snapshot.LastPollAtUtc is null ? null : _rowMapper.FormatLocalTime(snapshot.LastPollAtUtc.Value),
            lastDbWriteAtUtc is null ? null : _rowMapper.FormatLocalTime(lastDbWriteAtUtc.Value),
            CreateActiveWebSessionPreview(snapshot));
    }

    public string FormatCurrentWindowTitle(string? capturedWindowTitle, bool isWindowTitleVisible)
        => isWindowTitleVisible
            ? TextOrDefault(capturedWindowTitle, "No window title")
            : "Window title hidden by privacy settings";

    public WebSession? CreateActiveWebSessionForRange(
        DashboardActiveWebSessionPreview? preview,
        TimeRange range)
    {
        if (preview is null)
        {
            return null;
        }

        DateTimeOffset startedAtUtc = preview.StartedAtUtc > range.StartedAtUtc
            ? preview.StartedAtUtc
            : range.StartedAtUtc;
        DateTimeOffset endedAtUtc = preview.EndedAtUtc < range.EndedAtUtc
            ? preview.EndedAtUtc
            : range.EndedAtUtc;
        if (endedAtUtc <= startedAtUtc)
        {
            return null;
        }

        return new WebSession(
            "current-active-web-session",
            preview.BrowserFamily,
            url: null,
            preview.Domain,
            pageTitle: null,
            TimeRange.FromUtc(startedAtUtc, endedAtUtc),
            captureMethod: "LiveDashboardPreview",
            captureConfidence: "Live",
            isPrivateOrUnknown: false);
    }

    private static DashboardActiveWebSessionPreview? CreateActiveWebSessionPreview(DashboardTrackingSnapshot snapshot)
    {
        if (snapshot.CurrentWebSessionStartedAtUtc is null ||
            snapshot.CurrentWebSessionDuration <= TimeSpan.Zero ||
            string.IsNullOrWhiteSpace(snapshot.CurrentBrowserDomain))
        {
            return null;
        }

        DateTimeOffset endedAtUtc = snapshot.LastPollAtUtc
            ?? snapshot.CurrentWebSessionStartedAtUtc.Value.Add(snapshot.CurrentWebSessionDuration);
        if (endedAtUtc <= snapshot.CurrentWebSessionStartedAtUtc.Value)
        {
            return null;
        }

        return new DashboardActiveWebSessionPreview(
            FormatBrowserDomain(snapshot.CurrentBrowserDomain),
            TextOrDefault(snapshot.AppName, "Current browser"),
            snapshot.CurrentWebSessionStartedAtUtc.Value,
            endedAtUtc);
    }

    private static string TextOrDefault(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string FormatBrowserDomain(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return BrowserDomainUnavailableText;
        }

        string trimmed = value.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? absoluteUri) && !string.IsNullOrWhiteSpace(absoluteUri.Host))
        {
            return absoluteUri.Host.ToLowerInvariant();
        }

        string candidate = trimmed;
        int pathStart = candidate.IndexOfAny(['/', '?', '#']);
        if (pathStart >= 0)
        {
            candidate = candidate[..pathStart];
        }

        int portStart = candidate.LastIndexOf(':');
        if (portStart > 0)
        {
            candidate = candidate[..portStart];
        }

        return candidate.Trim().ToLowerInvariant();
    }

    private static string FormatClockDuration(TimeSpan duration)
    {
        TimeSpan safeDuration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;

        return $"{(int)safeDuration.TotalHours:D2}:{safeDuration.Minutes:D2}:{safeDuration.Seconds:D2}";
    }

    private static string FormatBrowserCaptureStatus(DashboardBrowserCaptureStatus status)
        => status switch
        {
            DashboardBrowserCaptureStatus.ExtensionConnected => "Browser extension connected",
            DashboardBrowserCaptureStatus.UiAutomationFallbackActive => "Domain from address bar fallback",
            DashboardBrowserCaptureStatus.Error => "Browser capture error",
            _ => "Browser capture unavailable"
        };
}

public sealed record DashboardCurrentActivityPresentation(
    string AppNameText,
    string ProcessNameText,
    string? CapturedWindowTitle,
    string CurrentWindowTitleText,
    string BrowserDomainText,
    string BrowserCaptureStatusText,
    string CurrentSessionDurationText,
    string? LastPollTimeText,
    string? LastDbWriteTimeText,
    DashboardActiveWebSessionPreview? ActiveWebSessionPreview);

public sealed record DashboardActiveWebSessionPreview(
    string Domain,
    string BrowserFamily,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc);
