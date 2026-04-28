using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Domain.Contracts;

public sealed record RegisterDeviceRequest
{
    public RegisterDeviceRequest(
        string userId,
        Platform platform,
        string deviceKey,
        string deviceName,
        string timezoneId)
    {
        UserId = RequireNonEmpty(userId, nameof(userId));
        Platform = platform;
        DeviceKey = RequireNonEmpty(deviceKey, nameof(deviceKey));
        DeviceName = RequireNonEmpty(deviceName, nameof(deviceName));
        TimezoneId = RequireNonEmpty(timezoneId, nameof(timezoneId));
    }

    public string UserId { get; }

    public Platform Platform { get; }

    public string DeviceKey { get; }

    public string DeviceName { get; }

    public string TimezoneId { get; }

    private static string RequireNonEmpty(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value must not be empty.", parameterName)
            : value;
}
