namespace Woong.MonitorStack.Server.Events;

public interface IRawEventRetentionAlertSink
{
    Task SendAsync(
        RawEventRetentionAlert alert,
        CancellationToken cancellationToken = default);
}

public sealed class LoggingRawEventRetentionAlertSink : IRawEventRetentionAlertSink
{
    private readonly ILogger<LoggingRawEventRetentionAlertSink> _logger;

    public LoggingRawEventRetentionAlertSink(ILogger<LoggingRawEventRetentionAlertSink> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(
        RawEventRetentionAlert alert,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Raw event retention alert {AlertKind}. Status {RunStatus}. Deleted {DeletedCount}. Cutoff {CutoffUtc}. Exception {ExceptionType}: {ExceptionMessage}.",
            alert.Kind,
            alert.RunStatus,
            alert.DeletedCount,
            alert.CutoffUtc,
            alert.ExceptionType,
            alert.ExceptionMessage);

        return Task.CompletedTask;
    }
}

public sealed record RawEventRetentionAlert(
    RawEventRetentionAlertKind Kind,
    string RunStatus,
    int? DeletedCount,
    DateTimeOffset? CutoffUtc,
    string? ExceptionType,
    string? ExceptionMessage);

public enum RawEventRetentionAlertKind
{
    HighDeleteCount,
    ConsecutiveFailures
}
