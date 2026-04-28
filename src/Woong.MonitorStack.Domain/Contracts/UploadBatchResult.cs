namespace Woong.MonitorStack.Domain.Contracts;

public sealed record UploadBatchResult(IReadOnlyList<UploadItemResult> Items);

public sealed record UploadItemResult(string ClientId, UploadItemStatus Status, string? ErrorMessage);

public enum UploadItemStatus
{
    Accepted = 1,
    Duplicate = 2,
    Error = 3
}
