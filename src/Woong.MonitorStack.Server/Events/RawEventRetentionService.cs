using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Events;

public interface IRawEventRetentionService
{
    Task<int> DeleteOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken = default);
}

public sealed class RawEventRetentionService : IRawEventRetentionService
{
    private readonly MonitorDbContext _dbContext;

    public RawEventRetentionService(MonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> DeleteOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken = default)
    {
        List<RawEventEntity> rawEvents = await _dbContext.RawEvents.ToListAsync(cancellationToken);
        List<RawEventEntity> expiredRawEvents = rawEvents
            .Where(rawEvent => rawEvent.OccurredAtUtc < cutoffUtc)
            .ToList();

        if (expiredRawEvents.Count == 0)
        {
            return 0;
        }

        _dbContext.RawEvents.RemoveRange(expiredRawEvents);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return expiredRawEvents.Count;
    }
}
