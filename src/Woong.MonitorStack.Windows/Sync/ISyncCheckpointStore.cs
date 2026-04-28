namespace Woong.MonitorStack.Windows.Sync;

public interface ISyncCheckpointStore
{
    void Save(DateTimeOffset syncedAtUtc);
}
