namespace Woong.MonitorStack.Server.Data;

public sealed class FocusSessionEntity
{
    public long Id { get; set; }

    public Guid DeviceId { get; set; }

    public string ClientSessionId { get; set; } = "";

    public string PlatformAppKey { get; set; } = "";

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset EndedAtUtc { get; set; }

    public long DurationMs { get; set; }

    public DateOnly LocalDate { get; set; }

    public string TimezoneId { get; set; } = "";

    public bool IsIdle { get; set; }

    public string Source { get; set; } = "";

    public int? ProcessId { get; set; }

    public string? ProcessName { get; set; }

    public string? ProcessPath { get; set; }

    public long? WindowHandle { get; set; }

    public string? WindowTitle { get; set; }
}
