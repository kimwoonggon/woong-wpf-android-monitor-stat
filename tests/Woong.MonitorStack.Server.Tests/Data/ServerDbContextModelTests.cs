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
    public void DeviceEntity_HasDeviceTokenVerifierColumns()
    {
        using var dbContext = CreateModelContext();

        var entityType = dbContext.Model.FindEntityType(typeof(DeviceEntity));

        Assert.Equal(128, entityType!.FindProperty(nameof(DeviceEntity.DeviceTokenSalt))!.GetMaxLength());
        Assert.False(entityType.FindProperty(nameof(DeviceEntity.DeviceTokenSalt))!.IsNullable);
        Assert.Equal(128, entityType.FindProperty(nameof(DeviceEntity.DeviceTokenHash))!.GetMaxLength());
        Assert.False(entityType.FindProperty(nameof(DeviceEntity.DeviceTokenHash))!.IsNullable);
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
    public void WebSessionEntity_HasDeviceClientSessionUniqueIndex()
    {
        var options = new DbContextOptionsBuilder<MonitorDbContext>()
            .UseNpgsql("Host=localhost;Database=woong_monitor_test;Username=test;Password=test")
            .Options;
        using var dbContext = new MonitorDbContext(options);

        var entityType = dbContext.Model.FindEntityType(typeof(WebSessionEntity));
        var uniqueIndex = Assert.Single(entityType!.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(WebSessionEntity.DeviceId), nameof(WebSessionEntity.ClientSessionId)]));

        Assert.Equal("web_sessions", entityType.GetTableName());
        Assert.True(uniqueIndex.IsUnique);
        Assert.Equal(128, entityType.FindProperty(nameof(WebSessionEntity.ClientSessionId))!.GetMaxLength());
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

    [Fact]
    public void LocationContextEntity_HasNullableCoordinatesAndDeviceClientContextUniqueIndex()
    {
        using var dbContext = CreateModelContext();

        var entityType = dbContext.Model.FindEntityType(typeof(LocationContextEntity));
        var uniqueIndex = Assert.Single(entityType!.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(LocationContextEntity.DeviceId), nameof(LocationContextEntity.ClientContextId)]));

        Assert.Equal("location_contexts", entityType.GetTableName());
        Assert.True(uniqueIndex.IsUnique);
        Assert.Equal(128, entityType.FindProperty(nameof(LocationContextEntity.ClientContextId))!.GetMaxLength());
        Assert.True(entityType.FindProperty(nameof(LocationContextEntity.Latitude))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(LocationContextEntity.Longitude))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(LocationContextEntity.AccuracyMeters))!.IsNullable);
        Assert.Equal(64, entityType.FindProperty(nameof(LocationContextEntity.CaptureMode))!.GetMaxLength());
        Assert.Equal(64, entityType.FindProperty(nameof(LocationContextEntity.PermissionState))!.GetMaxLength());
        Assert.Equal(128, entityType.FindProperty(nameof(LocationContextEntity.Source))!.GetMaxLength());
    }

    [Fact]
    public void ServerSessionEntities_HaveRequiredDeviceForeignKeys()
    {
        using var dbContext = CreateModelContext();

        AssertForeignKey<FocusSessionEntity, DeviceEntity>(
            dbContext,
            nameof(FocusSessionEntity.DeviceId));
        AssertForeignKey<WebSessionEntity, DeviceEntity>(
            dbContext,
            nameof(WebSessionEntity.DeviceId));
        AssertForeignKey<RawEventEntity, DeviceEntity>(
            dbContext,
            nameof(RawEventEntity.DeviceId));
        AssertForeignKey<DeviceStateSessionEntity, DeviceEntity>(
            dbContext,
            nameof(DeviceStateSessionEntity.DeviceId));
        AssertForeignKey<LocationContextEntity, DeviceEntity>(
            dbContext,
            nameof(LocationContextEntity.DeviceId));
    }

    [Fact]
    public void WebSessionEntity_HasCompositeForeignKeyToFocusSessionClientSession()
    {
        using var dbContext = CreateModelContext();

        var entityType = dbContext.Model.FindEntityType(typeof(WebSessionEntity));
        var foreignKey = Assert.Single(entityType!.GetForeignKeys(), key =>
            key.PrincipalEntityType.ClrType == typeof(FocusSessionEntity) &&
            key.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(WebSessionEntity.DeviceId), nameof(WebSessionEntity.FocusSessionId)]) &&
            key.PrincipalKey.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(FocusSessionEntity.DeviceId), nameof(FocusSessionEntity.ClientSessionId)]));

        Assert.True(foreignKey.IsRequired);
    }

    private static MonitorDbContext CreateModelContext()
    {
        var options = new DbContextOptionsBuilder<MonitorDbContext>()
            .UseNpgsql("Host=localhost;Database=woong_monitor_test;Username=test;Password=test")
            .Options;
        return new MonitorDbContext(options);
    }

    private static void AssertForeignKey<TEntity, TPrincipal>(
        MonitorDbContext dbContext,
        params string[] propertyNames)
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity));
        var foreignKey = Assert.Single(entityType!.GetForeignKeys(), key =>
            key.PrincipalEntityType.ClrType == typeof(TPrincipal) &&
            key.Properties.Select(property => property.Name).SequenceEqual(propertyNames));

        Assert.True(foreignKey.IsRequired);
    }
}
