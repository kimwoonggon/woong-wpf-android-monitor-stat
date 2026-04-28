using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.Storage;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.Sync;

public sealed class WindowsSyncWorker
{
    private readonly ISyncOutboxRepository _outboxRepository;
    private readonly IWindowsSyncApiClient _apiClient;
    private readonly ISystemClock _clock;
    private readonly ISyncCheckpointStore _checkpointStore;

    public WindowsSyncWorker(
        ISyncOutboxRepository outboxRepository,
        IWindowsSyncApiClient apiClient,
        ISystemClock clock)
        : this(outboxRepository, apiClient, clock, new NoOpSyncCheckpointStore())
    {
    }

    public WindowsSyncWorker(
        ISyncOutboxRepository outboxRepository,
        IWindowsSyncApiClient apiClient,
        ISystemClock clock,
        ISyncCheckpointStore checkpointStore)
    {
        _outboxRepository = outboxRepository;
        _apiClient = apiClient;
        _clock = clock;
        _checkpointStore = checkpointStore;
    }

    public async Task<WindowsSyncResult> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        int syncedCount = 0;
        int failedCount = 0;

        foreach (SyncOutboxItem item in _outboxRepository.QueryAll().Where(ShouldAttemptSync).ToList())
        {
            UploadBatchResult result = await _apiClient.UploadAsync(item, cancellationToken);
            if (IsSuccessfulBatch(result))
            {
                DateTimeOffset syncedAtUtc = _clock.UtcNow;
                _outboxRepository.MarkSynced(item.Id, syncedAtUtc);
                _checkpointStore.Save(syncedAtUtc);
                syncedCount++;
            }
            else
            {
                _outboxRepository.MarkFailed(item.Id, GetFailureMessage(result));
                failedCount++;
            }
        }

        return new WindowsSyncResult(syncedCount, failedCount);
    }

    private static bool ShouldAttemptSync(SyncOutboxItem item)
        => item.Status is SyncOutboxStatus.Pending or SyncOutboxStatus.Failed;

    private static bool IsSuccessfulBatch(UploadBatchResult result)
        => result.Items.Count > 0 &&
            result.Items.All(item => item.Status is UploadItemStatus.Accepted or UploadItemStatus.Duplicate);

    private static string GetFailureMessage(UploadBatchResult result)
        => result.Items
            .Select(item => item.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
            ?? "Server rejected sync payload.";

    private sealed class NoOpSyncCheckpointStore : ISyncCheckpointStore
    {
        public void Save(DateTimeOffset syncedAtUtc)
        {
        }
    }
}
