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
        string source,
        int? processId = null,
        string? processName = null,
        string? processPath = null,
        long? windowHandle = null,
        string? windowTitle = null)
    {
        ClientSessionId = RequireNonEmpty(clientSessionId, nameof(clientSessionId));
        DeviceId = RequireNonEmpty(deviceId, nameof(deviceId));
        PlatformAppKey = RequireNonEmpty(platformAppKey, nameof(platformAppKey));
        Range = range;
        LocalDate = localDate;
        TimezoneId = RequireNonEmpty(timezoneId, nameof(timezoneId));
        IsIdle = isIdle;
        Source = RequireNonEmpty(source, nameof(source));
        ProcessId = processId;
        ProcessName = NormalizeOptional(processName);
        ProcessPath = NormalizeOptional(processPath);
        WindowHandle = windowHandle;
        WindowTitle = NormalizeOptional(windowTitle);
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

    public int? ProcessId { get; }

    public string? ProcessName { get; }

    public string? ProcessPath { get; }

    public long? WindowHandle { get; }

    public string? WindowTitle { get; }

    public static FocusSession FromUtc(
        string clientSessionId,
        string deviceId,
        string platformAppKey,
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc,
        string timezoneId,
        bool isIdle,
        string source,
        int? processId = null,
        string? processName = null,
        string? processPath = null,
        long? windowHandle = null,
        string? windowTitle = null)
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
            source,
            processId,
            processName,
            processPath,
            windowHandle,
            windowTitle);
    }

    private static string RequireNonEmpty(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value must not be empty.", parameterName)
            : value;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
