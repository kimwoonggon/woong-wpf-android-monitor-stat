using Microsoft.EntityFrameworkCore;

namespace Woong.MonitorStack.Server.Data;

public sealed class MonitorDbContext(DbContextOptions<MonitorDbContext> options) : DbContext(options)
{
    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();

    public DbSet<FocusSessionEntity> FocusSessions => Set<FocusSessionEntity>();

    public DbSet<WebSessionEntity> WebSessions => Set<WebSessionEntity>();

    public DbSet<RawEventEntity> RawEvents => Set<RawEventEntity>();

    public DbSet<DailySummaryEntity> DailySummaries => Set<DailySummaryEntity>();

    public DbSet<DeviceStateSessionEntity> DeviceStateSessions => Set<DeviceStateSessionEntity>();

    public DbSet<AppFamilyEntity> AppFamilies => Set<AppFamilyEntity>();

    public DbSet<AppFamilyMappingEntity> AppFamilyMappings => Set<AppFamilyMappingEntity>();

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

        modelBuilder.Entity<FocusSessionEntity>(entity =>
        {
            entity.ToTable("focus_sessions");
            entity.HasKey(session => session.Id);
            entity.Property(session => session.ClientSessionId).HasMaxLength(128).IsRequired();
            entity.Property(session => session.PlatformAppKey).HasMaxLength(256).IsRequired();
            entity.Property(session => session.TimezoneId).HasMaxLength(128).IsRequired();
            entity.Property(session => session.Source).HasMaxLength(128).IsRequired();
            entity.Property(session => session.ProcessName).HasMaxLength(256);
            entity.Property(session => session.ProcessPath).HasMaxLength(1024);
            entity.Property(session => session.WindowTitle).HasMaxLength(512);
            entity.HasIndex(session => new { session.DeviceId, session.ClientSessionId }).IsUnique();
        });

        modelBuilder.Entity<WebSessionEntity>(entity =>
        {
            entity.ToTable("web_sessions");
            entity.HasKey(session => session.Id);
            entity.Property(session => session.FocusSessionId).HasMaxLength(128).IsRequired();
            entity.Property(session => session.BrowserFamily).HasMaxLength(64).IsRequired();
            entity.Property(session => session.Url).HasMaxLength(4096);
            entity.Property(session => session.Domain).HasMaxLength(253).IsRequired();
            entity.Property(session => session.PageTitle).HasMaxLength(512);
            entity.Property(session => session.CaptureMethod).HasMaxLength(64);
            entity.Property(session => session.CaptureConfidence).HasMaxLength(64);
            entity.HasIndex(session => new
            {
                session.DeviceId,
                session.FocusSessionId,
                session.StartedAtUtc,
                session.EndedAtUtc,
                session.Url
            }).IsUnique();
        });

        modelBuilder.Entity<RawEventEntity>(entity =>
        {
            entity.ToTable("raw_events");
            entity.HasKey(rawEvent => rawEvent.Id);
            entity.Property(rawEvent => rawEvent.ClientEventId).HasMaxLength(128).IsRequired();
            entity.Property(rawEvent => rawEvent.EventType).HasMaxLength(128).IsRequired();
            entity.Property(rawEvent => rawEvent.PayloadJson).IsRequired();
            entity.HasIndex(rawEvent => new { rawEvent.DeviceId, rawEvent.ClientEventId }).IsUnique();
        });

        modelBuilder.Entity<DailySummaryEntity>(entity =>
        {
            entity.ToTable("daily_summaries");
            entity.HasKey(summary => summary.Id);
            entity.Property(summary => summary.UserId).HasMaxLength(128).IsRequired();
            entity.Property(summary => summary.TimezoneId).HasMaxLength(128).IsRequired();
            entity.Property(summary => summary.TopAppsJson).IsRequired();
            entity.Property(summary => summary.TopDomainsJson).IsRequired();
            entity.HasIndex(summary => new
            {
                summary.UserId,
                summary.SummaryDate,
                summary.TimezoneId
            }).IsUnique();
        });

        modelBuilder.Entity<DeviceStateSessionEntity>(entity =>
        {
            entity.ToTable("device_state_sessions");
            entity.HasKey(session => session.Id);
            entity.Property(session => session.ClientSessionId).HasMaxLength(128).IsRequired();
            entity.Property(session => session.StateType).HasMaxLength(64).IsRequired();
            entity.Property(session => session.TimezoneId).HasMaxLength(128).IsRequired();
            entity.HasIndex(session => new { session.DeviceId, session.ClientSessionId }).IsUnique();
        });

        modelBuilder.Entity<AppFamilyEntity>(entity =>
        {
            entity.ToTable("app_families");
            entity.HasKey(family => family.Id);
            entity.Property(family => family.Key).HasMaxLength(128).IsRequired();
            entity.Property(family => family.DisplayName).HasMaxLength(256).IsRequired();
            entity.HasIndex(family => family.Key).IsUnique();
        });

        modelBuilder.Entity<AppFamilyMappingEntity>(entity =>
        {
            entity.ToTable("app_family_mappings");
            entity.HasKey(mapping => mapping.Id);
            entity.Property(mapping => mapping.MappingType).HasMaxLength(64).IsRequired();
            entity.Property(mapping => mapping.MatchKey).HasMaxLength(512).IsRequired();
            entity.HasIndex(mapping => new { mapping.MappingType, mapping.MatchKey }).IsUnique();
            entity.HasOne(mapping => mapping.AppFamily)
                .WithMany()
                .HasForeignKey(mapping => mapping.AppFamilyId);
        });
    }
}
