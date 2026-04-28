namespace Woong.MonitorStack.Domain.Contracts;

public sealed record FocusSessionUploadItem
{
    public FocusSessionUploadItem(
        string clientSessionId,
        string platformAppKey,
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc,
        long durationMs,
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
        ClientSessionId = RequiredContractText.Ensure(clientSessionId, nameof(clientSessionId));
        PlatformAppKey = RequiredContractText.Ensure(platformAppKey, nameof(platformAppKey));
        StartedAtUtc = startedAtUtc.ToUniversalTime();
        EndedAtUtc = endedAtUtc.ToUniversalTime();
        DurationMs = durationMs > 0 ? durationMs : throw new ArgumentOutOfRangeException(nameof(durationMs));
        LocalDate = localDate;
        TimezoneId = RequiredContractText.Ensure(timezoneId, nameof(timezoneId));
        IsIdle = isIdle;
        Source = RequiredContractText.Ensure(source, nameof(source));
        ProcessId = processId;
        ProcessName = NormalizeOptional(processName);
        ProcessPath = NormalizeOptional(processPath);
        WindowHandle = windowHandle;
        WindowTitle = NormalizeOptional(windowTitle);
    }

    public string ClientSessionId { get; }

    public string PlatformAppKey { get; }

    public DateTimeOffset StartedAtUtc { get; }

    public DateTimeOffset EndedAtUtc { get; }

    public long DurationMs { get; }

    public DateOnly LocalDate { get; }

    public string TimezoneId { get; }

    public bool IsIdle { get; }

    public string Source { get; }

    public int? ProcessId { get; }

    public string? ProcessName { get; }

    public string? ProcessPath { get; }

    public long? WindowHandle { get; }

    public string? WindowTitle { get; }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
