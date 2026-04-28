using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Tests.Data;

public sealed class ServerDbContextModelTests
{
    [Fact]
    public void DeviceEntity_HasUniqueIndexForUserPlatformAndDeviceKey()
    {
        var options = new DbContextOptionsBuilder<MonitorDbContext>()
            .UseNpgsql("Host=localhost;Database=woong_monitor_test;Username=test;Password=test")
            .Options;
        using var dbContext = new MonitorDbContext(options);

        var entityType = dbContext.Model.FindEntityType(typeof(DeviceEntity));
        var uniqueIndex = Assert.Single(entityType!.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(DeviceEntity.UserId), nameof(DeviceEntity.Platform), nameof(DeviceEntity.DeviceKey)]));

        Assert.True(uniqueIndex.IsUnique);
    }
}
