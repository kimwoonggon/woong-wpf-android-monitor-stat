namespace Woong.MonitorStack.Server.Data;

public sealed class RawEventEntity
{
    public long Id { get; set; }

    public Guid DeviceId { get; set; }

    public string ClientEventId { get; set; } = "";

    public string EventType { get; set; } = "";

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string PayloadJson { get; set; } = "";
}
