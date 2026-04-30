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

        bool deviceExists = await _dbContext.Devices.AnyAsync(device => device.Id == deviceId);
        if (!deviceExists)
        {
            return new UploadBatchResult(request.Sessions
                .Select(item => new UploadItemResult(
                    item.ClientSessionId,
                    UploadItemStatus.Error,
                    ErrorMessage: $"Device '{request.DeviceId}' is not registered."))
                .ToList());
        }

        List<string> requestedSessionIds = request.Sessions
            .Select(item => item.ClientSessionId)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        HashSet<string> seenSessionIds = (await _dbContext.FocusSessions
                .Where(session => session.DeviceId == deviceId &&
                    requestedSessionIds.Contains(session.ClientSessionId))
                .Select(session => session.ClientSessionId)
                .ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        foreach (FocusSessionUploadItem item in request.Sessions)
        {
            if (!seenSessionIds.Add(item.ClientSessionId))
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
                Source = item.Source,
                ProcessId = item.ProcessId,
                ProcessName = item.ProcessName,
                ProcessPath = item.ProcessPath,
                WindowHandle = item.WindowHandle,
                WindowTitle = item.WindowTitle
            });
            results.Add(new UploadItemResult(item.ClientSessionId, UploadItemStatus.Accepted, ErrorMessage: null));
        }

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
            HashSet<string> persistedSessionIds = (await _dbContext.FocusSessions
                    .Where(session => session.DeviceId == deviceId &&
                        requestedSessionIds.Contains(session.ClientSessionId))
                    .Select(session => session.ClientSessionId)
                    .ToListAsync())
                .ToHashSet(StringComparer.Ordinal);

            return new UploadBatchResult(request.Sessions
                .Select(item => persistedSessionIds.Contains(item.ClientSessionId)
                    ? new UploadItemResult(item.ClientSessionId, UploadItemStatus.Duplicate, ErrorMessage: null)
                    : new UploadItemResult(
                        item.ClientSessionId,
                        UploadItemStatus.Error,
                        ErrorMessage: $"Focus session '{item.ClientSessionId}' could not be persisted."))
                .ToList());
        }

        return new UploadBatchResult(results);
    }
}
