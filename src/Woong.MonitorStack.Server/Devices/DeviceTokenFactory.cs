using System.Security.Cryptography;
using System.Text;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Devices;

internal static class DeviceTokenFactory
{
    private const string TokenPrefix = "wms_dev_";

    public static string CreateSalt()
        => Base64Url(RandomNumberGenerator.GetBytes(32));

    public static string CreateToken(DeviceEntity device)
    {
        byte[] salt = DecodeBase64Url(device.DeviceTokenSalt);
        byte[] payload = Encoding.UTF8.GetBytes(CreateStablePayload(device));
        byte[] signature = HMACSHA256.HashData(salt, payload);

        return TokenPrefix + Base64Url(signature);
    }

    public static string HashToken(string token)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        return Base64Url(hash);
    }

    private static string CreateStablePayload(DeviceEntity device)
        => string.Join(
            ":",
            device.Id.ToString("N"),
            device.UserId,
            FormatPlatform(device.Platform),
            device.DeviceKey,
            device.CreatedAtUtc.ToUniversalTime().ToUnixTimeMilliseconds());

    private static string FormatPlatform(Platform platform)
        => platform switch
        {
            Platform.Windows => "windows",
            Platform.Android => "android",
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, "Unsupported platform.")
        };

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] DecodeBase64Url(string value)
    {
        string padded = value
            .Replace('-', '+')
            .Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');

        return Convert.FromBase64String(padded);
    }
}
