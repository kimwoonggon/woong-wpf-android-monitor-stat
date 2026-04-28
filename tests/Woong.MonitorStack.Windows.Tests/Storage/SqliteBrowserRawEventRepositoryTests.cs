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

    [Fact]
    public void DeleteOlderThan_RemovesOnlyExpiredRawEvents()
    {
        var repository = new SqliteBrowserRawEventRepository($"Data Source={_dbPath};Pooling=False");
        repository.Initialize();
        repository.Save(new BrowserRawEventRecord(
            "Chrome",
            WindowId: 1,
            TabId: 42,
            Url: null,
            Title: null,
            Domain: "old.example",
            ObservedAtUtc: new DateTimeOffset(2026, 3, 28, 0, 0, 0, TimeSpan.Zero)));
        repository.Save(new BrowserRawEventRecord(
            "Chrome",
            WindowId: 1,
            TabId: 42,
            Url: null,
            Title: null,
            Domain: "boundary.example",
            ObservedAtUtc: new DateTimeOffset(2026, 3, 29, 0, 0, 0, TimeSpan.Zero)));
        repository.Save(new BrowserRawEventRecord(
            "Chrome",
            WindowId: 1,
            TabId: 42,
            Url: null,
            Title: null,
            Domain: "new.example",
            ObservedAtUtc: new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)));

        int deleted = repository.DeleteOlderThan(new DateTimeOffset(2026, 3, 29, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(1, deleted);
        Assert.Equal(
            ["boundary.example", "new.example"],
            repository.QueryRecordsByTabId(42).Select(record => record.Domain ?? "").ToArray());
    }

    [Fact]
    public void RetentionService_UsesThirtyDayDefaultPolicy()
    {
        var repository = new SqliteBrowserRawEventRepository($"Data Source={_dbPath};Pooling=False");
        repository.Initialize();
        repository.Save(new BrowserRawEventRecord(
            "Chrome",
            WindowId: 1,
            TabId: 7,
            Url: null,
            Title: null,
            Domain: "expired.example",
            ObservedAtUtc: new DateTimeOffset(2026, 3, 28, 0, 0, 0, TimeSpan.Zero)));
        repository.Save(new BrowserRawEventRecord(
            "Chrome",
            WindowId: 1,
            TabId: 7,
            Url: null,
            Title: null,
            Domain: "retained.example",
            ObservedAtUtc: new DateTimeOffset(2026, 3, 29, 0, 0, 0, TimeSpan.Zero)));
        var service = new BrowserRawEventRetentionService(repository, BrowserRawEventRetentionPolicy.Default);

        int deleted = service.PruneExpired(new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(1, deleted);
        Assert.Equal(
            ["retained.example"],
            repository.QueryRecordsByTabId(7).Select(record => record.Domain ?? "").ToArray());
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
