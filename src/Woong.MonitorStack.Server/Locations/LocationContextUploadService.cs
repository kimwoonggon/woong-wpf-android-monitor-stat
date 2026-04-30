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

        List<string> requestedContextIds = request.Contexts
            .Select(item => item.ClientContextId)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        HashSet<string> seenContextIds = (await _dbContext.LocationContexts
                .Where(context => context.DeviceId == deviceId &&
                    requestedContextIds.Contains(context.ClientContextId))
                .Select(context => context.ClientContextId)
                .ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        foreach (LocationContextUploadItem item in request.Contexts)
        {
            if (!seenContextIds.Add(item.ClientContextId))
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

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
            HashSet<string> persistedContextIds = (await _dbContext.LocationContexts
                    .Where(context => context.DeviceId == deviceId &&
                        requestedContextIds.Contains(context.ClientContextId))
                    .Select(context => context.ClientContextId)
                    .ToListAsync())
                .ToHashSet(StringComparer.Ordinal);

            return new UploadBatchResult(request.Contexts
                .Select(item => persistedContextIds.Contains(item.ClientContextId)
                    ? new UploadItemResult(item.ClientContextId, UploadItemStatus.Duplicate, ErrorMessage: null)
                    : new UploadItemResult(
                        item.ClientContextId,
                        UploadItemStatus.Error,
                        ErrorMessage: $"Location context '{item.ClientContextId}' could not be persisted."))
                .ToList());
        }

        return new UploadBatchResult(results);
    }
}
