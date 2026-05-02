using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Woong.MonitorStack.Server.Events;

namespace Woong.MonitorStack.Server.Tests.Events;

public sealed class RawEventRetentionBackgroundServiceTests
{
    [Fact]
    public void TestingHost_DoesNotRegisterRawEventRetentionHostedService()
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));

        IEnumerable<IHostedService> hostedServices =
            factory.Services.GetServices<IHostedService>();

        Assert.DoesNotContain(
            hostedServices,
            hostedService => hostedService is RawEventRetentionBackgroundService);
    }

    [Fact]
    public async Task RunOnceAsync_CreatesScopeAndInvokesMaintenanceWithoutSleeping()
    {
        var maintenance = new RecordingRawEventRetentionMaintenanceService();
        using ServiceProvider services = new ServiceCollection()
            .AddSingleton<IRawEventRetentionMaintenanceService>(maintenance)
            .BuildServiceProvider();
        var service = new RawEventRetentionBackgroundService(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RawEventRetentionOptions
            {
                Enabled = true,
                RetentionDays = 30,
                Interval = TimeSpan.FromHours(6)
            }),
            NullLogger<RawEventRetentionBackgroundService>.Instance);

        RawEventRetentionMaintenanceResult result = await service.RunOnceAsync(CancellationToken.None);

        Assert.Equal(1, maintenance.CallCount);
        Assert.Equal(7, result.DeletedCount);
        Assert.False(result.Skipped);
    }

    [Fact]
    public async Task RunOnceAsync_WhenDeleteCountReachesThreshold_SendsOperationalAlertWithoutPayloadContent()
    {
        var maintenance = new RecordingRawEventRetentionMaintenanceService(deletedCount: 12_000);
        var alerts = new RecordingRawEventRetentionAlertSink();
        using ServiceProvider services = new ServiceCollection()
            .AddSingleton<IRawEventRetentionMaintenanceService>(maintenance)
            .AddSingleton<IRawEventRetentionAlertSink>(alerts)
            .BuildServiceProvider();
        var service = new RawEventRetentionBackgroundService(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RawEventRetentionOptions
            {
                Enabled = true,
                RetentionDays = 30,
                Interval = TimeSpan.FromHours(6),
                FailureAlertEnabled = true,
                HighDeleteCountAlertThreshold = 10_000
            }),
            NullLogger<RawEventRetentionBackgroundService>.Instance);

        await service.RunOnceAsync(CancellationToken.None);

        RawEventRetentionAlert alert = Assert.Single(alerts.Alerts);
        Assert.Equal(RawEventRetentionAlertKind.HighDeleteCount, alert.Kind);
        Assert.Equal("Completed", alert.RunStatus);
        Assert.Equal(12_000, alert.DeletedCount);
        Assert.Equal(new DateTimeOffset(2026, 4, 2, 12, 0, 0, TimeSpan.Zero), alert.CutoffUtc);
        Assert.Null(alert.ExceptionType);
        Assert.Null(alert.ExceptionMessage);
        string renderedAlert = alert.ToString() ?? "";
        Assert.DoesNotContain("typed", renderedAlert, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", renderedAlert, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payload", renderedAlert, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunOnceAsync_WhenFailuresReachThreshold_SendsFailureAlertWithExceptionMetadataOnly()
    {
        var maintenance = new ThrowingRawEventRetentionMaintenanceService();
        var alerts = new RecordingRawEventRetentionAlertSink();
        using ServiceProvider services = new ServiceCollection()
            .AddSingleton<IRawEventRetentionMaintenanceService>(maintenance)
            .AddSingleton<IRawEventRetentionAlertSink>(alerts)
            .BuildServiceProvider();
        var service = new RawEventRetentionBackgroundService(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RawEventRetentionOptions
            {
                Enabled = true,
                RetentionDays = 30,
                Interval = TimeSpan.FromHours(6),
                FailureAlertEnabled = true,
                FailureAlertAfterConsecutiveFailures = 3,
                HighDeleteCountAlertThreshold = 10_000
            }),
            NullLogger<RawEventRetentionBackgroundService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunOnceAsync(CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunOnceAsync(CancellationToken.None));
        Assert.Empty(alerts.Alerts);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunOnceAsync(CancellationToken.None));

        RawEventRetentionAlert alert = Assert.Single(alerts.Alerts);
        Assert.Equal(RawEventRetentionAlertKind.ConsecutiveFailures, alert.Kind);
        Assert.Equal("Failed", alert.RunStatus);
        Assert.Null(alert.DeletedCount);
        Assert.Null(alert.CutoffUtc);
        Assert.Equal(nameof(InvalidOperationException), alert.ExceptionType);
        Assert.Equal("retention database timeout", alert.ExceptionMessage);
        string renderedAlert = alert.ToString() ?? "";
        Assert.DoesNotContain("typed", renderedAlert, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", renderedAlert, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payload", renderedAlert, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RecordingRawEventRetentionMaintenanceService : IRawEventRetentionMaintenanceService
    {
        private readonly int _deletedCount;

        public RecordingRawEventRetentionMaintenanceService(int deletedCount = 7)
        {
            _deletedCount = deletedCount;
        }

        public int CallCount { get; private set; }

        public Task<RawEventRetentionMaintenanceResult> RunOnceAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                RawEventRetentionMaintenanceResult.Completed(
                    deletedCount: _deletedCount,
                    cutoffUtc: new DateTimeOffset(2026, 4, 2, 12, 0, 0, TimeSpan.Zero)));
        }
    }

    private sealed class ThrowingRawEventRetentionMaintenanceService : IRawEventRetentionMaintenanceService
    {
        public Task<RawEventRetentionMaintenanceResult> RunOnceAsync(
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("retention database timeout");
    }

    private sealed class RecordingRawEventRetentionAlertSink : IRawEventRetentionAlertSink
    {
        public List<RawEventRetentionAlert> Alerts { get; } = [];

        public Task SendAsync(
            RawEventRetentionAlert alert,
            CancellationToken cancellationToken = default)
        {
            Alerts.Add(alert);

            return Task.CompletedTask;
        }
    }
}
