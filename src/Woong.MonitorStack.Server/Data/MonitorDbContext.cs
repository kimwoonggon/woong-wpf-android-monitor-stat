using Microsoft.EntityFrameworkCore;

namespace Woong.MonitorStack.Server.Data;

public sealed class MonitorDbContext(DbContextOptions<MonitorDbContext> options) : DbContext(options)
{
    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeviceEntity>(entity =>
        {
            entity.ToTable("devices");
            entity.HasKey(device => device.Id);
            entity.Property(device => device.UserId).HasMaxLength(128).IsRequired();
            entity.Property(device => device.DeviceKey).HasMaxLength(256).IsRequired();
            entity.Property(device => device.DeviceName).HasMaxLength(256).IsRequired();
            entity.Property(device => device.TimezoneId).HasMaxLength(128).IsRequired();
            entity.HasIndex(device => new { device.UserId, device.Platform, device.DeviceKey }).IsUnique();
        });
    }
}
