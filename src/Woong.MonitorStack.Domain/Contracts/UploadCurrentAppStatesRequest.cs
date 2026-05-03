namespace Woong.MonitorStack.Domain.Contracts;

public sealed record UploadCurrentAppStatesRequest
{
    public UploadCurrentAppStatesRequest(string deviceId, IReadOnlyList<CurrentAppStateUploadItem> states)
    {
        ArgumentNullException.ThrowIfNull(states);

        DeviceId = RequiredContractText.Ensure(deviceId, nameof(deviceId));
        States = states.Count > 0 ? states : throw new ArgumentException("At least one state is required.", nameof(states));
    }

    public string DeviceId { get; }

    public IReadOnlyList<CurrentAppStateUploadItem> States { get; }
}
