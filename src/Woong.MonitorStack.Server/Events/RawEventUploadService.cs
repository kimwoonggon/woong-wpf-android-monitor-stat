using System.Text.Json;
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

        List<string> requestedEventIds = request.Events
            .Select(item => item.ClientEventId)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        HashSet<string> seenEventIds = (await _dbContext.RawEvents
                .Where(rawEvent => rawEvent.DeviceId == deviceId &&
                    requestedEventIds.Contains(rawEvent.ClientEventId))
                .Select(rawEvent => rawEvent.ClientEventId)
                .ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        foreach (RawEventUploadItem item in request.Events)
        {
            if (!seenEventIds.Add(item.ClientEventId))
            {
                results.Add(new UploadItemResult(item.ClientEventId, UploadItemStatus.Duplicate, ErrorMessage: null));
                continue;
            }

            if (ContainsForbiddenPayloadMetadata(item.PayloadJson))
            {
                results.Add(new UploadItemResult(
                    item.ClientEventId,
                    UploadItemStatus.Error,
                    ErrorMessage: "Raw event payload contains forbidden user input or content metadata."));
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

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
            HashSet<string> persistedEventIds = (await _dbContext.RawEvents
                    .Where(rawEvent => rawEvent.DeviceId == deviceId &&
                        requestedEventIds.Contains(rawEvent.ClientEventId))
                    .Select(rawEvent => rawEvent.ClientEventId)
                    .ToListAsync())
                .ToHashSet(StringComparer.Ordinal);

            return new UploadBatchResult(request.Events
                .Select(item => persistedEventIds.Contains(item.ClientEventId)
                    ? new UploadItemResult(item.ClientEventId, UploadItemStatus.Duplicate, ErrorMessage: null)
                    : new UploadItemResult(
                        item.ClientEventId,
                        UploadItemStatus.Error,
                        ErrorMessage: ContainsForbiddenPayloadMetadata(item.PayloadJson)
                            ? "Raw event payload contains forbidden user input or content metadata."
                            : $"Raw event '{item.ClientEventId}' could not be persisted."))
                .ToList());
        }

        return new UploadBatchResult(results);
    }

    private static bool ContainsForbiddenPayloadMetadata(string payloadJson)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(payloadJson);

            return ContainsForbiddenPayloadMetadata(document.RootElement);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool ContainsForbiddenPayloadMetadata(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (ForbiddenPayloadPropertyNames.Contains(property.Name) ||
                    ContainsForbiddenPayloadMetadata(property.Value))
                {
                    return true;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                if (ContainsForbiddenPayloadMetadata(item))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static readonly HashSet<string> ForbiddenPayloadPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "clipboard",
        "clipboardText",
        "formInput",
        "formText",
        "key",
        "keyText",
        "keys",
        "keystroke",
        "keystrokes",
        "message",
        "messageBody",
        "pageContent",
        "password",
        "screenContent",
        "screenshot",
        "textInput",
        "touchCoordinates",
        "typedText"
    };
}
