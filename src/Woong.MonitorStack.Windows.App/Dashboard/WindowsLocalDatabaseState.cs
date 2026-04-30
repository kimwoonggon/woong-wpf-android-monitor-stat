using System.IO;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public sealed class WindowsLocalDatabaseState
{
    private readonly object _gate = new();
    private string _databasePath;

    public WindowsLocalDatabaseState(string databasePath)
    {
        _databasePath = NormalizeDatabasePath(databasePath);
    }

    public string DatabasePath
    {
        get
        {
            lock (_gate)
            {
                return _databasePath;
            }
        }
    }

    public string ConnectionString => WindowsAppOptions.BuildConnectionString(DatabasePath);

    public void SwitchTo(string databasePath)
    {
        string normalizedPath = NormalizeDatabasePath(databasePath);
        lock (_gate)
        {
            _databasePath = normalizedPath;
        }
    }

    private static string NormalizeDatabasePath(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path must not be empty.", nameof(databasePath));
        }

        return Path.GetFullPath(databasePath);
    }
}
