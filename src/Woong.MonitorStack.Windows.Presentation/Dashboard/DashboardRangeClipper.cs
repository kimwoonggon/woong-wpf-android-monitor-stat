using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

internal static class DashboardRangeClipper
{
    public static IReadOnlyList<FocusSession> ClipFocusSessions(
        IEnumerable<FocusSession> sessions,
        TimeRange range)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(range);

        List<FocusSession> clippedSessions = [];
        foreach (FocusSession session in sessions)
        {
            TimeRange? clippedRange = ClipRange(session.Range, range);
            if (clippedRange is null)
            {
                continue;
            }

            clippedSessions.Add(new FocusSession(
                session.ClientSessionId,
                session.DeviceId,
                session.PlatformAppKey,
                clippedRange,
                session.LocalDate,
                session.TimezoneId,
                session.IsIdle,
                session.Source,
                session.ProcessId,
                session.ProcessName,
                session.ProcessPath,
                session.WindowHandle,
                session.WindowTitle));
        }

        return clippedSessions;
    }

    public static IReadOnlyList<WebSession> ClipWebSessions(
        IEnumerable<WebSession> sessions,
        TimeRange range)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(range);

        List<WebSession> clippedSessions = [];
        foreach (WebSession session in sessions)
        {
            TimeRange? clippedRange = ClipRange(session.Range, range);
            if (clippedRange is null)
            {
                continue;
            }

            clippedSessions.Add(new WebSession(
                session.FocusSessionId,
                session.BrowserFamily,
                session.Url,
                session.Domain,
                session.PageTitle,
                clippedRange,
                session.CaptureMethod,
                session.CaptureConfidence,
                session.IsPrivateOrUnknown));
        }

        return clippedSessions;
    }

    private static TimeRange? ClipRange(TimeRange sessionRange, TimeRange selectedRange)
    {
        DateTimeOffset startedAtUtc = sessionRange.StartedAtUtc > selectedRange.StartedAtUtc
            ? sessionRange.StartedAtUtc
            : selectedRange.StartedAtUtc;
        DateTimeOffset endedAtUtc = sessionRange.EndedAtUtc < selectedRange.EndedAtUtc
            ? sessionRange.EndedAtUtc
            : selectedRange.EndedAtUtc;

        return endedAtUtc <= startedAtUtc
            ? null
            : TimeRange.FromUtc(startedAtUtc, endedAtUtc);
    }
}
