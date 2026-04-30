using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Events;

public sealed class RawEventUploadService
{
    private readonly MonitorDbContext _dbContext;

    public RawEventUploadService(MonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UploadBatchResult> UploadAsync(UploadRawEventsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Guid deviceId = Guid.Parse(request.DeviceId);
        var results = new List<UploadItemResult>();

        bool deviceExists = await _dbContext.Devices.AnyAsync(device => device.Id == deviceId);
        if (!deviceExists)
        {
            return new UploadBatchResult(request.Events
                .Select(item => new UploadItemResult(
                    item.ClientEventId,
                    UploadItemStatus.Error,
                    ErrorMessage: $"Device '{request.DeviceId}' is not registered."))
                .ToList());
        }

        foreach (RawEventUploadItem item in request.Events)
        {
            bool exists = await _dbContext.RawEvents.AnyAsync(rawEvent =>
                rawEvent.DeviceId == deviceId &&
                rawEvent.ClientEventId == item.ClientEventId);

            if (exists)
            {
                results.Add(new UploadItemResult(item.ClientEventId, UploadItemStatus.Duplicate, ErrorMessage: null));
                continue;
            }

            _dbContext.RawEvents.Add(new RawEventEntity
            {
                DeviceId = deviceId,
                ClientEventId = item.ClientEventId,
                EventType = item.EventType,
                OccurredAtUtc = item.OccurredAtUtc,
                PayloadJson = item.PayloadJson
            });
            results.Add(new UploadItemResult(item.ClientEventId, UploadItemStatus.Accepted, ErrorMessage: null));
        }

        await _dbContext.SaveChangesAsync();

        return new UploadBatchResult(results);
    }
}
