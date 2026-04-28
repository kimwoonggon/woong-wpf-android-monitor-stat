using Woong.MonitorStack.Windows.Browser;

namespace Woong.MonitorStack.Windows.Tests.Browser;

public sealed class BrowserWebSessionizerTests
{
    [Fact]
    public void Apply_WhenActiveTabChanges_CreatesWebSessionForPreviousTab()
    {
        var sessionizer = new BrowserWebSessionizer(focusSessionId: "focus-1");
        var first = ChromeTabChangedMessage.FromExtensionPayload(
            windowId: 7,
            tabId: 42,
            url: "https://www.youtube.com/watch?v=abc",
            title: "Video",
            observedAtUtc: new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero));
        var second = ChromeTabChangedMessage.FromExtensionPayload(
            windowId: 7,
            tabId: 43,
            url: "https://docs.microsoft.com/dotnet",
            title: ".NET docs",
            observedAtUtc: new DateTimeOffset(2026, 4, 28, 1, 5, 0, TimeSpan.Zero));

        Assert.Empty(sessionizer.Apply(first));
        var completed = Assert.Single(sessionizer.Apply(second));

        Assert.Equal("focus-1", completed.FocusSessionId);
        Assert.Equal("Chrome", completed.BrowserFamily);
        Assert.Equal("youtube.com", completed.Domain);
        Assert.Equal(300_000, completed.DurationMs);
    }

    [Fact]
    public void Apply_WhenDuplicateTabEventArrives_DoesNotInflateDuration()
    {
        var sessionizer = new BrowserWebSessionizer(focusSessionId: "focus-1");
        var first = ChromeTabChangedMessage.FromExtensionPayload(
            windowId: 7,
            tabId: 42,
            url: "https://www.youtube.com/watch?v=abc",
            title: "Video",
            observedAtUtc: new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero));
        var duplicate = ChromeTabChangedMessage.FromExtensionPayload(
            windowId: 7,
            tabId: 42,
            url: "https://www.youtube.com/watch?v=abc",
            title: "Video",
            observedAtUtc: new DateTimeOffset(2026, 4, 28, 1, 1, 0, TimeSpan.Zero));
        var next = ChromeTabChangedMessage.FromExtensionPayload(
            windowId: 7,
            tabId: 43,
            url: "https://docs.microsoft.com/dotnet",
            title: ".NET docs",
            observedAtUtc: new DateTimeOffset(2026, 4, 28, 1, 5, 0, TimeSpan.Zero));

        _ = sessionizer.Apply(first);
        Assert.Empty(sessionizer.Apply(duplicate));
        var completed = Assert.Single(sessionizer.Apply(next));

        Assert.Equal("youtube.com", completed.Domain);
        Assert.Equal(300_000, completed.DurationMs);
    }
}
