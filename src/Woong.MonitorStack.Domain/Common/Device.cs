namespace Woong.MonitorStack.Domain.Common;

public sealed record Device
{
    public Device(
        string id,
        string userId,
        Platform platform,
        string deviceKey,
        string deviceName,
        string timezoneId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? lastSeenAtUtc)
    {
        Id = RequiredText.Ensure(id, nameof(id));
        UserId = RequiredText.Ensure(userId, nameof(userId));
        Platform = platform;
        DeviceKey = RequiredText.Ensure(deviceKey, nameof(deviceKey));
        DeviceName = RequiredText.Ensure(deviceName, nameof(deviceName));
        TimezoneId = RequiredText.Ensure(timezoneId, nameof(timezoneId));
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        LastSeenAtUtc = lastSeenAtUtc?.ToUniversalTime();
    }

    public string Id { get; }

    public string UserId { get; }

    public Platform Platform { get; }

    public string DeviceKey { get; }

    public string DeviceName { get; }

    public string TimezoneId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? LastSeenAtUtc { get; }
}
