namespace Woong.MonitorStack.Domain.Contracts;

public sealed record RawEventUploadItem
{
    public RawEventUploadItem(
        string clientEventId,
        string eventType,
        DateTimeOffset occurredAtUtc,
        string payloadJson)
    {
        ClientEventId = RequiredContractText.Ensure(clientEventId, nameof(clientEventId));
        EventType = RequiredContractText.Ensure(eventType, nameof(eventType));
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
        PayloadJson = RequiredContractText.Ensure(payloadJson, nameof(payloadJson));
    }

    public string ClientEventId { get; }

    public string EventType { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public string PayloadJson { get; }
}
