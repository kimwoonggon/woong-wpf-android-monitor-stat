using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Sessions;

public sealed class WebSessionUploadService
{
    private readonly MonitorDbContext _dbContext;

    public WebSessionUploadService(MonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UploadBatchResult> UploadAsync(UploadWebSessionsRequest request)
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
        HashSet<string> seenSessionIds = (await _dbContext.WebSessions
                .Where(session => session.DeviceId == deviceId &&
                    requestedSessionIds.Contains(session.ClientSessionId))
                .Select(session => session.ClientSessionId)
                .ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        foreach (WebSessionUploadItem item in request.Sessions)
        {
            if (!seenSessionIds.Add(item.ClientSessionId))
            {
                results.Add(new UploadItemResult(item.ClientSessionId, UploadItemStatus.Duplicate, ErrorMessage: null));
                continue;
            }

            bool focusSessionExists = await _dbContext.FocusSessions.AnyAsync(session =>
                session.DeviceId == deviceId &&
                session.ClientSessionId == item.FocusSessionId);

            if (!focusSessionExists)
            {
                results.Add(new UploadItemResult(
                    item.ClientSessionId,
                    UploadItemStatus.Error,
                    ErrorMessage: $"Focus session '{item.FocusSessionId}' is not registered for device '{request.DeviceId}'."));
                continue;
            }

            _dbContext.WebSessions.Add(new WebSessionEntity
            {
                DeviceId = deviceId,
                ClientSessionId = item.ClientSessionId,
                FocusSessionId = item.FocusSessionId,
                BrowserFamily = item.BrowserFamily,
                Url = item.Url,
                Domain = item.Domain,
                PageTitle = item.PageTitle,
                StartedAtUtc = item.StartedAtUtc,
                EndedAtUtc = item.EndedAtUtc,
                DurationMs = item.DurationMs,
                CaptureMethod = item.CaptureMethod,
                CaptureConfidence = item.CaptureConfidence,
                IsPrivateOrUnknown = item.IsPrivateOrUnknown
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
            HashSet<string> persistedSessionIds = (await _dbContext.WebSessions
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
                        ErrorMessage: $"Web session '{item.ClientSessionId}' could not be persisted."))
                .ToList());
        }

        return new UploadBatchResult(results);
    }
}
