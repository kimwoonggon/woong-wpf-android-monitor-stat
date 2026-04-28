namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed record DashboardTrackingSnapshot(
    string? AppName,
    string? ProcessName,
    string? WindowTitle,
    TimeSpan CurrentSessionDuration,
    DashboardPersistedSessionSnapshot? LastPersistedSession)
{
    public static DashboardTrackingSnapshot Empty { get; } = new(
        AppName: null,
        ProcessName: null,
        WindowTitle: null,
        CurrentSessionDuration: TimeSpan.Zero,
        LastPersistedSession: null);
}

public sealed record DashboardPersistedSessionSnapshot(
    string? AppName,
    string? ProcessName,
    DateTimeOffset EndedAtUtc,
    TimeSpan Duration)
{
    public string ToDisplayText(string timezoneId)
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        string appName = string.IsNullOrWhiteSpace(AppName)
            ? string.IsNullOrWhiteSpace(ProcessName) ? "Unknown app" : ProcessName
            : AppName;
        string endedAtLocal = TimeZoneInfo
            .ConvertTime(EndedAtUtc, timeZone)
            .ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        string duration = FormatDuration((long)Math.Max(0, Duration.TotalMilliseconds));

        return $"{appName} persisted at {endedAtLocal} for {duration}";
    }

    private static string FormatDuration(long durationMs)
    {
        if (durationMs <= 0)
        {
            return "0m";
        }

        long totalMinutes = durationMs / 60_000;
        long hours = totalMinutes / 60;
        long minutes = totalMinutes % 60;

        return hours > 0
            ? $"{hours}h {minutes:D2}m"
            : $"{Math.Max(1, minutes)}m";
    }
}
