using Woong.MonitorStack.Windows.Browser;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.Tests.Storage;

public sealed class SqliteBrowserRawEventRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    [Fact]
    public void SaveAndQueryByTabId_RoundTripsBrowserRawEvent()
    {
        var repository = new SqliteBrowserRawEventRepository($"Data Source={_dbPath};Pooling=False");
        var message = ChromeTabChangedMessage.FromExtensionPayload(
            windowId: 7,
            tabId: 42,
            url: "https://www.youtube.com/watch?v=abc",
            title: "Video",
            observedAtUtc: new DateTimeOffset(2026, 4, 28, 1, 2, 3, TimeSpan.Zero));

        repository.Initialize();
        repository.Save(message);

        var saved = Assert.Single(repository.QueryByTabId(42));
        Assert.Equal("Chrome", saved.BrowserFamily);
        Assert.Equal("youtube.com", saved.Domain);
        Assert.Equal("Video", saved.Title);
        Assert.Equal(message.ObservedAtUtc, saved.ObservedAtUtc);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
