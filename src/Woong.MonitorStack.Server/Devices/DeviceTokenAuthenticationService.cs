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
        => await IsAuthorizedAsync(
            deviceId,
            deviceToken,
            authenticatedUserId: null,
            requireAuthenticatedUser: false,
            cancellationToken);

    public async Task<bool> IsAuthorizedAsync(
        string deviceId,
        string? deviceToken,
        string? authenticatedUserId,
        bool requireAuthenticatedUser,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceToken) ||
            !Guid.TryParseExact(deviceId, "N", out Guid parsedDeviceId))
        {
            return false;
        }

        DeviceTokenVerifier? verifier = await _dbContext.Devices
            .Where(device => device.Id == parsedDeviceId)
            .Select(device => new DeviceTokenVerifier(device.UserId, device.DeviceTokenHash))
            .SingleOrDefaultAsync(cancellationToken);

        if (verifier is null || string.IsNullOrWhiteSpace(verifier.DeviceTokenHash))
        {
            return false;
        }

        if (requireAuthenticatedUser && string.IsNullOrWhiteSpace(authenticatedUserId))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(authenticatedUserId) &&
            !string.Equals(verifier.UserId, authenticatedUserId, StringComparison.Ordinal))
        {
            return false;
        }

        string actualHash = DeviceTokenFactory.HashToken(deviceToken);

        return FixedTimeEquals(verifier.DeviceTokenHash, actualHash);
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] actualBytes = Encoding.UTF8.GetBytes(actual);

        return expectedBytes.Length == actualBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private sealed record DeviceTokenVerifier(string UserId, string DeviceTokenHash);
}
