using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Server.Data;

public sealed class DeviceEntity
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = "";

    public Platform Platform { get; set; }

    public string DeviceKey { get; set; } = "";

    public string DeviceName { get; set; } = "";

    public string TimezoneId { get; set; } = "";

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset LastSeenAtUtc { get; set; }
}
