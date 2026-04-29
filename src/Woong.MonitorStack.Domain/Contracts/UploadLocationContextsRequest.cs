namespace Woong.MonitorStack.Domain.Contracts;

public sealed record UploadLocationContextsRequest
{
    public UploadLocationContextsRequest(string deviceId, IReadOnlyList<LocationContextUploadItem> contexts)
    {
        ArgumentNullException.ThrowIfNull(contexts);

        DeviceId = RequiredContractText.Ensure(deviceId, nameof(deviceId));
        Contexts = contexts.Count > 0
            ? contexts
            : throw new ArgumentException("At least one location context is required.", nameof(contexts));
    }

    public string DeviceId { get; }

    public IReadOnlyList<LocationContextUploadItem> Contexts { get; }
}
