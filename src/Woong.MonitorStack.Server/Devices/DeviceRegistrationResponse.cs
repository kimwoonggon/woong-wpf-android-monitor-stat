namespace Woong.MonitorStack.Server.Devices;

public sealed record DeviceRegistrationResponse(
    string DeviceId,
    string UserId,
    string Platform,
    string DeviceKey,
    string DeviceName,
    string TimezoneId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastSeenAtUtc,
    bool IsNew);
