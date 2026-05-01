using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Server.Data;
using Woong.MonitorStack.Server.Events;
using Woong.MonitorStack.Server.Tests.Data;

namespace Woong.MonitorStack.Server.Tests.Events;

public sealed class RawEventRetentionServiceTests
{
    [Fact]
    public async Task DeleteOlderThanAsync_RemovesOnlyRawEventsOlderThanCutoffAndKeepsDurableSessionFacts()
    {
        await using RelationalTestDatabase database = await RelationalTestDatabase.CreateAsync();
        Guid deviceId = Guid.NewGuid();
        var cutoff = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        await SeedRetentionRowsAsync(database.Context, deviceId, cutoff);
        var service = new RawEventRetentionService(database.Context);

        int deletedCount = await service.DeleteOlderThanAsync(cutoff);

        Assert.Equal(1, deletedCount);
        List<RawEventEntity> rawEvents = await database.Context.RawEvents
            .OrderBy(rawEvent => rawEvent.ClientEventId)
            .ToListAsync();
        Assert.Equal(["raw-at-cutoff", "raw-new"], rawEvents.Select(rawEvent => rawEvent.ClientEventId));
        Assert.All(rawEvents, rawEvent => Assert.True(rawEvent.OccurredAtUtc >= cutoff));
        Assert.Equal(2, await database.Context.FocusSessions.CountAsync());
        Assert.Equal(1, await database.Context.WebSessions.CountAsync());
    }

    private static async Task SeedRetentionRowsAsync(
        MonitorDbContext dbContext,
        Guid deviceId,
        DateTimeOffset cutoff)
    {
        dbContext.Devices.Add(new DeviceEntity
        {
            Id = deviceId,
            UserId = "user-raw-retention",
            Platform = Platform.Windows,
            DeviceKey = "windows-raw-retention-key",
            DeviceName = "Windows Workstation",
            TimezoneId = "UTC",
            CreatedAtUtc = cutoff.AddDays(-40),
            LastSeenAtUtc = cutoff
        });
        dbContext.FocusSessions.AddRange(
            new FocusSessionEntity
            {
                DeviceId = deviceId,
                ClientSessionId = "focus-old",
                PlatformAppKey = "Code.exe",
                StartedAtUtc = cutoff.AddDays(-40),
                EndedAtUtc = cutoff.AddDays(-40).AddMinutes(5),
                DurationMs = 300_000,
                LocalDate = DateOnly.FromDateTime(cutoff.AddDays(-40).Date),
                TimezoneId = "UTC",
                IsIdle = false,
                Source = "foreground_window"
            },
            new FocusSessionEntity
            {
                DeviceId = deviceId,
                ClientSessionId = "focus-new",
                PlatformAppKey = "chrome.exe",
                StartedAtUtc = cutoff.AddHours(1),
                EndedAtUtc = cutoff.AddHours(1).AddMinutes(10),
                DurationMs = 600_000,
                LocalDate = DateOnly.FromDateTime(cutoff.Date),
                TimezoneId = "UTC",
                IsIdle = false,
                Source = "foreground_window"
            });
        dbContext.WebSessions.Add(new WebSessionEntity
        {
            DeviceId = deviceId,
            ClientSessionId = "web-old",
            FocusSessionId = "focus-old",
            BrowserFamily = "Chrome",
            Url = null,
            Domain = "example.com",
            PageTitle = null,
            StartedAtUtc = cutoff.AddDays(-40),
            EndedAtUtc = cutoff.AddDays(-40).AddMinutes(3),
            DurationMs = 180_000,
            CaptureMethod = "native_messaging",
            CaptureConfidence = "high",
            IsPrivateOrUnknown = false
        });
        dbContext.RawEvents.AddRange(
            new RawEventEntity
            {
                DeviceId = deviceId,
                ClientEventId = "raw-old",
                EventType = "foreground_window",
                OccurredAtUtc = cutoff.AddTicks(-1),
                PayloadJson = """{"processName":"Code.exe"}"""
            },
            new RawEventEntity
            {
                DeviceId = deviceId,
                ClientEventId = "raw-at-cutoff",
                EventType = "foreground_window",
                OccurredAtUtc = cutoff,
                PayloadJson = """{"processName":"Code.exe"}"""
            },
            new RawEventEntity
            {
                DeviceId = deviceId,
                ClientEventId = "raw-new",
                EventType = "foreground_window",
                OccurredAtUtc = cutoff.AddMinutes(1),
                PayloadJson = """{"processName":"chrome.exe"}"""
            });
        await dbContext.SaveChangesAsync();
    }
}
