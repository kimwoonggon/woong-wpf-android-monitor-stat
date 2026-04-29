using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Locations;

public sealed class LocationContextUploadService
{
    private readonly MonitorDbContext _dbContext;

    public LocationContextUploadService(MonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UploadBatchResult> UploadAsync(UploadLocationContextsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Guid deviceId = Guid.Parse(request.DeviceId);
        var results = new List<UploadItemResult>();

        bool deviceExists = await _dbContext.Devices.AnyAsync(device => device.Id == deviceId);
        if (!deviceExists)
        {
            return new UploadBatchResult(request.Contexts
                .Select(item => new UploadItemResult(
                    item.ClientContextId,
                    UploadItemStatus.Error,
                    ErrorMessage: $"Device '{request.DeviceId}' is not registered."))
                .ToList());
        }

        foreach (LocationContextUploadItem item in request.Contexts)
        {
            bool exists = await _dbContext.LocationContexts.AnyAsync(context =>
                context.DeviceId == deviceId &&
                context.ClientContextId == item.ClientContextId);

            if (exists)
            {
                results.Add(new UploadItemResult(item.ClientContextId, UploadItemStatus.Duplicate, ErrorMessage: null));
                continue;
            }

            _dbContext.LocationContexts.Add(new LocationContextEntity
            {
                DeviceId = deviceId,
                ClientContextId = item.ClientContextId,
                CapturedAtUtc = item.CapturedAtUtc,
                LocalDate = item.LocalDate,
                TimezoneId = item.TimezoneId,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                AccuracyMeters = item.AccuracyMeters,
                CaptureMode = item.CaptureMode,
                PermissionState = item.PermissionState,
                Source = item.Source,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            results.Add(new UploadItemResult(item.ClientContextId, UploadItemStatus.Accepted, ErrorMessage: null));
        }

        await _dbContext.SaveChangesAsync();

        return new UploadBatchResult(results);
    }
}
