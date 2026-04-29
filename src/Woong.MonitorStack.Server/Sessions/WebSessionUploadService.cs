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
                session.ClientSessionId == item.ClientSessionId);

            if (exists)
            {
                results.Add(new UploadItemResult(item.ClientSessionId, UploadItemStatus.Duplicate, ErrorMessage: null));
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

        await _dbContext.SaveChangesAsync();

        return new UploadBatchResult(results);
    }
}
