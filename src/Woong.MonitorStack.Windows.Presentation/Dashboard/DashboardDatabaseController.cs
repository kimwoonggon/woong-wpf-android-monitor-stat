namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public interface IDashboardDatabaseController
{
    string CurrentDatabasePath { get; }

    bool CanDeleteCurrentDatabase { get; }

    DashboardDatabaseActionResult CreateNewDatabase();

    DashboardDatabaseActionResult LoadExistingDatabase();

    DashboardDatabaseActionResult DeleteCurrentDatabase();
}

public sealed record DashboardDatabaseActionResult(
    bool Succeeded,
    string DatabasePath,
    string StatusMessage);

public sealed class NullDashboardDatabaseController : IDashboardDatabaseController
{
    public string CurrentDatabasePath => "No local database configured";

    public bool CanDeleteCurrentDatabase => false;

    public DashboardDatabaseActionResult CreateNewDatabase()
        => new(false, CurrentDatabasePath, "Database creation is unavailable in this mode.");

    public DashboardDatabaseActionResult LoadExistingDatabase()
        => new(false, CurrentDatabasePath, "Database loading is unavailable in this mode.");

    public DashboardDatabaseActionResult DeleteCurrentDatabase()
        => new(false, CurrentDatabasePath, "Database deletion is unavailable in this mode.");
}
