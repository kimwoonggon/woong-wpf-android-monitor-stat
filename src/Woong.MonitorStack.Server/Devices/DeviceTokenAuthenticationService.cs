using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Devices;

public sealed class DeviceTokenAuthenticationService
{
    public const string HeaderName = "X-Device-Token";

    private readonly MonitorDbContext _dbContext;

    public DeviceTokenAuthenticationService(MonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsAuthorizedAsync(
        string deviceId,
        string? deviceToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceToken) ||
            !Guid.TryParseExact(deviceId, "N", out Guid parsedDeviceId))
        {
            return false;
        }

        string? expectedHash = await _dbContext.Devices
            .Where(device => device.Id == parsedDeviceId)
            .Select(device => device.DeviceTokenHash)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            return false;
        }

        string actualHash = DeviceTokenFactory.HashToken(deviceToken);

        return FixedTimeEquals(expectedHash, actualHash);
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] actualBytes = Encoding.UTF8.GetBytes(actual);

        return expectedBytes.Length == actualBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
