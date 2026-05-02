using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Devices;

public sealed class DeviceRegistrationService
{
    private readonly MonitorDbContext _dbContext;

    public DeviceRegistrationService(MonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DeviceRegistrationResponse> RegisterAsync(RegisterDeviceRequest request, DateTimeOffset seenAtUtc)
    {
        ArgumentNullException.ThrowIfNull(request);

        DateTimeOffset seenAtUtcNormalized = seenAtUtc.ToUniversalTime();
        DeviceEntity? device = await _dbContext.Devices.FirstOrDefaultAsync(existing =>
            existing.UserId == request.UserId &&
            existing.Platform == request.Platform &&
            existing.DeviceKey == request.DeviceKey);
        bool isNew = device is null;

        if (device is null)
        {
            device = new DeviceEntity
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Platform = request.Platform,
                DeviceKey = request.DeviceKey,
                DeviceName = request.DeviceName,
                TimezoneId = request.TimezoneId,
                DeviceTokenSalt = DeviceTokenFactory.CreateSalt(),
                CreatedAtUtc = seenAtUtcNormalized,
                LastSeenAtUtc = seenAtUtcNormalized
            };
            _dbContext.Devices.Add(device);
        }
        else
        {
            device.DeviceName = request.DeviceName;
            device.TimezoneId = request.TimezoneId;
            device.LastSeenAtUtc = seenAtUtcNormalized;
            if (string.IsNullOrWhiteSpace(device.DeviceTokenSalt))
            {
                device.DeviceTokenSalt = DeviceTokenFactory.CreateSalt();
            }
        }

        string deviceToken = DeviceTokenFactory.CreateToken(device);
        if (string.IsNullOrWhiteSpace(device.DeviceTokenHash))
        {
            device.DeviceTokenHash = DeviceTokenFactory.HashToken(deviceToken);
        }

        await _dbContext.SaveChangesAsync();

        return new DeviceRegistrationResponse(
            device.Id.ToString("N"),
            device.UserId,
            FormatPlatform(device.Platform),
            device.DeviceKey,
            device.DeviceName,
            device.TimezoneId,
            deviceToken,
            device.CreatedAtUtc,
            device.LastSeenAtUtc,
            isNew);
    }

    public async Task<DeviceTokenRotationResponse?> RotateTokenAsync(string deviceId)
    {
        if (!Guid.TryParseExact(deviceId, "N", out Guid parsedDeviceId))
        {
            return null;
        }

        DeviceEntity? device = await _dbContext.Devices
            .SingleOrDefaultAsync(existing => existing.Id == parsedDeviceId);
        if (device is null)
        {
            return null;
        }

        device.DeviceTokenSalt = DeviceTokenFactory.CreateSalt();
        string deviceToken = DeviceTokenFactory.CreateToken(device);
        device.DeviceTokenHash = DeviceTokenFactory.HashToken(deviceToken);

        await _dbContext.SaveChangesAsync();

        return new DeviceTokenRotationResponse(device.Id.ToString("N"), deviceToken);
    }

    private static string FormatPlatform(Platform platform)
        => platform switch
        {
            Platform.Windows => "windows",
            Platform.Android => "android",
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, "Unsupported platform.")
        };

}
