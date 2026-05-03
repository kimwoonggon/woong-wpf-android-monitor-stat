namespace Woong.MonitorStack.Windows.Storage;

public sealed record CurrentAppStateRecord
{
    public CurrentAppStateRecord(
        string deviceId,
        string platformAppKey,
        int? processId,
        string? processName,
        string? processPath,
        long? windowHandle,
        DateTimeOffset observedAtUtc,
        DateOnly? localDate = null,
        string? timezoneId = null,
        string status = "Active",
        string source = "windows_current_app_state",
        string? clientStateId = null)
    {
        DeviceId = RequiredStorageText.Ensure(deviceId, nameof(deviceId));
        PlatformAppKey = RequiredStorageText.Ensure(platformAppKey, nameof(platformAppKey));
        ProcessId = processId;
        ProcessName = string.IsNullOrWhiteSpace(processName) ? null : processName.Trim();
        ProcessPath = string.IsNullOrWhiteSpace(processPath) ? null : processPath.Trim();
        WindowHandle = windowHandle;
        ObservedAtUtc = observedAtUtc.ToUniversalTime();
        LocalDate = localDate ?? DateOnly.FromDateTime(ObservedAtUtc.UtcDateTime);
        TimezoneId = string.IsNullOrWhiteSpace(timezoneId) ? "UTC" : timezoneId.Trim();
        Status = RequiredStorageText.Ensure(status, nameof(status));
        Source = RequiredStorageText.Ensure(source, nameof(source));
        ClientStateId = string.IsNullOrWhiteSpace(clientStateId)
            ? CreateClientStateId(DeviceId, ObservedAtUtc)
            : clientStateId.Trim();
    }

    public string ClientStateId { get; }

    public string DeviceId { get; }

    public string PlatformAppKey { get; }

    public int? ProcessId { get; }

    public string? ProcessName { get; }

    public string? ProcessPath { get; }

    public long? WindowHandle { get; }

    public DateTimeOffset ObservedAtUtc { get; }

    public DateOnly LocalDate { get; }

    public string TimezoneId { get; }

    public string Status { get; }

    public string Source { get; }

    private static string CreateClientStateId(string deviceId, DateTimeOffset observedAtUtc)
        => $"windows-current:{deviceId}:{observedAtUtc.UtcTicks}";
}
