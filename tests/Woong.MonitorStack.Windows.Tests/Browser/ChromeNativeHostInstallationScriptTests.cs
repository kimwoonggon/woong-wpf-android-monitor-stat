namespace Woong.MonitorStack.Windows.Tests.Browser;

public sealed class ChromeNativeHostInstallationScriptTests
{
    [Fact]
    public void InstallScript_RegistersStableChromeNativeHostForCurrentUser()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "install-chrome-native-host.ps1");

        string script = File.ReadAllText(scriptPath);

        Assert.Contains("com.woong.monitorstack.chrome", script, StringComparison.Ordinal);
        Assert.Contains("ChromeExtensionId", script, StringComparison.Ordinal);
        Assert.Contains("NativeMessagingHosts", script, StringComparison.Ordinal);
        Assert.Contains("Woong.MonitorStack.ChromeNativeHost", script, StringComparison.Ordinal);
        Assert.Contains("HKCU", script, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            if (File.Exists(Path.Combine(directory, "Woong.MonitorStack.sln")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
