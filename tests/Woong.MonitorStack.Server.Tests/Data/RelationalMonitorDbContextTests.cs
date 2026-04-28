using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Tests.Data;

public sealed class RelationalMonitorDbContextTests
{
    [Fact]
    public async Task DeviceUniqueIndex_IsEnforcedByRelationalProvider()
    {
        await using var database = await RelationalTestDatabase.CreateAsync();

        database.Context.Devices.Add(new DeviceEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Platform = Platform.Windows,
            DeviceKey = "device-key",
            DeviceName = "Workstation",
            TimezoneId = "Asia/Seoul",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastSeenAtUtc = DateTimeOffset.UtcNow
        });
        await database.Context.SaveChangesAsync();

        database.Context.Devices.Add(new DeviceEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Platform = Platform.Windows,
            DeviceKey = "device-key",
            DeviceName = "Workstation duplicate",
            TimezoneId = "Asia/Seoul",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastSeenAtUtc = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task ResetAsync_RecreatesEmptyRelationalSchema()
    {
        await using var database = await RelationalTestDatabase.CreateAsync();
        database.Context.Devices.Add(new DeviceEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Platform = Platform.Android,
            DeviceKey = "device-key",
            DeviceName = "Phone",
            TimezoneId = "Asia/Seoul",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastSeenAtUtc = DateTimeOffset.UtcNow
        });
        await database.Context.SaveChangesAsync();

        await database.ResetAsync();

        Assert.Empty(await database.Context.Devices.ToListAsync());
    }
}
