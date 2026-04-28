using Woong.MonitorStack.Windows.Browser;

namespace Woong.MonitorStack.Windows.Tests.Browser;

public sealed class ChromeTabChangedMessageTests
{
    [Fact]
    public void FromExtensionPayload_ExtractsRegistrableDomain()
    {
        var message = ChromeTabChangedMessage.FromExtensionPayload(
            windowId: 7,
            tabId: 42,
            url: "https://www.youtube.com/watch?v=abc",
            title: "Video",
            observedAtUtc: new DateTimeOffset(2026, 4, 28, 1, 2, 3, TimeSpan.Zero));

        Assert.Equal("youtube.com", message.Domain);
    }
}
