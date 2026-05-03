using System.Text;

namespace Woong.MonitorStack.Windows.Sync;

public sealed class FileWindowsSyncTokenStore : IWindowsSyncTokenStore
{
    private readonly IWindowsUserDataProtector _protector;

    public FileWindowsSyncTokenStore(string tokenFilePath, IWindowsUserDataProtector protector)
    {
        TokenFilePath = string.IsNullOrWhiteSpace(tokenFilePath)
            ? throw new ArgumentException("Token file path must not be empty.", nameof(tokenFilePath))
            : tokenFilePath;
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
    }

    public string TokenFilePath { get; }

    public string? GetDeviceToken()
    {
        if (!File.Exists(TokenFilePath))
        {
            return null;
        }

        string protectedText = File.ReadAllText(TokenFilePath, Encoding.UTF8);
        if (string.IsNullOrWhiteSpace(protectedText))
        {
            return null;
        }

        byte[] protectedBytes = Convert.FromBase64String(protectedText);
        byte[] plaintextBytes = _protector.Unprotect(protectedBytes);
        string deviceToken = Encoding.UTF8.GetString(plaintextBytes).Trim();

        return string.IsNullOrWhiteSpace(deviceToken) ? null : deviceToken;
    }

    public void SaveDeviceToken(string deviceToken)
    {
        string normalizedDeviceToken = string.IsNullOrWhiteSpace(deviceToken)
            ? throw new ArgumentException("Device token must not be empty.", nameof(deviceToken))
            : deviceToken.Trim();

        string? directory = Path.GetDirectoryName(TokenFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(normalizedDeviceToken);
        byte[] protectedBytes = _protector.Protect(plaintextBytes);
        File.WriteAllText(TokenFilePath, Convert.ToBase64String(protectedBytes), Encoding.UTF8);
    }

    public void DeleteDeviceToken()
    {
        if (File.Exists(TokenFilePath))
        {
            File.Delete(TokenFilePath);
        }
    }
}
