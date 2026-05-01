using Microsoft.Extensions.Options;
using Woong.MonitorStack.Server.Events;

namespace Woong.MonitorStack.Server.Tests.Events;

public sealed class RawEventRetentionMaintenanceServiceTests
{
    [Fact]
    public async Task RunOnceAsync_WhenDisabled_DoesNotCallRetentionService()
    {
        var retention = new RecordingRawEventRetentionService();
        var service = new RawEventRetentionMaintenanceService(
            Options.Create(new RawEventRetentionOptions
            {
                Enabled = false,
                RetentionDays = 30
            }),
            retention,
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 2, 12, 0, 0, TimeSpan.Zero)));

        RawEventRetentionMaintenanceResult result = await service.RunOnceAsync();

        Assert.True(result.Skipped);
        Assert.Equal(0, result.DeletedCount);
        Assert.Null(result.CutoffUtc);
        Assert.Equal(0, retention.CallCount);
    }

    [Fact]
    public async Task RunOnceAsync_WhenEnabled_DeletesRowsOlderThanConfiguredRetentionCutoff()
    {
        var retention = new RecordingRawEventRetentionService(deletedCount: 4);
        var service = new RawEventRetentionMaintenanceService(
            Options.Create(new RawEventRetentionOptions
            {
                Enabled = true,
                RetentionDays = 30
            }),
            retention,
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 2, 12, 0, 0, TimeSpan.Zero)));

        RawEventRetentionMaintenanceResult result = await service.RunOnceAsync();

        var expectedCutoff = new DateTimeOffset(2026, 4, 2, 12, 0, 0, TimeSpan.Zero);
        Assert.False(result.Skipped);
        Assert.Equal(4, result.DeletedCount);
        Assert.Equal(expectedCutoff, result.CutoffUtc);
        Assert.Equal(1, retention.CallCount);
        Assert.Equal(expectedCutoff, retention.LastCutoffUtc);
    }

    private sealed class RecordingRawEventRetentionService : IRawEventRetentionService
    {
        private readonly int _deletedCount;

        public RecordingRawEventRetentionService(int deletedCount = 0)
        {
            _deletedCount = deletedCount;
        }

        public int CallCount { get; private set; }

        public DateTimeOffset? LastCutoffUtc { get; private set; }

        public Task<int> DeleteOlderThanAsync(
            DateTimeOffset cutoffUtc,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastCutoffUtc = cutoffUtc;

            return Task.FromResult(_deletedCount);
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
