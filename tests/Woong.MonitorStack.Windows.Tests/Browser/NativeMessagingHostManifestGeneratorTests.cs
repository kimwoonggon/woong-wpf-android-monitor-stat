using System.Text.Json;
using Woong.MonitorStack.Windows.Browser;

namespace Woong.MonitorStack.Windows.Tests.Browser;

public sealed class NativeMessagingHostManifestGeneratorTests
{
    [Fact]
    public void GenerateJson_CreatesChromeNativeMessagingManifest()
    {
        string json = NativeMessagingHostManifestGenerator.GenerateJson(
            hostName: "com.woong.monitorstack.chrome",
            hostExecutablePath: @"C:\Users\gerard\AppData\Local\WoongMonitor\Woong.MonitorStack.ChromeHost.exe",
            chromeExtensionId: "abcdefghijklmnopabcdefghijklmnop",
            description: "Woong Monitor Chrome native messaging host");

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        Assert.Equal("com.woong.monitorstack.chrome", root.GetProperty("name").GetString());
        Assert.Equal("Woong Monitor Chrome native messaging host", root.GetProperty("description").GetString());
        Assert.Equal(@"C:\Users\gerard\AppData\Local\WoongMonitor\Woong.MonitorStack.ChromeHost.exe", root.GetProperty("path").GetString());
        Assert.Equal("stdio", root.GetProperty("type").GetString());
        JsonElement allowedOrigins = root.GetProperty("allowed_origins");
        Assert.Equal("chrome-extension://abcdefghijklmnopabcdefghijklmnop/", allowedOrigins[0].GetString());
    }
}
