using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.CurrentApps;

public sealed class CurrentAppStateUploadService
{
    private readonly MonitorDbContext _dbContext;

    public CurrentAppStateUploadService(MonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UploadBatchResult> UploadAsync(UploadCurrentAppStatesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Guid deviceId = Guid.Parse(request.DeviceId);
        var results = new List<UploadItemResult>();

        DeviceEntity? device = await _dbContext.Devices
            .SingleOrDefaultAsync(candidate => candidate.Id == deviceId);
        if (device is null)
        {
            return new UploadBatchResult(request.States
                .Select(item => new UploadItemResult(
                    item.ClientStateId,
                    UploadItemStatus.Error,
                    ErrorMessage: $"Device '{request.DeviceId}' is not registered."))
                .ToList());
        }

        CurrentAppStateEntity? persisted = await _dbContext.CurrentAppStates
            .SingleOrDefaultAsync(state => state.DeviceId == deviceId);

        foreach (CurrentAppStateUploadItem item in request.States)
        {
            if (item.Platform != device.Platform)
            {
                results.Add(new UploadItemResult(
                    item.ClientStateId,
                    UploadItemStatus.Error,
                    ErrorMessage: $"Current app state platform '{item.Platform}' does not match device platform '{device.Platform}'."));
                continue;
            }

            if (persisted is not null && persisted.ObservedAtUtc >= item.ObservedAtUtc)
            {
                results.Add(new UploadItemResult(item.ClientStateId, UploadItemStatus.Duplicate, ErrorMessage: null));
                continue;
            }

            if (persisted is null)
            {
                persisted = new CurrentAppStateEntity
                {
                    DeviceId = deviceId,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };
                _dbContext.CurrentAppStates.Add(persisted);
            }

            Apply(item, persisted);
            results.Add(new UploadItemResult(item.ClientStateId, UploadItemStatus.Accepted, ErrorMessage: null));
        }

        await _dbContext.SaveChangesAsync();

        return new UploadBatchResult(results);
    }

    private static void Apply(CurrentAppStateUploadItem item, CurrentAppStateEntity entity)
    {
        entity.ClientStateId = item.ClientStateId;
        entity.Platform = item.Platform;
        entity.PlatformAppKey = item.PlatformAppKey;
        entity.ObservedAtUtc = item.ObservedAtUtc;
        entity.LocalDate = item.LocalDate;
        entity.TimezoneId = item.TimezoneId;
        entity.Status = item.Status;
        entity.Source = item.Source;
        entity.ProcessId = item.ProcessId;
        entity.ProcessName = item.ProcessName;
        entity.ProcessPath = item.ProcessPath;
        entity.WindowHandle = item.WindowHandle;
        entity.WindowTitle = item.WindowTitle;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
