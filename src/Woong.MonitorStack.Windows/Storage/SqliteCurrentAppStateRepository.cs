using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Woong.MonitorStack.Windows.Storage;

public sealed class SqliteCurrentAppStateRepository
{
    private readonly Func<string> _connectionStringFactory;

    public SqliteCurrentAppStateRepository(string connectionString)
        : this(() => connectionString)
    {
    }

    public SqliteCurrentAppStateRepository(Func<string> connectionStringFactory)
    {
        _connectionStringFactory = connectionStringFactory ?? throw new ArgumentNullException(nameof(connectionStringFactory));
    }

    public void Initialize()
    {
        using var connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS current_app_state (
                id INTEGER NOT NULL PRIMARY KEY CHECK (id = 1),
                client_state_id TEXT NOT NULL,
                device_id TEXT NOT NULL,
                platform_app_key TEXT NOT NULL,
                process_id INTEGER NULL,
                process_name TEXT NULL,
                process_path TEXT NULL,
                window_handle INTEGER NULL,
                observed_at_utc TEXT NOT NULL,
                local_date TEXT NOT NULL,
                timezone_id TEXT NOT NULL,
                status TEXT NOT NULL,
                source TEXT NOT NULL
            );
            """;
        _ = command.ExecuteNonQuery();
        EnsureBridgeColumn(connection, "client_state_id", "TEXT NULL");
        EnsureBridgeColumn(connection, "local_date", "TEXT NULL");
        EnsureBridgeColumn(connection, "timezone_id", "TEXT NULL");
        EnsureBridgeColumn(connection, "status", "TEXT NULL");
        EnsureBridgeColumn(connection, "source", "TEXT NULL");
    }

    public void Upsert(CurrentAppStateRecord state)
    {
        ArgumentNullException.ThrowIfNull(state);

        using var connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO current_app_state (
                id,
                client_state_id,
                device_id,
                platform_app_key,
                process_id,
                process_name,
                process_path,
                window_handle,
                observed_at_utc,
                local_date,
                timezone_id,
                status,
                source
            ) VALUES (
                1,
                $clientStateId,
                $deviceId,
                $platformAppKey,
                $processId,
                $processName,
                $processPath,
                $windowHandle,
                $observedAtUtc,
                $localDate,
                $timezoneId,
                $status,
                $source
            )
            ON CONFLICT(id) DO UPDATE SET
                client_state_id = excluded.client_state_id,
                device_id = excluded.device_id,
                platform_app_key = excluded.platform_app_key,
                process_id = excluded.process_id,
                process_name = excluded.process_name,
                process_path = excluded.process_path,
                window_handle = excluded.window_handle,
                observed_at_utc = excluded.observed_at_utc,
                local_date = excluded.local_date,
                timezone_id = excluded.timezone_id,
                status = excluded.status,
                source = excluded.source;
            """;
        AddParameters(command, state);

        _ = command.ExecuteNonQuery();
    }

    public CurrentAppStateRecord? GetLatest()
    {
        using var connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                device_id,
                platform_app_key,
                process_id,
                process_name,
                process_path,
                window_handle,
                observed_at_utc,
                local_date,
                timezone_id,
                status,
                source,
                client_state_id
            FROM current_app_state
            WHERE id = 1;
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadState(reader) : null;
    }

    private SqliteConnection OpenConnection()
    {
        string connectionString = _connectionStringFactory();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("SQLite connection string must not be empty.");
        }

        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static void AddParameters(SqliteCommand command, CurrentAppStateRecord state)
    {
        _ = command.Parameters.AddWithValue("$clientStateId", state.ClientStateId);
        _ = command.Parameters.AddWithValue("$deviceId", state.DeviceId);
        _ = command.Parameters.AddWithValue("$platformAppKey", state.PlatformAppKey);
        _ = command.Parameters.AddWithValue("$processId", (object?)state.ProcessId ?? DBNull.Value);
        _ = command.Parameters.AddWithValue("$processName", (object?)state.ProcessName ?? DBNull.Value);
        _ = command.Parameters.AddWithValue("$processPath", (object?)state.ProcessPath ?? DBNull.Value);
        _ = command.Parameters.AddWithValue("$windowHandle", (object?)state.WindowHandle ?? DBNull.Value);
        _ = command.Parameters.AddWithValue("$observedAtUtc", FormatUtc(state.ObservedAtUtc));
        _ = command.Parameters.AddWithValue("$localDate", state.LocalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        _ = command.Parameters.AddWithValue("$timezoneId", state.TimezoneId);
        _ = command.Parameters.AddWithValue("$status", state.Status);
        _ = command.Parameters.AddWithValue("$source", state.Source);
    }

    private static CurrentAppStateRecord ReadState(SqliteDataReader reader)
        => new(
            reader.GetString(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetInt32(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetInt64(5),
            DateTimeOffset.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
            DateOnly.ParseExact(reader.GetString(7), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            reader.GetString(8),
            reader.GetString(9),
            reader.GetString(10),
            reader.GetString(11));

    private static void EnsureBridgeColumn(SqliteConnection connection, string columnName, string definition)
    {
        using SqliteCommand existsCommand = connection.CreateCommand();
        existsCommand.CommandText = """
            SELECT COUNT(*)
            FROM pragma_table_info('current_app_state')
            WHERE name = $columnName;
            """;
        _ = existsCommand.Parameters.AddWithValue("$columnName", columnName);
        long exists = Convert.ToInt64(existsCommand.ExecuteScalar(), CultureInfo.InvariantCulture);
        if (exists > 0)
        {
            return;
        }

        using SqliteCommand addCommand = connection.CreateCommand();
        addCommand.CommandText = $"ALTER TABLE current_app_state ADD COLUMN {columnName} {definition};";
        _ = addCommand.ExecuteNonQuery();
    }

    private static string FormatUtc(DateTimeOffset value)
        => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
}
