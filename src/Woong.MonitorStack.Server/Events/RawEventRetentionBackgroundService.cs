using Microsoft.Extensions.Options;

namespace Woong.MonitorStack.Server.Events;

public sealed class RawEventRetentionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RawEventRetentionOptions _options;
    private readonly ILogger<RawEventRetentionBackgroundService> _logger;

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

            if (result.Skipped)
            {
                _logger.LogInformation("Raw event retention run skipped because retention is disabled.");
            }
            else
            {
                _logger.LogInformation(
                    "Raw event retention run completed. Deleted {DeletedCount} rows older than {CutoffUtc}.",
                    result.DeletedCount,
                    result.CutoffUtc);
            }

            return result;
        }
        catch (Exception exception)
        {
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
