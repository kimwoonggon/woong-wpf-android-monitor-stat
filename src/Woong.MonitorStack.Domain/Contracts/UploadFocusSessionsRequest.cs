namespace Woong.MonitorStack.Domain.Contracts;

public sealed record UploadFocusSessionsRequest
{
    public UploadFocusSessionsRequest(string deviceId, IReadOnlyList<FocusSessionUploadItem> sessions)
    {
        DeviceId = RequiredContractText.Ensure(deviceId, nameof(deviceId));
        Sessions = sessions.Count > 0 ? sessions : throw new ArgumentException("At least one session is required.", nameof(sessions));
    }

    public string DeviceId { get; }

    public IReadOnlyList<FocusSessionUploadItem> Sessions { get; }
}
