using Woong.MonitorStack.Windows.Browser;

namespace Woong.MonitorStack.Windows.Tests.Browser;

public sealed class ChromeNativeMessageParserTests
{
    [Fact]
    public void ParseActiveTabChanged_MapsExtensionJsonToTabMessage()
    {
        const string json = """
            {
              "type": "activeTabChanged",
              "browserFamily": "Chrome",
              "windowId": 7,
              "tabId": 42,
              "url": "https://docs.microsoft.com/dotnet",
              "title": ".NET docs",
              "observedAtUtc": "2026-04-28T01:02:03Z"
            }
            """;

        var message = ChromeNativeMessageParser.ParseActiveTabChanged(json);

        Assert.Equal("Chrome", message.BrowserFamily);
        Assert.Equal(7, message.WindowId);
        Assert.Equal(42, message.TabId);
        Assert.Equal("microsoft.com", message.Domain);
        Assert.Equal(new DateTimeOffset(2026, 4, 28, 1, 2, 3, TimeSpan.Zero), message.ObservedAtUtc);
    }

    [Fact]
    public void ParseActiveTabChanged_AllowsEmptyTitleBecauseChromeCanReportBeforeTitleLoads()
    {
        const string json = """
            {
              "type": "activeTabChanged",
              "browserFamily": "Chrome",
              "windowId": 7,
              "tabId": 42,
              "url": "https://github.example/start.html",
              "title": "",
              "observedAtUtc": "2026-04-28T01:02:03Z"
            }
            """;

        var message = ChromeNativeMessageParser.ParseActiveTabChanged(json);

        Assert.Equal("", message.Title);
        Assert.Equal("github.example", message.Domain);
    }
}
