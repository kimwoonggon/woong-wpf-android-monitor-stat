namespace Woong.MonitorStack.Server.Tests.Data;

public sealed class ProductionMigrationFilesTests
{
    [Fact]
    public void InitialCreateMigration_DefinesCorePostgreSqlTables()
    {
        var migrationsDirectory = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Woong.MonitorStack.Server",
            "Data",
            "Migrations");
        var migrationFile = Directory
            .EnumerateFiles(migrationsDirectory, "*_InitialCreate.cs")
            .Single();
        var migrationText = File.ReadAllText(migrationFile);

        Assert.Contains("devices", migrationText, StringComparison.Ordinal);
        Assert.Contains("focus_sessions", migrationText, StringComparison.Ordinal);
        Assert.Contains("web_sessions", migrationText, StringComparison.Ordinal);
        Assert.Contains("raw_events", migrationText, StringComparison.Ordinal);
        Assert.Contains("daily_summaries", migrationText, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "total_todolist.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
