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

    [Fact]
    public void DeviceStateSessionEntity_HasDeviceClientSessionUniqueIndex()
    {
        var options = new DbContextOptionsBuilder<MonitorDbContext>()
            .UseNpgsql("Host=localhost;Database=woong_monitor_test;Username=test;Password=test")
            .Options;
        using var dbContext = new MonitorDbContext(options);

        var entityType = dbContext.Model.FindEntityType(typeof(DeviceStateSessionEntity));
        var uniqueIndex = Assert.Single(entityType!.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(DeviceStateSessionEntity.DeviceId), nameof(DeviceStateSessionEntity.ClientSessionId)]));

        Assert.Equal("device_state_sessions", entityType.GetTableName());
        Assert.True(uniqueIndex.IsUnique);
        Assert.Equal(64, entityType.FindProperty(nameof(DeviceStateSessionEntity.StateType))!.GetMaxLength());
        Assert.Equal(128, entityType.FindProperty(nameof(DeviceStateSessionEntity.TimezoneId))!.GetMaxLength());
    }

    [Fact]
    public void AppFamilyMappingEntity_HasFamilyAndMappingUniqueIndexes()
    {
        var options = new DbContextOptionsBuilder<MonitorDbContext>()
            .UseNpgsql("Host=localhost;Database=woong_monitor_test;Username=test;Password=test")
            .Options;
        using var dbContext = new MonitorDbContext(options);

        var familyEntityType = dbContext.Model.FindEntityType(typeof(AppFamilyEntity));
        var mappingEntityType = dbContext.Model.FindEntityType(typeof(AppFamilyMappingEntity));

        Assert.NotNull(familyEntityType);
        Assert.NotNull(mappingEntityType);
        Assert.Equal("app_families", familyEntityType!.GetTableName());
        Assert.Equal("app_family_mappings", mappingEntityType!.GetTableName());
        Assert.Contains(familyEntityType.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(AppFamilyEntity.Key)]));
        Assert.Contains(mappingEntityType.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(AppFamilyMappingEntity.MappingType), nameof(AppFamilyMappingEntity.MatchKey)]));
        Assert.Equal(64, mappingEntityType.FindProperty(nameof(AppFamilyMappingEntity.MappingType))!.GetMaxLength());
        Assert.Equal(512, mappingEntityType.FindProperty(nameof(AppFamilyMappingEntity.MatchKey))!.GetMaxLength());
    }
}
