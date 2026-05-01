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

    [Fact]
    public void WebSessionClientSessionMigration_BackfillsLegacyRowsBeforeAddingUniqueIndex()
    {
        var migrationsDirectory = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Woong.MonitorStack.Server",
            "Data",
            "Migrations");
        var migrationFile = Directory
            .EnumerateFiles(migrationsDirectory, "*_AddWebSessionClientSessionId.cs")
            .Single();
        string migrationText = File.ReadAllText(migrationFile);

        Assert.Contains("nullable: true", migrationText, StringComparison.Ordinal);
        Assert.Contains("legacy-web-session-", migrationText, StringComparison.Ordinal);
        Assert.Contains("AlterColumn<string>", migrationText, StringComparison.Ordinal);
        Assert.Contains("IX_web_sessions_DeviceId_ClientSessionId", migrationText, StringComparison.Ordinal);
        Assert.DoesNotContain("defaultValue: \"\"", migrationText, StringComparison.Ordinal);
    }

    [Fact]
    public void ServerSessionForeignKeyMigration_AddsDeviceAndFocusRelationships()
    {
        var migrationsDirectory = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Woong.MonitorStack.Server",
            "Data",
            "Migrations");
        var migrationFile = Directory
            .EnumerateFiles(migrationsDirectory, "*_AddServerSessionForeignKeys.cs")
            .Single();
        string migrationText = File.ReadAllText(migrationFile);

        Assert.Contains("FK_focus_sessions_devices_DeviceId", migrationText, StringComparison.Ordinal);
        Assert.Contains("FK_web_sessions_devices_DeviceId", migrationText, StringComparison.Ordinal);
        Assert.Contains("FK_raw_events_devices_DeviceId", migrationText, StringComparison.Ordinal);
        Assert.Contains("FK_device_state_sessions_devices_DeviceId", migrationText, StringComparison.Ordinal);
        Assert.Contains("FK_web_sessions_focus_sessions_DeviceId_FocusSessionId", migrationText, StringComparison.Ordinal);
        Assert.Contains("ReferentialAction.Restrict", migrationText, StringComparison.Ordinal);
    }

    [Fact]
    public void LocationContextMigration_AddsPrivacySafeNullableCoordinateTable()
    {
        var migrationsDirectory = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Woong.MonitorStack.Server",
            "Data",
            "Migrations");
        var migrationFile = Directory
            .EnumerateFiles(migrationsDirectory, "*_AddLocationContextTable.cs")
            .Single();
        string migrationText = File.ReadAllText(migrationFile);

        Assert.Contains("location_contexts", migrationText, StringComparison.Ordinal);
        Assert.Contains("Latitude", migrationText, StringComparison.Ordinal);
        Assert.Contains("Longitude", migrationText, StringComparison.Ordinal);
        Assert.Contains("AccuracyMeters", migrationText, StringComparison.Ordinal);
        Assert.Contains("nullable: true", migrationText, StringComparison.Ordinal);
        Assert.Contains("IX_location_contexts_DeviceId_ClientContextId", migrationText, StringComparison.Ordinal);
        Assert.Contains("FK_location_contexts_devices_DeviceId", migrationText, StringComparison.Ordinal);
    }

    [Fact]
    public void DeviceTokenVerifierMigration_AddsSaltAndHashWithoutPlaintextTokenColumn()
    {
        var migrationsDirectory = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Woong.MonitorStack.Server",
            "Data",
            "Migrations");
        var migrationFile = Directory
            .EnumerateFiles(migrationsDirectory, "*_AddDeviceTokenVerifier.cs")
            .Single();
        string migrationText = File.ReadAllText(migrationFile);

        Assert.Contains("DeviceTokenHash", migrationText, StringComparison.Ordinal);
        Assert.Contains("DeviceTokenSalt", migrationText, StringComparison.Ordinal);
        Assert.Contains("maxLength: 128", migrationText, StringComparison.Ordinal);
        Assert.DoesNotContain("DeviceToken\"", migrationText, StringComparison.Ordinal);
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
