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

    private sealed class RecordingRawEventRetentionMaintenanceService : IRawEventRetentionMaintenanceService
    {
        public int CallCount { get; private set; }

        public Task<RawEventRetentionMaintenanceResult> RunOnceAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                RawEventRetentionMaintenanceResult.Completed(
                    deletedCount: 7,
                    cutoffUtc: new DateTimeOffset(2026, 4, 2, 12, 0, 0, TimeSpan.Zero)));
        }
    }
}
