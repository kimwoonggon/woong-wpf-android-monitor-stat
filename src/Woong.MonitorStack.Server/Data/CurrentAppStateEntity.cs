using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Server.Data;

public sealed class CurrentAppStateEntity
{
    public long Id { get; set; }

    public Guid DeviceId { get; set; }

    public string ClientStateId { get; set; } = "";

    public Platform Platform { get; set; }

    public string PlatformAppKey { get; set; } = "";

    public DateTimeOffset ObservedAtUtc { get; set; }

    public DateOnly LocalDate { get; set; }

    public string TimezoneId { get; set; } = "";

    public string Status { get; set; } = "";

    public string Source { get; set; } = "";

    public int? ProcessId { get; set; }

    public string? ProcessName { get; set; }

    public string? ProcessPath { get; set; }

    public long? WindowHandle { get; set; }

    public string? WindowTitle { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
