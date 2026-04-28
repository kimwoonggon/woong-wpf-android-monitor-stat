namespace Woong.MonitorStack.Windows.Storage;

public interface ISyncOutboxRepository
{
    IReadOnlyList<SyncOutboxItem> QueryAll();

    void MarkSynced(string id, DateTimeOffset syncedAtUtc);

    void MarkFailed(string id, string error);
}
