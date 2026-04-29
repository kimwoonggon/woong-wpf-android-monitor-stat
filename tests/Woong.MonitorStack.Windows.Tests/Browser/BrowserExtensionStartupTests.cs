namespace Woong.MonitorStack.Windows.Tests.Browser;

public sealed class BrowserExtensionStartupTests
{
    [Fact]
    public void ChromeExtension_ReportsCurrentActiveTabOnStartupAndNativeReconnect()
    {
        string backgroundScript = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "extensions",
            "chrome",
            "background.js"));

        Assert.Contains("reportCurrentActiveTab", backgroundScript, StringComparison.Ordinal);
        Assert.Contains("chrome.runtime.onStartup", backgroundScript, StringComparison.Ordinal);
        Assert.Contains("chrome.runtime.onInstalled", backgroundScript, StringComparison.Ordinal);
        Assert.Contains("nativePort.onDisconnect", backgroundScript, StringComparison.Ordinal);
        Assert.Contains("reportCurrentActiveTab();", backgroundScript, StringComparison.Ordinal);
    }

    [Fact]
    public void ChromeExtension_DetectsChromiumBrowserFamilyInsteadOfHardcodingChromeOnly()
    {
        string backgroundScript = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "extensions",
            "chrome",
            "background.js"));

        Assert.Contains("detectBrowserFamily", backgroundScript, StringComparison.Ordinal);
        Assert.Contains("Microsoft Edge", backgroundScript, StringComparison.Ordinal);
        Assert.Contains("Brave", backgroundScript, StringComparison.Ordinal);
        Assert.DoesNotContain("const browserFamily = \"Chrome\";", backgroundScript, StringComparison.Ordinal);
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
