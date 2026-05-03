using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Domain.Contracts;

public sealed record CurrentAppStateUploadItem
{
    public CurrentAppStateUploadItem(
        string clientStateId,
        Platform platform,
        string platformAppKey,
        DateTimeOffset observedAtUtc,
        DateOnly localDate,
        string timezoneId,
        string status,
        string source,
        int? processId = null,
        string? processName = null,
        string? processPath = null,
        long? windowHandle = null,
        string? windowTitle = null)
    {
        ClientStateId = RequiredContractText.Ensure(clientStateId, nameof(clientStateId));
        Platform = platform;
        PlatformAppKey = RequiredContractText.Ensure(platformAppKey, nameof(platformAppKey));
        ObservedAtUtc = observedAtUtc.ToUniversalTime();
        LocalDate = localDate;
        TimezoneId = RequiredContractText.Ensure(timezoneId, nameof(timezoneId));
        Status = RequiredContractText.Ensure(status, nameof(status));
        Source = RequiredContractText.Ensure(source, nameof(source));
        ProcessId = processId;
        ProcessName = NormalizeOptional(processName);
        ProcessPath = NormalizeOptional(processPath);
        WindowHandle = windowHandle;
        WindowTitle = NormalizeOptional(windowTitle);
    }

    public string ClientStateId { get; }

    public Platform Platform { get; }

    public string PlatformAppKey { get; }

    public DateTimeOffset ObservedAtUtc { get; }

    public DateOnly LocalDate { get; }

    public string TimezoneId { get; }

    public string Status { get; }

    public string Source { get; }

    public int? ProcessId { get; }

    public string? ProcessName { get; }

    public string? ProcessPath { get; }

    public long? WindowHandle { get; }

    public string? WindowTitle { get; }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
