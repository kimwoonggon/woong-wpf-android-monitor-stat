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

    [Fact]
    public void FocusSessionEntity_HasProcessWindowMetadataColumns()
    {
        var options = new DbContextOptionsBuilder<MonitorDbContext>()
            .UseNpgsql("Host=localhost;Database=woong_monitor_test;Username=test;Password=test")
            .Options;
        using var dbContext = new MonitorDbContext(options);

        var entityType = dbContext.Model.FindEntityType(typeof(FocusSessionEntity));

        Assert.NotNull(entityType!.FindProperty(nameof(FocusSessionEntity.ProcessId)));
        Assert.Equal(256, entityType.FindProperty(nameof(FocusSessionEntity.ProcessName))!.GetMaxLength());
        Assert.Equal(1024, entityType.FindProperty(nameof(FocusSessionEntity.ProcessPath))!.GetMaxLength());
        Assert.NotNull(entityType.FindProperty(nameof(FocusSessionEntity.WindowHandle)));
        Assert.Equal(512, entityType.FindProperty(nameof(FocusSessionEntity.WindowTitle))!.GetMaxLength());
    }

    [Fact]
    public void WebSessionEntity_HasNullableUrlAndCaptureMetadataColumns()
    {
        var options = new DbContextOptionsBuilder<MonitorDbContext>()
            .UseNpgsql("Host=localhost;Database=woong_monitor_test;Username=test;Password=test")
            .Options;
        using var dbContext = new MonitorDbContext(options);

        var entityType = dbContext.Model.FindEntityType(typeof(WebSessionEntity));

        Assert.True(entityType!.FindProperty(nameof(WebSessionEntity.Url))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(WebSessionEntity.PageTitle))!.IsNullable);
        Assert.Equal(64, entityType.FindProperty(nameof(WebSessionEntity.CaptureMethod))!.GetMaxLength());
        Assert.Equal(64, entityType.FindProperty(nameof(WebSessionEntity.CaptureConfidence))!.GetMaxLength());
        Assert.True(entityType.FindProperty(nameof(WebSessionEntity.IsPrivateOrUnknown))!.IsNullable);
    }
}
