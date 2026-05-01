using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Woong.MonitorStack.Server.Events;

namespace Woong.MonitorStack.Server.Tests.Events;

public sealed class RawEventRetentionBackgroundServiceLoggingTests
{
    [Fact]
    public async Task RunOnceAsync_WhenMaintenanceSkips_LogsDisabledSkip()
    {
        var maintenance = new StubRawEventRetentionMaintenanceService(
            RawEventRetentionMaintenanceResult.Skip());
        var logger = new CapturingLogger<RawEventRetentionBackgroundService>();
        RawEventRetentionBackgroundService service = CreateService(maintenance, logger);

        RawEventRetentionMaintenanceResult result = await service.RunOnceAsync(CancellationToken.None);

        Assert.True(result.Skipped);
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("skipped", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RunOnceAsync_WhenMaintenanceDeletesRows_LogsStartAndDeletedCount()
    {
        var cutoff = new DateTimeOffset(2026, 4, 2, 12, 0, 0, TimeSpan.Zero);
        var maintenance = new StubRawEventRetentionMaintenanceService(
            RawEventRetentionMaintenanceResult.Completed(deletedCount: 5, cutoff));
        var logger = new CapturingLogger<RawEventRetentionBackgroundService>();
        RawEventRetentionBackgroundService service = CreateService(maintenance, logger);

        RawEventRetentionMaintenanceResult result = await service.RunOnceAsync(CancellationToken.None);

        Assert.False(result.Skipped);
        Assert.Equal(5, result.DeletedCount);
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("starting", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("5", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunOnceAsync_WhenMaintenanceFails_LogsFailureAndRethrows()
    {
        var maintenance = new ThrowingRawEventRetentionMaintenanceService();
        var logger = new CapturingLogger<RawEventRetentionBackgroundService>();
        RawEventRetentionBackgroundService service = CreateService(maintenance, logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RunOnceAsync(CancellationToken.None));

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Error &&
            entry.Exception is InvalidOperationException &&
            entry.Message.Contains("failed", StringComparison.OrdinalIgnoreCase));
    }

    private static RawEventRetentionBackgroundService CreateService(
        IRawEventRetentionMaintenanceService maintenance,
        CapturingLogger<RawEventRetentionBackgroundService> logger)
    {
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(maintenance)
            .BuildServiceProvider();

        return new RawEventRetentionBackgroundService(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RawEventRetentionOptions
            {
                Enabled = true,
                RetentionDays = 30,
                Interval = TimeSpan.FromHours(6)
            }),
            logger);
    }

    private sealed class StubRawEventRetentionMaintenanceService : IRawEventRetentionMaintenanceService
    {
        private readonly RawEventRetentionMaintenanceResult _result;

        public StubRawEventRetentionMaintenanceService(RawEventRetentionMaintenanceResult result)
        {
            _result = result;
        }

        public Task<RawEventRetentionMaintenanceResult> RunOnceAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class ThrowingRawEventRetentionMaintenanceService : IRawEventRetentionMaintenanceService
    {
        public Task<RawEventRetentionMaintenanceResult> RunOnceAsync(
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("retention failure");
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
}
