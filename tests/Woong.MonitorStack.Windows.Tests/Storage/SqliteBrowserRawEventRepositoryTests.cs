using Microsoft.Data.Sqlite;
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

    [Fact]
    public void Initialize_WhenLegacyBrowserRawEventTableIsMissingClientEventId_BackfillsIdsAndEnforcesUniqueness()
    {
        CreateLegacyBrowserRawEventTableWithoutClientEventId();
        var repository = new SqliteBrowserRawEventRepository($"Data Source={_dbPath};Pooling=False");

        repository.Initialize();

        Assert.True(BrowserRawEventColumnIsRequired("client_event_id"));
        BrowserRawEventRecord saved = Assert.Single(repository.QueryRecordsByTabId(42));
        Assert.Equal("legacy-browser-raw-event:1", saved.ClientEventId);

        repository.Save(new BrowserRawEventRecord(
            "Chrome",
            WindowId: 1,
            TabId: 42,
            Url: null,
            Title: null,
            Domain: "duplicate.example",
            ObservedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 1, 0, TimeSpan.Zero),
            ClientEventId: "legacy-browser-raw-event:1"));

        BrowserRawEventRecord afterDuplicateSave = Assert.Single(repository.QueryRecordsByTabId(42));
        Assert.Equal("legacy-browser-raw-event:1", afterDuplicateSave.ClientEventId);
        Assert.Equal("legacy.example", afterDuplicateSave.Domain);
    }

    [Fact]
    public void Save_WhenClientEventIdAlreadyExists_DoesNotInsertDuplicate()
    {
        var repository = new SqliteBrowserRawEventRepository($"Data Source={_dbPath};Pooling=False");
        var first = CreateRawEventRecord(
            clientEventId: "chrome-event-duplicate",
            domain: "first.example",
            observedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));
        var duplicate = CreateRawEventRecord(
            clientEventId: "chrome-event-duplicate",
            domain: "second.example",
            observedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 1, 0, TimeSpan.Zero));

        repository.Initialize();
        repository.Save(first);
        repository.Save(duplicate);

        BrowserRawEventRecord saved = Assert.Single(repository.QueryRecordsByTabId(42));
        Assert.Equal("chrome-event-duplicate", saved.ClientEventId);
        Assert.Equal("first.example", saved.Domain);
    }

    [Fact]
    public async Task Save_WhenDuplicateClientEventIdsAreSavedConcurrently_DoesNotCreateExtraRows()
    {
        const string duplicateClientEventId = "chrome-event-concurrent-duplicate";
        string connectionString = $"Data Source={_dbPath};Pooling=False;Default Timeout=30";
        var repository = new SqliteBrowserRawEventRepository(connectionString);

        repository.Initialize();

        Task[] saves = Enumerable.Range(0, 12)
            .Select(index => Task.Run(() =>
            {
                var workerRepository = new SqliteBrowserRawEventRepository(connectionString);
                workerRepository.Save(CreateRawEventRecord(
                    clientEventId: duplicateClientEventId,
                    domain: $"worker-{index}.example",
                    observedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, index, TimeSpan.Zero)));
            }))
            .ToArray();

        await Task.WhenAll(saves);

        BrowserRawEventRecord saved = Assert.Single(repository.QueryRecordsByTabId(42));
        Assert.Equal(duplicateClientEventId, saved.ClientEventId);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private static BrowserRawEventRecord CreateRawEventRecord(
        string clientEventId,
        string domain,
        DateTimeOffset observedAtUtc)
        => new(
            "Chrome",
            WindowId: 1,
            TabId: 42,
            Url: null,
            Title: null,
            Domain: domain,
            ObservedAtUtc: observedAtUtc,
            ClientEventId: clientEventId);

    private void CreateLegacyBrowserRawEventTableWithoutClientEventId()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath};Pooling=False");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE browser_raw_event (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                browser_family TEXT NOT NULL,
                window_id INTEGER NOT NULL,
                tab_id INTEGER NOT NULL,
                url TEXT NULL,
                title TEXT NULL,
                domain TEXT NULL,
                observed_at_utc TEXT NOT NULL
            );

            INSERT INTO browser_raw_event (
                browser_family,
                window_id,
                tab_id,
                url,
                title,
                domain,
                observed_at_utc
            ) VALUES (
                'Chrome',
                1,
                42,
                NULL,
                NULL,
                'legacy.example',
                '2026-04-28T00:00:00.0000000+00:00'
            );
            """;
        _ = command.ExecuteNonQuery();
    }

    private bool BrowserRawEventColumnIsRequired(string columnName)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath};Pooling=False");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(browser_raw_event);";

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return reader.GetInt32(3) == 1;
            }
        }

        return false;
    }
}
