namespace Woong.MonitorStack.Domain.Contracts;

public sealed record UploadRawEventsRequest
{
    public UploadRawEventsRequest(string deviceId, IReadOnlyList<RawEventUploadItem> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        DeviceId = RequiredContractText.Ensure(deviceId, nameof(deviceId));
        Events = events.Count > 0 ? events : throw new ArgumentException("At least one event is required.", nameof(events));
    }

    public string DeviceId { get; }

    public IReadOnlyList<RawEventUploadItem> Events { get; }
}
