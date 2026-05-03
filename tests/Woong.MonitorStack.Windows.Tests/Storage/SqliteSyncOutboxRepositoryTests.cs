using Microsoft.Data.Sqlite;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.Tests.Storage;

public sealed class SqliteSyncOutboxRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    [Fact]
    public void MarkSynced_MovesPendingItemToSynced()
    {
        var repository = new SqliteSyncOutboxRepository($"Data Source={_dbPath};Pooling=False");
        var item = SyncOutboxItem.Pending(
            id: "outbox-1",
            aggregateType: "focus_session",
            aggregateId: "session-1",
            payloadJson: "{\"clientSessionId\":\"session-1\"}",
            createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));

        repository.Initialize();
        repository.Add(item);
        repository.MarkSynced("outbox-1", new DateTimeOffset(2026, 4, 28, 0, 1, 0, TimeSpan.Zero));

        var saved = Assert.Single(repository.QueryAll());
        Assert.Equal(SyncOutboxStatus.Synced, saved.Status);
        Assert.NotNull(saved.SyncedAtUtc);
    }

    [Fact]
    public void MarkFailed_IncrementsRetryCountAndStoresError()
    {
        var repository = new SqliteSyncOutboxRepository($"Data Source={_dbPath};Pooling=False");
        var item = SyncOutboxItem.Pending(
            id: "outbox-1",
            aggregateType: "focus_session",
            aggregateId: "session-1",
            payloadJson: "{\"clientSessionId\":\"session-1\"}",
            createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));

        repository.Initialize();
        repository.Add(item);
        repository.MarkFailed("outbox-1", "server unavailable");

        var saved = Assert.Single(repository.QueryAll());
        Assert.Equal(SyncOutboxStatus.Failed, saved.Status);
        Assert.Equal(1, saved.RetryCount);
        Assert.Equal("server unavailable", saved.LastError);
    }

    [Fact]
    public void Add_WhenAggregateIdentityAlreadyQueued_IgnoresDifferentOutboxId()
    {
        var repository = new SqliteSyncOutboxRepository($"Data Source={_dbPath};Pooling=False");
        SyncOutboxItem first = CreatePendingItem(
            id: "outbox-1",
            aggregateType: "focus_session",
            aggregateId: "session-1",
            createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));
        SyncOutboxItem duplicateAggregate = CreatePendingItem(
            id: "outbox-2",
            aggregateType: "focus_session",
            aggregateId: "session-1",
            createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 1, 0, TimeSpan.Zero));

        repository.Initialize();
        repository.Add(first);
        repository.Add(duplicateAggregate);

        SyncOutboxItem saved = Assert.Single(repository.QueryAll());
        Assert.Equal("outbox-1", saved.Id);
        Assert.Equal(SyncOutboxStatus.Pending, saved.Status);
    }

    [Fact]
    public void MarkFailed_WhenItemIsAlreadySynced_DoesNotReopenOrIncrementRetryCount()
    {
        var repository = new SqliteSyncOutboxRepository($"Data Source={_dbPath};Pooling=False");
        SyncOutboxItem item = CreatePendingItem(
            id: "outbox-1",
            aggregateType: "focus_session",
            aggregateId: "session-1",
            createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));
        var syncedAtUtc = new DateTimeOffset(2026, 4, 28, 0, 1, 0, TimeSpan.Zero);

        repository.Initialize();
        repository.Add(item);
        repository.MarkSynced("outbox-1", syncedAtUtc);
        repository.MarkFailed("outbox-1", "late failure");

        SyncOutboxItem saved = Assert.Single(repository.QueryAll());
        Assert.Equal(SyncOutboxStatus.Synced, saved.Status);
        Assert.Equal(0, saved.RetryCount);
        Assert.Equal(syncedAtUtc, saved.SyncedAtUtc);
        Assert.Null(saved.LastError);
    }

    [Fact]
    public void MarkSynced_WhenItemIsAlreadySynced_DoesNotChangeTerminalMetadata()
    {
        var repository = new SqliteSyncOutboxRepository($"Data Source={_dbPath};Pooling=False");
        SyncOutboxItem item = CreatePendingItem(
            id: "outbox-1",
            aggregateType: "focus_session",
            aggregateId: "session-1",
            createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));
        var firstSyncedAtUtc = new DateTimeOffset(2026, 4, 28, 0, 1, 0, TimeSpan.Zero);

        repository.Initialize();
        repository.Add(item);
        repository.MarkSynced("outbox-1", firstSyncedAtUtc);
        repository.MarkSynced("outbox-1", firstSyncedAtUtc.AddMinutes(5));

        SyncOutboxItem saved = Assert.Single(repository.QueryAll());
        Assert.Equal(SyncOutboxStatus.Synced, saved.Status);
        Assert.Equal(firstSyncedAtUtc, saved.SyncedAtUtc);
        Assert.Equal(0, saved.RetryCount);
        Assert.Null(saved.LastError);
    }

    [Fact]
    public void Initialize_WhenLegacyDuplicateAggregateIdentityRowsExist_PreservesOneRowAndDedupes()
    {
        CreateLegacyOutboxTableWithDuplicateAggregateRows();
        var repository = new SqliteSyncOutboxRepository($"Data Source={_dbPath};Pooling=False");

        repository.Initialize();

        SyncOutboxItem saved = Assert.Single(repository.QueryAll());
        Assert.Equal("outbox-1", saved.Id);
        Assert.Equal("focus_session", saved.AggregateType);
        Assert.Equal("session-1", saved.AggregateId);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private static SyncOutboxItem CreatePendingItem(
        string id,
        string aggregateType,
        string aggregateId,
        DateTimeOffset createdAtUtc)
        => SyncOutboxItem.Pending(
            id: id,
            aggregateType: aggregateType,
            aggregateId: aggregateId,
            payloadJson: $$"""{"id":"{{aggregateId}}"}""",
            createdAtUtc: createdAtUtc);

    private void CreateLegacyOutboxTableWithDuplicateAggregateRows()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath};Pooling=False");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE sync_outbox (
                id TEXT NOT NULL PRIMARY KEY,
                aggregate_type TEXT NOT NULL,
                aggregate_id TEXT NOT NULL,
                payload_json TEXT NOT NULL,
                status INTEGER NOT NULL,
                retry_count INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL,
                synced_at_utc TEXT NULL,
                last_error TEXT NULL
            );

            INSERT INTO sync_outbox (
                id,
                aggregate_type,
                aggregate_id,
                payload_json,
                status,
                retry_count,
                created_at_utc,
                synced_at_utc,
                last_error
            ) VALUES (
                'outbox-1',
                'focus_session',
                'session-1',
                '{"id":"session-1"}',
                1,
                0,
                '2026-04-28T00:00:00.0000000+00:00',
                NULL,
                NULL
            ), (
                'outbox-2',
                'focus_session',
                'session-1',
                '{"id":"session-1"}',
                1,
                0,
                '2026-04-28T00:01:00.0000000+00:00',
                NULL,
                NULL
            );
            """;
        _ = command.ExecuteNonQuery();
    }
}
