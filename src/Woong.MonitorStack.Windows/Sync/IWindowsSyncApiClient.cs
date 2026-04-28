using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.Sync;

public interface IWindowsSyncApiClient
{
    Task<UploadBatchResult> UploadAsync(SyncOutboxItem item, CancellationToken cancellationToken = default);
}
