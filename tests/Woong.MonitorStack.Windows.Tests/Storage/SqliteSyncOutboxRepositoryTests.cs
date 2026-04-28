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

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
