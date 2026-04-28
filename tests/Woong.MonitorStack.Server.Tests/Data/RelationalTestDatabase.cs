using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Tests.Data;

public sealed class RelationalTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private RelationalTestDatabase(SqliteConnection connection, MonitorDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public MonitorDbContext Context { get; }

    public static async Task<RelationalTestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<MonitorDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new MonitorDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return new RelationalTestDatabase(connection, context);
    }

    public async Task ResetAsync()
    {
        await Context.Database.EnsureDeletedAsync();
        await Context.Database.EnsureCreatedAsync();
        Context.ChangeTracker.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
