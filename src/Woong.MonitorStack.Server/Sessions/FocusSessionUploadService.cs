using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Sessions;

public sealed class FocusSessionUploadService
{
    private readonly MonitorDbContext _dbContext;

    public FocusSessionUploadService(MonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UploadBatchResult> UploadAsync(UploadFocusSessionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Guid deviceId = Guid.Parse(request.DeviceId);
        var results = new List<UploadItemResult>();

        foreach (FocusSessionUploadItem item in request.Sessions)
        {
            bool exists = await _dbContext.FocusSessions.AnyAsync(session =>
                session.DeviceId == deviceId &&
                session.ClientSessionId == item.ClientSessionId);

            if (exists)
            {
                results.Add(new UploadItemResult(item.ClientSessionId, UploadItemStatus.Duplicate, ErrorMessage: null));
                continue;
            }

            _dbContext.FocusSessions.Add(new FocusSessionEntity
            {
                DeviceId = deviceId,
                ClientSessionId = item.ClientSessionId,
                PlatformAppKey = item.PlatformAppKey,
                StartedAtUtc = item.StartedAtUtc,
                EndedAtUtc = item.EndedAtUtc,
                DurationMs = item.DurationMs,
                LocalDate = item.LocalDate,
                TimezoneId = item.TimezoneId,
                IsIdle = item.IsIdle,
                Source = item.Source
            });
            results.Add(new UploadItemResult(item.ClientSessionId, UploadItemStatus.Accepted, ErrorMessage: null));
        }

        await _dbContext.SaveChangesAsync();

        return new UploadBatchResult(results);
    }
}
