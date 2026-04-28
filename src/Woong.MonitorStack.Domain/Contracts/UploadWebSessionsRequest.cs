namespace Woong.MonitorStack.Domain.Contracts;

public sealed record UploadWebSessionsRequest
{
    public UploadWebSessionsRequest(string deviceId, IReadOnlyList<WebSessionUploadItem> sessions)
    {
        DeviceId = RequiredContractText.Ensure(deviceId, nameof(deviceId));
        Sessions = sessions.Count > 0 ? sessions : throw new ArgumentException("At least one session is required.", nameof(sessions));
    }

    public string DeviceId { get; }

    public IReadOnlyList<WebSessionUploadItem> Sessions { get; }
}
