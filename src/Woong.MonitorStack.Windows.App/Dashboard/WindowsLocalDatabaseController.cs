using System.IO;
using Microsoft.Data.Sqlite;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public interface IWindowsDatabaseFilePicker
{
    string? PickNewDatabasePath(string currentDatabasePath);

    string? PickExistingDatabasePath(string currentDatabasePath);

    bool ConfirmDeleteDatabase(string currentDatabasePath);
}

public sealed class WindowsLocalDatabaseController(
    WindowsLocalDatabaseState databaseState,
    SqliteFocusSessionRepository focusSessionRepository,
    SqliteWebSessionRepository webSessionRepository,
    SqliteSyncOutboxRepository syncOutboxRepository,
    IWindowsDatabaseFilePicker filePicker) : IDashboardDatabaseController
{
    public string CurrentDatabasePath => databaseState.DatabasePath;

    public bool CanDeleteCurrentDatabase => true;

    public DashboardDatabaseActionResult CreateNewDatabase()
    {
        string? selectedPath = filePicker.PickNewDatabasePath(databaseState.DatabasePath);
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return Cancelled("Create database cancelled.");
        }

        PrepareDatabaseAt(selectedPath, deleteExisting: false);
        return Success("Created local database.");
    }

    public DashboardDatabaseActionResult LoadExistingDatabase()
    {
        string? selectedPath = filePicker.PickExistingDatabasePath(databaseState.DatabasePath);
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return Cancelled("Load database cancelled.");
        }

        if (!File.Exists(selectedPath))
        {
            return new DashboardDatabaseActionResult(false, databaseState.DatabasePath, $"Database file does not exist: {selectedPath}");
        }

        PrepareDatabaseAt(selectedPath, deleteExisting: false);
        return Success("Loaded existing local database.");
    }

    public DashboardDatabaseActionResult DeleteCurrentDatabase()
    {
        string currentPath = databaseState.DatabasePath;
        if (!filePicker.ConfirmDeleteDatabase(currentPath))
        {
            return Cancelled("Delete database cancelled.");
        }

        PrepareDatabaseAt(currentPath, deleteExisting: true);
        return Success("Deleted local database and recreated an empty database.");
    }

    private void PrepareDatabaseAt(string databasePath, bool deleteExisting)
    {
        string fullPath = Path.GetFullPath(databasePath);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        SqliteConnection.ClearAllPools();
        if (deleteExisting && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        databaseState.SwitchTo(fullPath);
        InitializeRepositories();
    }

    private void InitializeRepositories()
    {
        focusSessionRepository.Initialize();
        webSessionRepository.Initialize();
        syncOutboxRepository.Initialize();
    }

    private DashboardDatabaseActionResult Success(string message)
        => new(true, databaseState.DatabasePath, $"{message} {databaseState.DatabasePath}");

    private DashboardDatabaseActionResult Cancelled(string message)
        => new(false, databaseState.DatabasePath, message);
}
