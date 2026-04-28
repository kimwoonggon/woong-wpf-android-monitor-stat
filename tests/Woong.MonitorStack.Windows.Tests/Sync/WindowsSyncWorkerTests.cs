using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.Storage;
using Woong.MonitorStack.Windows.Sync;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.Tests.Sync;

public sealed class WindowsSyncWorkerTests
{
    [Fact]
    public async Task ProcessPendingAsync_WhenApiAcceptsItem_MarksOutboxItemSynced()
    {
        var syncedAtUtc = new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero);
        var item = SyncOutboxItem.Pending(
            id: "outbox-1",
            aggregateType: "focus_session",
            aggregateId: "session-1",
            payloadJson: "{\"clientSessionId\":\"session-1\"}",
            createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeSyncOutboxRepository([item]);
        var apiClient = new FakeSyncApiClient(new UploadBatchResult(
            [new UploadItemResult("session-1", UploadItemStatus.Accepted, ErrorMessage: null)]));
        var worker = new WindowsSyncWorker(repository, apiClient, new FakeClock(syncedAtUtc));

        WindowsSyncResult result = await worker.ProcessPendingAsync();

        Assert.Equal(1, result.SyncedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal([item], apiClient.UploadedItems);
        SyncOutboxItem saved = Assert.Single(repository.Items);
        Assert.Equal(SyncOutboxStatus.Synced, saved.Status);
        Assert.Equal(syncedAtUtc, saved.SyncedAtUtc);
        Assert.Null(saved.LastError);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenApiReturnsError_MarksOutboxItemFailedForRetry()
    {
        var item = SyncOutboxItem.Pending(
            id: "outbox-1",
            aggregateType: "focus_session",
            aggregateId: "session-1",
            payloadJson: "{\"clientSessionId\":\"session-1\"}",
            createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeSyncOutboxRepository([item]);
        var apiClient = new FakeSyncApiClient(new UploadBatchResult(
            [new UploadItemResult("session-1", UploadItemStatus.Error, "rate limited")]));
        var worker = new WindowsSyncWorker(repository, apiClient, new FakeClock(DateTimeOffset.UtcNow));

        WindowsSyncResult result = await worker.ProcessPendingAsync();

        Assert.Equal(0, result.SyncedCount);
        Assert.Equal(1, result.FailedCount);
        SyncOutboxItem saved = Assert.Single(repository.Items);
        Assert.Equal(SyncOutboxStatus.Failed, saved.Status);
        Assert.Equal(1, saved.RetryCount);
        Assert.Equal("rate limited", saved.LastError);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenApiReportsDuplicate_MarksOutboxItemSynced()
    {
        var failedItem = new SyncOutboxItem(
            id: "outbox-1",
            aggregateType: "focus_session",
            aggregateId: "session-1",
            payloadJson: "{\"clientSessionId\":\"session-1\"}",
            status: SyncOutboxStatus.Failed,
            retryCount: 2,
            createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            syncedAtUtc: null,
            lastError: "previous failure");
        var repository = new FakeSyncOutboxRepository([failedItem]);
        var apiClient = new FakeSyncApiClient(new UploadBatchResult(
            [new UploadItemResult("session-1", UploadItemStatus.Duplicate, ErrorMessage: null)]));
        var syncedAtUtc = new DateTimeOffset(2026, 4, 28, 2, 0, 0, TimeSpan.Zero);
        var worker = new WindowsSyncWorker(repository, apiClient, new FakeClock(syncedAtUtc));

        WindowsSyncResult result = await worker.ProcessPendingAsync();

        Assert.Equal(1, result.SyncedCount);
        Assert.Equal(0, result.FailedCount);
        SyncOutboxItem saved = Assert.Single(repository.Items);
        Assert.Equal(SyncOutboxStatus.Synced, saved.Status);
        Assert.Equal(2, saved.RetryCount);
        Assert.Equal(syncedAtUtc, saved.SyncedAtUtc);
        Assert.Null(saved.LastError);
    }

    private sealed class FakeSyncApiClient : IWindowsSyncApiClient
    {
        private readonly UploadBatchResult _result;

        public FakeSyncApiClient(UploadBatchResult result)
        {
            _result = result;
        }

        public List<SyncOutboxItem> UploadedItems { get; } = [];

        public Task<UploadBatchResult> UploadAsync(SyncOutboxItem item, CancellationToken cancellationToken = default)
        {
            UploadedItems.Add(item);

            return Task.FromResult(_result);
        }
    }

    private sealed class FakeSyncOutboxRepository : ISyncOutboxRepository
    {
        public FakeSyncOutboxRepository(IEnumerable<SyncOutboxItem> items)
        {
            Items = items.ToList();
        }

        public List<SyncOutboxItem> Items { get; }

        public IReadOnlyList<SyncOutboxItem> QueryAll()
            => Items;

        public void MarkSynced(string id, DateTimeOffset syncedAtUtc)
        {
            Replace(id, item => new SyncOutboxItem(
                item.Id,
                item.AggregateType,
                item.AggregateId,
                item.PayloadJson,
                SyncOutboxStatus.Synced,
                item.RetryCount,
                item.CreatedAtUtc,
                syncedAtUtc,
                lastError: null));
        }

        public void MarkFailed(string id, string error)
        {
            Replace(id, item => new SyncOutboxItem(
                item.Id,
                item.AggregateType,
                item.AggregateId,
                item.PayloadJson,
                SyncOutboxStatus.Failed,
                item.RetryCount + 1,
                item.CreatedAtUtc,
                item.SyncedAtUtc,
                error));
        }

        private void Replace(string id, Func<SyncOutboxItem, SyncOutboxItem> replace)
        {
            int index = Items.FindIndex(item => item.Id == id);
            Items[index] = replace(Items[index]);
        }
    }

    private sealed class FakeClock : ISystemClock
    {
        public FakeClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}
