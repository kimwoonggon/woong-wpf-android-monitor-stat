using System.Text.Json;

namespace Woong.MonitorStack.Windows.Tests.Browser;

public sealed class ChromeExtensionManifestTests
{
    [Fact]
    public void Manifest_DeclaresMv3ServiceWorkerAndRequiredPermissions()
    {
        var manifestPath = Path.Combine(FindRepositoryRoot(), "extensions", "chrome", "manifest.json");
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal(3, root.GetProperty("manifest_version").GetInt32());
        Assert.Equal("background.js", root.GetProperty("background").GetProperty("service_worker").GetString());

        var permissions = root
            .GetProperty("permissions")
            .EnumerateArray()
            .Select(permission => permission.GetString())
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("tabs", permissions);
        Assert.Contains("webNavigation", permissions);
        Assert.Contains("nativeMessaging", permissions);
    }

    [Fact]
    public void BackgroundScript_UsesPersistentNativePortForMultipleTabEvents()
    {
        var backgroundPath = Path.Combine(FindRepositoryRoot(), "extensions", "chrome", "background.js");

        string script = File.ReadAllText(backgroundPath);

        Assert.Contains("chrome.runtime.connectNative", script, StringComparison.Ordinal);
        Assert.Contains("port.postMessage", script, StringComparison.Ordinal);
        Assert.DoesNotContain("sendNativeMessage", script, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "total_todolist.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
