using System.Collections.Concurrent;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;

namespace Woong.MonitorStack.Server.Devices;

public sealed class DeviceRegistrationService
{
    private readonly ConcurrentDictionary<DeviceRegistrationKey, RegisteredDevice> _devices = new();

    public DeviceRegistrationResponse Register(RegisterDeviceRequest request, DateTimeOffset seenAtUtc)
    {
        ArgumentNullException.ThrowIfNull(request);

        var key = new DeviceRegistrationKey(request.UserId, request.Platform, request.DeviceKey);
        bool isNew = false;
        RegisteredDevice device = _devices.AddOrUpdate(
            key,
            _ =>
            {
                isNew = true;
                return RegisteredDevice.Create(request, seenAtUtc);
            },
            (_, existing) => existing.MarkSeen(request, seenAtUtc));

        return new DeviceRegistrationResponse(
            device.DeviceId,
            device.UserId,
            FormatPlatform(device.Platform),
            device.DeviceKey,
            device.DeviceName,
            device.TimezoneId,
            device.CreatedAtUtc,
            device.LastSeenAtUtc,
            isNew);
    }

    private static string FormatPlatform(Platform platform)
        => platform switch
        {
            Platform.Windows => "windows",
            Platform.Android => "android",
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, "Unsupported platform.")
        };

    private sealed record DeviceRegistrationKey(string UserId, Platform Platform, string DeviceKey);

    private sealed record RegisteredDevice(
        string DeviceId,
        string UserId,
        Platform Platform,
        string DeviceKey,
        string DeviceName,
        string TimezoneId,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset LastSeenAtUtc)
    {
        public static RegisteredDevice Create(RegisterDeviceRequest request, DateTimeOffset nowUtc)
            => new(
                Guid.NewGuid().ToString("N"),
                request.UserId,
                request.Platform,
                request.DeviceKey,
                request.DeviceName,
                request.TimezoneId,
                nowUtc.ToUniversalTime(),
                nowUtc.ToUniversalTime());

        public RegisteredDevice MarkSeen(RegisterDeviceRequest request, DateTimeOffset nowUtc)
            => this with
            {
                DeviceName = request.DeviceName,
                TimezoneId = request.TimezoneId,
                LastSeenAtUtc = nowUtc.ToUniversalTime()
            };
    }
}
