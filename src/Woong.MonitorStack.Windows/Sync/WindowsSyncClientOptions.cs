namespace Woong.MonitorStack.Windows.Sync;

public sealed record WindowsSyncClientOptions
{
    public WindowsSyncClientOptions(string deviceToken)
    {
        DeviceToken = string.IsNullOrWhiteSpace(deviceToken)
            ? throw new ArgumentException("Value must not be empty.", nameof(deviceToken))
            : deviceToken;
    }

    public string DeviceToken { get; }
}
