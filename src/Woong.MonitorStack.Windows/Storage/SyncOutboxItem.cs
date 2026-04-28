namespace Woong.MonitorStack.Windows.Storage;

public sealed record SyncOutboxItem
{
    public SyncOutboxItem(
        string id,
        string aggregateType,
        string aggregateId,
        string payloadJson,
        SyncOutboxStatus status,
        int retryCount,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? syncedAtUtc,
        string? lastError)
    {
        Id = RequiredStorageText.Ensure(id, nameof(id));
        AggregateType = RequiredStorageText.Ensure(aggregateType, nameof(aggregateType));
        AggregateId = RequiredStorageText.Ensure(aggregateId, nameof(aggregateId));
        PayloadJson = RequiredStorageText.Ensure(payloadJson, nameof(payloadJson));
        Status = status;
        RetryCount = retryCount >= 0 ? retryCount : throw new ArgumentOutOfRangeException(nameof(retryCount));
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        SyncedAtUtc = syncedAtUtc?.ToUniversalTime();
        LastError = string.IsNullOrWhiteSpace(lastError) ? null : lastError;
    }

    public string Id { get; }

    public string AggregateType { get; }

    public string AggregateId { get; }

    public string PayloadJson { get; }

    public SyncOutboxStatus Status { get; }

    public int RetryCount { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? SyncedAtUtc { get; }

    public string? LastError { get; }

    public static SyncOutboxItem Pending(
        string id,
        string aggregateType,
        string aggregateId,
        string payloadJson,
        DateTimeOffset createdAtUtc)
        => new(
            id,
            aggregateType,
            aggregateId,
            payloadJson,
            SyncOutboxStatus.Pending,
            retryCount: 0,
            createdAtUtc,
            syncedAtUtc: null,
            lastError: null);
}
