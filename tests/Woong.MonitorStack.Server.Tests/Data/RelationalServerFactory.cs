using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Woong.MonitorStack.Server.Data;

namespace Woong.MonitorStack.Server.Tests.Data;

public sealed class RelationalServerFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public RelationalServerFactory()
    {
        _connection.Open();
    }

    public async Task EnsureDatabaseCreatedAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MonitorDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<MonitorDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
