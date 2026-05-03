using System.Text;
using Woong.MonitorStack.Windows.Sync;

namespace Woong.MonitorStack.Windows.Tests.Sync;

public sealed class FileWindowsSyncTokenStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"woong-sync-token-{Guid.NewGuid():N}");

    [Fact]
    public void SaveDeviceToken_StoresProtectedBytesWithoutPlaintextLeakage()
    {
        string tokenPath = Path.Combine(_directory, "device-token.bin");
        var store = new FileWindowsSyncTokenStore(tokenPath, new PrefixProtector());

        store.SaveDeviceToken(" secret-device-token ");

        Assert.Equal("secret-device-token", store.GetDeviceToken());
        string storedText = File.ReadAllText(tokenPath, Encoding.UTF8);
        Assert.DoesNotContain("secret-device-token", storedText, StringComparison.Ordinal);
        Assert.DoesNotContain("device-token", storedText, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteDeviceToken_RemovesStoredToken()
    {
        string tokenPath = Path.Combine(_directory, "device-token.bin");
        var store = new FileWindowsSyncTokenStore(tokenPath, new PrefixProtector());
        store.SaveDeviceToken("device-token-1");

        store.DeleteDeviceToken();

        Assert.Null(store.GetDeviceToken());
        Assert.False(File.Exists(tokenPath));
    }

    [Fact]
    public void SaveDeviceToken_WhenTokenIsMissing_ThrowsWithoutCreatingFile()
    {
        string tokenPath = Path.Combine(_directory, "device-token.bin");
        var store = new FileWindowsSyncTokenStore(tokenPath, new PrefixProtector());

        ArgumentException exception = Assert.Throws<ArgumentException>(() => store.SaveDeviceToken(" "));

        Assert.DoesNotContain(tokenPath, exception.Message, StringComparison.Ordinal);
        Assert.False(File.Exists(tokenPath));
    }

    [Fact]
    public void DpapiWindowsUserDataProtector_WhenWindowsIsAvailable_RoundTripsWithoutPlaintext()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var protector = new DpapiWindowsUserDataProtector();
        byte[] plaintext = Encoding.UTF8.GetBytes("secret-device-token");

        byte[] protectedBytes = protector.Protect(plaintext);

        Assert.NotEqual(plaintext, protectedBytes);
        Assert.DoesNotContain("secret-device-token", Encoding.UTF8.GetString(protectedBytes), StringComparison.Ordinal);
        Assert.Equal(plaintext, protector.Unprotect(protectedBytes));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private sealed class PrefixProtector : IWindowsUserDataProtector
    {
        public byte[] Protect(byte[] plaintext)
            => Encoding.UTF8.GetBytes($"protected:{Convert.ToBase64String(plaintext)}");

        public byte[] Unprotect(byte[] protectedData)
        {
            string text = Encoding.UTF8.GetString(protectedData);
            return Convert.FromBase64String(text["protected:".Length..]);
        }
    }
}
