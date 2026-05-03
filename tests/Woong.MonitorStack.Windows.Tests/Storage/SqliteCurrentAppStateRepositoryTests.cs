using Microsoft.Data.Sqlite;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.Tests.Storage;

public sealed class SqliteCurrentAppStateRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    [Fact]
    public void Upsert_ReplacesLatestObservedForegroundAppInSingleRow()
    {
        SqliteCurrentAppStateRepository repository = CreateRepository();
        repository.Upsert(new CurrentAppStateRecord(
            "windows-device-1",
            "Code.exe",
            processId: 10,
            processName: "Code.exe",
            processPath: "C:\\Apps\\Code.exe",
            windowHandle: 100,
            observedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero)));

        repository.Upsert(new CurrentAppStateRecord(
            "windows-device-1",
            "chrome.exe",
            processId: 20,
            processName: "chrome.exe",
            processPath: "C:\\Apps\\chrome.exe",
            windowHandle: 200,
            observedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 3, 0, TimeSpan.Zero)));

        CurrentAppStateRecord? latest = repository.GetLatest();

        Assert.NotNull(latest);
        Assert.Equal("windows-device-1", latest.DeviceId);
        Assert.Equal("chrome.exe", latest.PlatformAppKey);
        Assert.Equal(20, latest.ProcessId);
        Assert.Equal("chrome.exe", latest.ProcessName);
        Assert.Equal(@"C:\Apps\chrome.exe", latest.ProcessPath);
        Assert.Equal(200, latest.WindowHandle);
        Assert.Equal(new DateTimeOffset(2026, 4, 28, 0, 3, 0, TimeSpan.Zero), latest.ObservedAtUtc);
        Assert.Equal(new DateOnly(2026, 4, 28), latest.LocalDate);
        Assert.Equal("UTC", latest.TimezoneId);
        Assert.Equal("Active", latest.Status);
        Assert.Equal("windows_current_app_state", latest.Source);
        Assert.StartsWith("windows-current:windows-device-1:", latest.ClientStateId, StringComparison.Ordinal);
        Assert.Equal(1, CountRows());
    }

    [Fact]
    public void Initialize_CreatesMetadataOnlySchemaWithoutContentColumns()
    {
        SqliteCurrentAppStateRepository repository = CreateRepository();

        IReadOnlySet<string> columns = ReadCurrentAppStateColumns();

        Assert.Contains("device_id", columns);
        Assert.Contains("client_state_id", columns);
        Assert.Contains("platform_app_key", columns);
        Assert.Contains("process_id", columns);
        Assert.Contains("process_name", columns);
        Assert.Contains("process_path", columns);
        Assert.Contains("window_handle", columns);
        Assert.Contains("observed_at_utc", columns);
        Assert.Contains("local_date", columns);
        Assert.Contains("timezone_id", columns);
        Assert.Contains("status", columns);
        Assert.Contains("source", columns);
        Assert.DoesNotContain("window_title", columns);
        Assert.DoesNotContain("page_title", columns);
        Assert.DoesNotContain("url", columns);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private SqliteCurrentAppStateRepository CreateRepository()
    {
        var repository = new SqliteCurrentAppStateRepository($"Data Source={_dbPath};Pooling=False");
        repository.Initialize();

        return repository;
    }

    private int CountRows()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath};Pooling=False");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM current_app_state;";

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private IReadOnlySet<string> ReadCurrentAppStateColumns()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath};Pooling=False");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(current_app_state);";

        using SqliteDataReader reader = command.ExecuteReader();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }
}
