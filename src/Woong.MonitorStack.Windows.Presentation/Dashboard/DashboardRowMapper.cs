using System.Globalization;
using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed class DashboardRowMapper
{
    private readonly TimeZoneInfo _timeZone;

    public DashboardRowMapper(TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        _timeZone = timeZone;
    }

    public IReadOnlyList<DashboardSessionRow> BuildRecentSessionRows(
        IEnumerable<FocusSession> focusSessions,
        bool isWindowTitleVisible)
        => focusSessions
            .OrderByDescending(session => session.StartedAtUtc)
            .Select(session => new DashboardSessionRow(
                session.PlatformAppKey,
                TextOrDefault(session.ProcessName, session.PlatformAppKey),
                FormatLocalTime(session.StartedAtUtc),
                FormatLocalTime(session.EndedAtUtc),
                FormatSessionDuration(session.DurationMs),
                session.IsIdle ? "Idle" : "Active",
                isWindowTitleVisible
                    ? TextOrDefault(session.WindowTitle, "No window title")
                    : "Hidden by privacy setting",
                session.Source,
                session.IsIdle,
                session.ProcessPath))
            .ToList();

    public IReadOnlyList<DashboardWebSessionRow> BuildRecentWebSessionRows(
        IEnumerable<WebSession> webSessions,
        bool isWindowTitleVisible)
        => webSessions
            .OrderByDescending(session => session.StartedAtUtc)
            .Select(session => new DashboardWebSessionRow(
                session.Domain,
                isWindowTitleVisible
                    ? TextOrDefault(session.PageTitle, "No page title")
                    : "Page title hidden by privacy settings",
                FormatUrlMode(session),
                FormatLocalTime(session.StartedAtUtc),
                FormatLocalTime(session.EndedAtUtc),
                FormatSessionDuration(session.DurationMs),
                session.BrowserFamily,
                TextOrDefault(session.CaptureConfidence, "Unknown")))
            .ToList();

    public IReadOnlyList<DashboardEventLogRow> BuildLiveEventRows(
        IEnumerable<FocusSession> focusSessions,
        IEnumerable<WebSession> webSessions)
    {
        IEnumerable<(DateTimeOffset OccurredAtUtc, DashboardEventLogRow Row)> focusRows = focusSessions
            .Select(session => (
                session.StartedAtUtc,
                new DashboardEventLogRow(
                    "Focus",
                    FormatLocalTime(session.StartedAtUtc),
                    session.PlatformAppKey,
                    "",
                    session.PlatformAppKey)));
        IEnumerable<(DateTimeOffset OccurredAtUtc, DashboardEventLogRow Row)> webRows = webSessions
            .Select(session => (
                session.StartedAtUtc,
                new DashboardEventLogRow(
                    "Web",
                    FormatLocalTime(session.StartedAtUtc),
                    "",
                    session.Domain,
                    session.Domain)));

        return focusRows
            .Concat(webRows)
            .OrderByDescending(row => row.OccurredAtUtc)
            .Select(row => row.Row)
            .ToList();
    }

    public DashboardEventLogRow BuildRuntimeEventRow(
        DateTimeOffset occurredAtUtc,
        string eventType,
        string appName,
        string domain,
        string message)
        => new(eventType, FormatLocalTime(occurredAtUtc), appName, domain, message);

    public string FormatLocalTime(DateTimeOffset utcValue)
        => TimeZoneInfo.ConvertTime(utcValue, _timeZone).ToString("HH:mm", CultureInfo.InvariantCulture);

    private static string TextOrDefault(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string FormatUrlMode(WebSession session)
    {
        if (string.IsNullOrWhiteSpace(session.Url))
        {
            return "Domain only";
        }

        return Uri.TryCreate(session.Url, UriKind.Absolute, out Uri? uri) &&
               string.Equals($"{uri.GetLeftPart(UriPartial.Authority)}/", session.Url, StringComparison.OrdinalIgnoreCase)
            ? "Domain only"
            : "Full URL disabled";
    }

    private static string FormatSessionDuration(long durationMs)
    {
        if (durationMs <= 0)
        {
            return "0s";
        }

        long totalSeconds = Math.Max(1, (durationMs + 999) / 1_000);
        long hours = totalSeconds / 3_600;
        long minutes = totalSeconds % 3_600 / 60;
        long seconds = totalSeconds % 60;

        if (hours > 0)
        {
            return seconds > 0
                ? $"{hours}h {minutes:D2}m {seconds:D2}s"
                : $"{hours}h {minutes:D2}m";
        }

        if (minutes > 0)
        {
            return seconds > 0
                ? $"{minutes}m {seconds:D2}s"
                : $"{minutes}m";
        }

        return $"{seconds}s";
    }
}
