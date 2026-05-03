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
              "clientEventId": "chrome-event-1",
              "browserFamily": "Chrome",
              "windowId": 7,
              "tabId": 42,
              "url": "https://docs.microsoft.com/dotnet",
              "title": ".NET docs",
              "observedAtUtc": "2026-04-28T01:02:03Z"
            }
            """;

        var message = ChromeNativeMessageParser.ParseActiveTabChanged(json);

        Assert.Equal("chrome-event-1", message.ClientEventId);
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

    [Fact]
    public void ParseActiveTabChanged_WhenClientEventIdIsMissing_DerivesStableMetadataOnlyId()
    {
        const string firstJson = """
            {
              "type": "activeTabChanged",
              "browserFamily": "Chrome",
              "windowId": 7,
              "tabId": 42,
              "url": "https://example.com/private/path?token=secret-token",
              "title": "Sensitive private title",
              "observedAtUtc": "2026-04-28T01:02:03Z"
            }
            """;
        const string secondJson = """
            {
              "type": "activeTabChanged",
              "browserFamily": "Chrome",
              "windowId": 7,
              "tabId": 42,
              "url": "https://example.com/another/private/path?message=secret-message",
              "title": "Another sensitive title",
              "observedAtUtc": "2026-04-28T01:02:03Z"
            }
            """;

        var first = ChromeNativeMessageParser.ParseActiveTabChanged(firstJson);
        var second = ChromeNativeMessageParser.ParseActiveTabChanged(secondJson);

        Assert.StartsWith("chrome-active-tab:", first.ClientEventId, StringComparison.Ordinal);
        Assert.Equal(first.ClientEventId, second.ClientEventId);
        Assert.DoesNotContain("secret", first.ClientEventId, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private", first.ClientEventId, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("title", first.ClientEventId, StringComparison.OrdinalIgnoreCase);
    }
}
