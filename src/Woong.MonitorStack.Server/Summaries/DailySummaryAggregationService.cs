using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Summaries;

public sealed class DailySummaryAggregationService
{
    private readonly MonitorDbContext _dbContext;

    public DailySummaryAggregationService(MonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DailySummaryEntity> GenerateAsync(
        string userId,
        DateOnly summaryDate,
        string timezoneId,
        DateTimeOffset generatedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(timezoneId);

        var queryService = new DailySummaryQueryService(_dbContext);
        DailySummary summary = await queryService.GetAsync(userId, summaryDate, timezoneId);
        DailySummaryEntity? entity = await _dbContext.DailySummaries
            .SingleOrDefaultAsync(existing =>
                existing.UserId == userId &&
                existing.SummaryDate == summaryDate &&
                existing.TimezoneId == timezoneId);

        if (entity is null)
        {
            entity = new DailySummaryEntity
            {
                UserId = userId,
                SummaryDate = summaryDate,
                TimezoneId = timezoneId
            };
            _dbContext.DailySummaries.Add(entity);
        }

        entity.TotalActiveMs = summary.TotalActiveMs;
        entity.TotalIdleMs = summary.TotalIdleMs;
        entity.TotalWebMs = summary.TotalWebMs;
        entity.TopAppsJson = JsonSerializer.Serialize(summary.TopApps);
        entity.TopDomainsJson = JsonSerializer.Serialize(summary.TopDomains);
        entity.GeneratedAtUtc = generatedAtUtc;

        await _dbContext.SaveChangesAsync();

        return entity;
    }
}
