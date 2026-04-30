using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Tests.Data;

public sealed class PostgresTestDatabase : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container;

    private PostgresTestDatabase(PostgreSqlContainer container, MonitorDbContext context)
    {
        _container = container;
        Context = context;
    }

    public MonitorDbContext Context { get; }

    public MonitorDbContext CreateContext()
        => new(new DbContextOptionsBuilder<MonitorDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options);

    public static async Task<PostgresTestDatabase> CreateAsync()
    {
        PostgresTestDatabase database = await CreateUnmigratedAsync();
        await database.Context.Database.MigrateAsync();

        return database;
    }

    public static async Task<PostgresTestDatabase> CreateUnmigratedAsync()
    {
        var container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("woong_monitor_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await container.StartAsync();

        var options = new DbContextOptionsBuilder<MonitorDbContext>()
            .UseNpgsql(container.GetConnectionString())
            .Options;
        var context = new MonitorDbContext(options);

        return new PostgresTestDatabase(container, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _container.DisposeAsync();
    }
}
