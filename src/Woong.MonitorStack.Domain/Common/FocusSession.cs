namespace Woong.MonitorStack.Domain.Common;

public sealed record FocusSession
{
    public FocusSession(
        string clientSessionId,
        string deviceId,
        string platformAppKey,
        TimeRange range,
        DateOnly localDate,
        string timezoneId,
        bool isIdle,
        string source)
    {
        ClientSessionId = RequireNonEmpty(clientSessionId, nameof(clientSessionId));
        DeviceId = RequireNonEmpty(deviceId, nameof(deviceId));
        PlatformAppKey = RequireNonEmpty(platformAppKey, nameof(platformAppKey));
        Range = range;
        LocalDate = localDate;
        TimezoneId = RequireNonEmpty(timezoneId, nameof(timezoneId));
        IsIdle = isIdle;
        Source = RequireNonEmpty(source, nameof(source));
    }

    public string ClientSessionId { get; }

    public string DeviceId { get; }

    public string PlatformAppKey { get; }

    public TimeRange Range { get; }

    public DateTimeOffset StartedAtUtc => Range.StartedAtUtc;

    public DateTimeOffset EndedAtUtc => Range.EndedAtUtc;

    public long DurationMs => Convert.ToInt64(Range.Duration.TotalMilliseconds);

    public DateOnly LocalDate { get; }

    public string TimezoneId { get; }

    public bool IsIdle { get; }

    public string Source { get; }

    public static FocusSession FromUtc(
        string clientSessionId,
        string deviceId,
        string platformAppKey,
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc,
        string timezoneId,
        bool isIdle,
        string source)
    {
        var range = TimeRange.FromUtc(startedAtUtc, endedAtUtc);

        return new FocusSession(
            clientSessionId,
            deviceId,
            platformAppKey,
            range,
            LocalDateCalculator.GetLocalDate(range.StartedAtUtc, timezoneId),
            timezoneId,
            isIdle,
            source);
    }

    private static string RequireNonEmpty(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value must not be empty.", parameterName)
            : value;
}
