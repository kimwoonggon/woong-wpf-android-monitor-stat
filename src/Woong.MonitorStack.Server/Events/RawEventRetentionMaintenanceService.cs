using Microsoft.Extensions.Options;

namespace Woong.MonitorStack.Server.Events;

public sealed class RawEventRetentionOptions
{
    public bool Enabled { get; set; }

    public int RetentionDays { get; set; } = 30;

    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(24);

    public bool FailureAlertEnabled { get; set; }

    public int FailureAlertAfterConsecutiveFailures { get; set; } = 3;

    public int HighDeleteCountAlertThreshold { get; set; } = 10_000;
}

public interface IRawEventRetentionMaintenanceService
{
    Task<RawEventRetentionMaintenanceResult> RunOnceAsync(CancellationToken cancellationToken = default);
}

public sealed class RawEventRetentionMaintenanceService : IRawEventRetentionMaintenanceService
{
    private readonly RawEventRetentionOptions _options;
    private readonly IRawEventRetentionService _retentionService;
    private readonly TimeProvider _timeProvider;

    public RawEventRetentionMaintenanceService(
        IOptions<RawEventRetentionOptions> options,
        IRawEventRetentionService retentionService,
        TimeProvider timeProvider)
    {
        _options = options.Value;
        _retentionService = retentionService;
        _timeProvider = timeProvider;
    }

    public async Task<RawEventRetentionMaintenanceResult> RunOnceAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return RawEventRetentionMaintenanceResult.Skip();
        }

        if (_options.RetentionDays <= 0)
        {
            throw new InvalidOperationException("Raw event retention days must be greater than zero.");
        }

        DateTimeOffset cutoffUtc = _timeProvider.GetUtcNow().AddDays(-_options.RetentionDays);
        int deletedCount = await _retentionService.DeleteOlderThanAsync(cutoffUtc, cancellationToken);

        return RawEventRetentionMaintenanceResult.Completed(deletedCount, cutoffUtc);
    }
}

public sealed record RawEventRetentionMaintenanceResult(
    bool Skipped,
    int DeletedCount,
    DateTimeOffset? CutoffUtc)
{
    public static RawEventRetentionMaintenanceResult Skip()
        => new(Skipped: true, DeletedCount: 0, CutoffUtc: null);

    public static RawEventRetentionMaintenanceResult Completed(int deletedCount, DateTimeOffset cutoffUtc)
        => new(Skipped: false, DeletedCount: deletedCount, CutoffUtc: cutoffUtc);
}
