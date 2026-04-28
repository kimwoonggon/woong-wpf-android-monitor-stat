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

        foreach (WebSessionUploadItem item in request.Sessions)
        {
            bool exists = await _dbContext.WebSessions.AnyAsync(session =>
                session.DeviceId == deviceId &&
                session.FocusSessionId == item.FocusSessionId &&
                session.StartedAtUtc == item.StartedAtUtc &&
                session.EndedAtUtc == item.EndedAtUtc &&
                session.Url == item.Url);

            if (exists)
            {
                results.Add(new UploadItemResult(item.FocusSessionId, UploadItemStatus.Duplicate, ErrorMessage: null));
                continue;
            }

            _dbContext.WebSessions.Add(new WebSessionEntity
            {
                DeviceId = deviceId,
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
            results.Add(new UploadItemResult(item.FocusSessionId, UploadItemStatus.Accepted, ErrorMessage: null));
        }

        await _dbContext.SaveChangesAsync();

        return new UploadBatchResult(results);
    }
}
