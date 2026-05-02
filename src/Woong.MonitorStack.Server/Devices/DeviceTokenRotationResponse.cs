namespace Woong.MonitorStack.Server.Devices;

public sealed record DeviceTokenRotationResponse(
    string DeviceId,
    string DeviceToken);
