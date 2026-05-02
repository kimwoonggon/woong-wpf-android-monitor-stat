using Microsoft.Extensions.Options;

namespace Woong.MonitorStack.Server.Events;

public sealed class RawEventRetentionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RawEventRetentionOptions _options;
    private readonly ILogger<RawEventRetentionBackgroundService> _logger;
    private int _consecutiveFailureCount;

    public RawEventRetentionBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<RawEventRetentionOptions> options,
        ILogger<RawEventRetentionBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RawEventRetentionMaintenanceResult> RunOnceAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Raw event retention run starting.");

            using IServiceScope scope = _scopeFactory.CreateScope();
            var maintenance = scope.ServiceProvider.GetRequiredService<IRawEventRetentionMaintenanceService>();
            RawEventRetentionMaintenanceResult result = await maintenance.RunOnceAsync(cancellationToken);
            var alertSink = scope.ServiceProvider.GetService<IRawEventRetentionAlertSink>();

            if (result.Skipped)
            {
                _logger.LogInformation("Raw event retention run skipped because retention is disabled.");
                _consecutiveFailureCount = 0;
            }
            else
            {
                _logger.LogInformation(
                    "Raw event retention run completed. Deleted {DeletedCount} rows older than {CutoffUtc}.",
                    result.DeletedCount,
                    result.CutoffUtc);
                _consecutiveFailureCount = 0;
                if (_options.FailureAlertEnabled &&
                    _options.HighDeleteCountAlertThreshold > 0 &&
                    result.DeletedCount >= _options.HighDeleteCountAlertThreshold &&
                    alertSink is not null)
                {
                    await alertSink.SendAsync(
                        new RawEventRetentionAlert(
                            RawEventRetentionAlertKind.HighDeleteCount,
                            "Completed",
                            result.DeletedCount,
                            result.CutoffUtc,
                            ExceptionType: null,
                            ExceptionMessage: null),
                        cancellationToken);
                }
            }

            return result;
        }
        catch (Exception exception)
        {
            _consecutiveFailureCount++;
            if (_options.FailureAlertEnabled &&
                _options.FailureAlertAfterConsecutiveFailures > 0 &&
                _consecutiveFailureCount == _options.FailureAlertAfterConsecutiveFailures)
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                var alertSink = scope.ServiceProvider.GetService<IRawEventRetentionAlertSink>();
                if (alertSink is not null)
                {
                    await alertSink.SendAsync(
                        new RawEventRetentionAlert(
                            RawEventRetentionAlertKind.ConsecutiveFailures,
                            "Failed",
                            DeletedCount: null,
                            CutoffUtc: null,
                            exception.GetType().Name,
                            exception.Message),
                        cancellationToken);
                }
            }
            _logger.LogError(exception, "Raw event retention run failed.");
            throw;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (_options.Interval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Raw event retention interval must be greater than zero.");
        }

        await RunOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(_options.Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }
}
